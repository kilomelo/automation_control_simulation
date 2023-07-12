using System;
using System.Collections;
using System.Diagnostics;
using ACS_Common.Base;
using ACS_Common.Driver;
using ACS_Common.GCode;
using ACS_Common.Utils;
using UnityEngine;

namespace ACS_Common.MainBoard
{
    /// <summary>
    /// 3d打印主板
    /// </summary>
    public partial class PrinterMainBoard : ACS_Behaviour, IGCommandStreamHolder
    {
        /// <summary>
        /// 打印主板的状态
        /// </summary>
        public struct PrinterMainBoardStatus
        {
            public enum ECommandState : SByte
            {
                Shutdown = 0,
                SelfTest = 1,
                Idle = 2,
                Waiting = 3,
                Printing = 4,
                Pause = 5,
                PrintFailed = 6,
            }
            // 当前状态
            public ECommandState CommandState;
            // 正在执行的行数
            public long ExecutingCommandLineIdx;
            // 命令流总行数
            public long CommandStreamTotalLine;
            // 正在执行的命令进度
            public float ExecutingProgress;
            // 已执行时间
            public long ExecuteTimeMilliseconds;

            public override string ToString()
            {
                var millisec = ExecuteTimeMilliseconds;
                var timeHour = millisec / (1000 * 60 * 60);
                millisec -= timeHour * (1000 * 60 * 60);
                var timeMin = millisec / (1000 * 60);
                millisec -= timeMin * (1000 * 60);
                var timeSec = millisec / 1000;
                millisec -= timeSec * 1000;
                var result = $"[{CommandState}] | [{ExecutingCommandLineIdx + 1}/{CommandStreamTotalLine} {StringUtils.ProgressBar(ExecutingProgress)}]";
                if (timeHour > 0) return $"{result} | Time: [{timeHour} h {timeMin} m {timeSec} s {millisec} ms]";
                if (timeMin > 0) return $"{result} | Time: [{timeMin} m {timeSec} s {millisec} ms]";
                if (timeSec > 0) return $"{result} | Time: [{timeSec} s {millisec} ms]";
                return $"{result} | Time: [{millisec} ms]";
            }
        }
        
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverX;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverY;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverZ;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverE;

        private PrinterMainBoardStatus _status;
        public PrinterMainBoardStatus Status => _status; 
        
        public GCommandStream Stream { get; private set; }
        public Action OnStreamUpdate { get; set; }
        public Action OnPrintProgressUpdate { get; set; }
        public Action OnStateChange { get; set; }

        private enum EControlSignal : SByte
        {
            None = 0,
            Pause = 1,
            Continue = 2,
            Stop = 3,
        }

        private EControlSignal _controlSignal;

        private Coroutine _printLoop;
        private Coroutine _runCommand;
        private Coroutine _startup;
        
        Stopwatch _sw = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// 发送GCode
        /// </summary>
        /// <param name="code"></param>
        public void SendGCode(GCommand command)
        {
            const string m = nameof(SendGCode);
            LogMethod(m, $"command: {command}");
            ExecuteCommand(command);
        }

        /// <summary>
        /// 载入GCode文件
        /// </summary>
        /// <param name="gCodeFilePath"></param>
        public void LoadGCodeFile(string gCodeFilePath)
        {
            const string m = nameof(LoadGCodeFile);
            LogMethod(m, $"gCodeFilePath: {gCodeFilePath}");
            if (_status.CommandState != PrinterMainBoardStatus.ECommandState.Idle)
            {
                LogErr(m, $"state is not Idle");
                return;
            }
            if (string.IsNullOrEmpty(gCodeFilePath))
            {
                LogErr(m, $"gCodeFilePath is empty");
                return;
            }
            Stream?.Dispose();
            Stream = new GCommandStream(gCodeFilePath);
            Stream.CacheCapacity = 500;
            OnStreamUpdate?.Invoke();
        }

        /// <summary>
        /// 开始打印已载入的GCode文件
        /// </summary>
        public void Execute()
        {
            const string m = nameof(Execute);
            LogMethod(m);
            if (_status.CommandState != PrinterMainBoardStatus.ECommandState.Idle)
            {
                LogErr(m, $"state is not Idle");
                return;
            }
            _printLoop = StartCoroutine(PrintLoop());
            // DisplayTimerProperties();
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            const string m = nameof(Pause);
            LogMethod(m);
            if (_status.CommandState != PrinterMainBoardStatus.ECommandState.Printing)
            {
                LogErr(m, $"state is not Printing");
                return;
            }
            if (EControlSignal.Stop == _controlSignal)
            {
                LogInfo(m, "can't pause, wait for stop");
                return;
            }
            _controlSignal = EControlSignal.Pause;
        }

        /// <summary>
        /// 继续
        /// </summary>
        public void Continue()
        {
            const string m = nameof(Continue);
            LogMethod(m);
            if (_status.CommandState != PrinterMainBoardStatus.ECommandState.Pause)
            {
                LogErr(m, $"state is not Pause");
                return;
            }
            if (EControlSignal.Stop == _controlSignal)
            {
                LogInfo(m, "can't continue, wait for stop");
                return;
            }
            _controlSignal = EControlSignal.Continue;
        }

        /// <summary>
        /// 停止打印
        /// </summary>
        public void Stop()
        {
            const string m = nameof(Stop);
            LogMethod(m);
            if (_status.CommandState != PrinterMainBoardStatus.ECommandState.Printing && _status.CommandState != PrinterMainBoardStatus.ECommandState.Pause)
            {
                LogErr(m, $"state is not Printing");
                return;
            }
            _controlSignal = EControlSignal.Stop;
        }

        /// <summary>
        /// 执行单条命令
        /// </summary>
        /// <param name="command"></param>
        private void ExecuteCommand(GCommand command)
        {
            const string m = nameof(ExecuteCommand);
            // LogMethod(m, $"command: {command}");
            _runCommand = StartCoroutine(RunGCommand(command));
        }

        private void Start()
        {
            const string m = nameof(Start);
            LogMethod(m);
            _startup = StartCoroutine(Startup());
            LogInfo(m, $"Stopwatch.IsHighResolution: {Stopwatch.IsHighResolution}, Stopwatch.Frequency: {Stopwatch.Frequency}");
        }

        protected override void Clear()
        {
            base.Clear();
            Stream?.Dispose();
        }

        private IEnumerator Startup()
        {
            const string m = nameof(Startup);
            LogMethod(m);
            if (null != _stepMotorDriverX) _dof["x"] = new DOF("x");
            if (null != _stepMotorDriverY) _dof["y"] = new DOF("y");
            if (null != _stepMotorDriverZ) _dof["z"] = new DOF("z");
            if (null != _stepMotorDriverE) _dof["e"] = new DOF("e");
            yield return SelfTest();
            SetState(PrinterMainBoardStatus.ECommandState.Idle);
        }

        /// <summary>
        /// 开机自检
        /// </summary>
        /// <returns></returns>
        private IEnumerator SelfTest()
        {
            const string m = nameof(SelfTest);
            LogMethod(m);
            SetState(PrinterMainBoardStatus.ECommandState.SelfTest);
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator PrintLoop()
        {
            const string m = nameof(PrintLoop);
            LogMethod(m);
            SetState(PrinterMainBoardStatus.ECommandState.Waiting);
            if (null == Stream)
            {
                LogErr(m, $"null == Stream");
                SetState(PrinterMainBoardStatus.ECommandState.PrintFailed);
                yield break;
            }

            while (Stream is { IndexBuilt: false })
            {
                yield return 0;
            }
            if (null == Stream)
            {
                LogErr(m, $"null == Stream");
                SetState(PrinterMainBoardStatus.ECommandState.PrintFailed);
                yield break;
            }
            SetState(PrinterMainBoardStatus.ECommandState.Printing);
            SetExecutingCommandLineIdx(0);
            _status.CommandStreamTotalLine = Stream.TotalLines;
            var iteral = 0;
            _sw.Reset();
            _lastCommandFinishTimeStamp = 0;
            while (_status.ExecutingCommandLineIdx < _status.CommandStreamTotalLine)
            {
                var nextCommand = Stream.GetCommand(_status.ExecutingCommandLineIdx + iteral);
                while (nextCommand.CommandType is Def.EGCommandType.Invalid or Def.EGCommandType.None)
                {
                    iteral++;
                    nextCommand = Stream.GetCommand(_status.ExecutingCommandLineIdx + iteral);
                }
                SetExecutingCommandLineIdx(_status.ExecutingCommandLineIdx + iteral);
                iteral = 1;
                ExecuteCommand(nextCommand);
                while (_status.ExecutingProgress < 1f)
                {
                    yield return 0;
                }
                if (null == Stream)
                {
                    LogErr(m, $"null == Stream");
                    SetState(PrinterMainBoardStatus.ECommandState.PrintFailed);
                    yield break;
                }
                if (EControlSignal.Pause == _controlSignal)
                {
                    _controlSignal = EControlSignal.None;
                    SetState(PrinterMainBoardStatus.ECommandState.Pause);
                    while (EControlSignal.None == _controlSignal) yield return 0;
                    if (EControlSignal.Continue == _controlSignal)
                    {
                        _controlSignal = EControlSignal.None;
                        SetState(PrinterMainBoardStatus.ECommandState.Printing);
                    }
                }
                if (EControlSignal.Stop == _controlSignal)
                {
                    SetExecutingCommandLineIdx(-1);
                    SetState(PrinterMainBoardStatus.ECommandState.Idle);
                    yield break;
                }
                
                // yield return new WaitForSeconds(1f);
            }

            SetState(PrinterMainBoardStatus.ECommandState.Idle);
            LogInfo(m, "print task done");
        }

        private void SetState(PrinterMainBoardStatus.ECommandState state)
        {
            if (_status.CommandState == state) return;
            _status.CommandState = state;
            _controlSignal = EControlSignal.None;
            OnStateChange?.Invoke();
        }

        private void SetExecutingCommandLineIdx(long idx)
        {
            const string m = nameof(SetExecutingCommandLineIdx);
            // LogMethod(m, $"idx: {idx}");
            if (_status.ExecutingCommandLineIdx == idx) return;
            _status.ExecutingCommandLineIdx = idx;
            _status.ExecutingProgress = 0f;
            OnPrintProgressUpdate?.Invoke();
        }

        private void SetCommandExecuteProgress(float progress)
        {
            const string m = nameof(SetCommandExecuteProgress);
            // LogMethod(m, $"progress: {progress}");
            _status.ExecutingProgress = progress;
            OnPrintProgressUpdate?.Invoke();
        }
    }
}