using System;
using System.Collections;
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
    public class PrinterMainBoard : ACS_Behaviour, IGCommandStreamHolder
    {
        /// <summary>
        /// 打印主板的状态
        /// </summary>
        public struct PrinterMainBoardStatus
        {
            public enum EPrinterState : SByte
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
            public EPrinterState State;
            // 正在执行的行数
            public long ExecutingCommandLineIdx;
            // 命令流总行数
            public long CommandStreamTotalLine;
            // 正在执行的命令进度
            public float ExecutingProgress;

            public override string ToString()
            {
                return $"State: [{State}] | Line: [{ExecutingCommandLineIdx + 1} / {CommandStreamTotalLine}] | Progress: [{StringUtils.ProgressBar(ExecutingProgress)}]";
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
            if (_status.State != PrinterMainBoardStatus.EPrinterState.Idle)
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
            OnStreamUpdate?.Invoke();
        }

        /// <summary>
        /// 开始打印已载入的GCode文件
        /// </summary>
        public void Execute()
        {
            const string m = nameof(Execute);
            LogMethod(m);
            if (_status.State != PrinterMainBoardStatus.EPrinterState.Idle)
            {
                LogErr(m, $"state is not Idle");
                return;
            }
            StartCoroutine(Printing());
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            const string m = nameof(Pause);
            LogMethod(m);
            if (_status.State != PrinterMainBoardStatus.EPrinterState.Printing)
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
            if (_status.State != PrinterMainBoardStatus.EPrinterState.Pause)
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
            if (_status.State != PrinterMainBoardStatus.EPrinterState.Printing && _status.State != PrinterMainBoardStatus.EPrinterState.Pause)
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
            LogMethod(m, $"command: {command}");
        }

        private void Start()
        {
            const string m = nameof(Start);
            LogMethod(m);
            StartCoroutine(Startup());
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
            yield return SelfTest();
            SetState(PrinterMainBoardStatus.EPrinterState.Idle);
        }

        /// <summary>
        /// 开机自检
        /// </summary>
        /// <returns></returns>
        private IEnumerator SelfTest()
        {
            const string m = nameof(SelfTest);
            LogMethod(m);
            SetState(PrinterMainBoardStatus.EPrinterState.SelfTest);
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator Printing()
        {
            const string m = nameof(Printing);
            LogMethod(m);
            SetState(PrinterMainBoardStatus.EPrinterState.Waiting);
            if (null == Stream)
            {
                LogErr(m, $"null == Stream");
                SetState(PrinterMainBoardStatus.EPrinterState.PrintFailed);
                yield break;
            }

            while (Stream is { IndexBuilt: false })
            {
                yield return 0;
            }
            if (null == Stream)
            {
                LogErr(m, $"null == Stream");
                SetState(PrinterMainBoardStatus.EPrinterState.PrintFailed);
                yield break;
            }
            SetState(PrinterMainBoardStatus.EPrinterState.Printing);
            SetExecutingCommandLineIdx(0);
            _status.CommandStreamTotalLine = Stream.TotalLines;
            while (_status.ExecutingCommandLineIdx < _status.CommandStreamTotalLine)
            {
                if (null == Stream)
                {
                    LogErr(m, $"null == Stream");
                    SetState(PrinterMainBoardStatus.EPrinterState.PrintFailed);
                    yield break;
                }
                // ExecuteCommand();
                while (_status.ExecutingProgress < 1f)
                {
                    SetCommandExecuteProgress(_status.ExecutingProgress + .25f);
                    yield return new WaitForSeconds(0.01f);
                }
                if (EControlSignal.Pause == _controlSignal)
                {
                    _controlSignal = EControlSignal.None;
                    SetState(PrinterMainBoardStatus.EPrinterState.Pause);
                    while (EControlSignal.None == _controlSignal) yield return 0;
                    if (EControlSignal.Continue == _controlSignal)
                    {
                        _controlSignal = EControlSignal.None;
                        SetState(PrinterMainBoardStatus.EPrinterState.Printing);
                    }
                }
                if (EControlSignal.Stop == _controlSignal)
                {
                    SetExecutingCommandLineIdx(-1);
                    SetState(PrinterMainBoardStatus.EPrinterState.Idle);
                    yield break;
                }
                
                // yield return new WaitForSeconds(1f);
                SetExecutingCommandLineIdx(_status.ExecutingCommandLineIdx + 1);
            }

            SetState(PrinterMainBoardStatus.EPrinterState.Idle);
            LogInfo(m, "print task done");
        }

        private void SetState(PrinterMainBoardStatus.EPrinterState state)
        {
            if (_status.State == state) return;
            _status.State = state;
            _controlSignal = EControlSignal.None;
            OnStateChange?.Invoke();
        }

        private void SetExecutingCommandLineIdx(long idx)
        {
            const string m = nameof(SetExecutingCommandLineIdx);
            // LogMethod(m, $"idx: {idx}");
            // if (_status.ExecutingCommandLineIdx == idx) return;
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