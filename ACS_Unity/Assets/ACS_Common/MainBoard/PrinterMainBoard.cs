using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ACS_Common.Base;
using ACS_Common.GCode;
using ACS_Common.Utils;

namespace ACS_Common.MainBoard
{
    /// <summary>
    /// 3d打印主板
    /// </summary>
    public partial class PrinterMainBoard : ACS_Object
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
                Canceled = 7,
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
        

        private PrinterMainBoardStatus _status;
        public PrinterMainBoardStatus Status => _status;
        private enum EControlSignal : SByte
        {
            None = 0,
            Pause = 1,
            Continue = 2,
            Stop = 3,
        }

        private EControlSignal _controlSignal;
        private GCodeBuffer _buffer;
        private Task _printLoop;
        private CancellationTokenSource _ts;
        
        private Stopwatch _sw = new System.Diagnostics.Stopwatch();

        // /// <summary>
        // /// 发送GCode
        // /// </summary>
        // /// <param name="code"></param>
        // public void SendGCode(GCommand command)
        // {
        //     const string m = nameof(SendGCode);
        //     LogMethod(m, $"command: {command}");
        //     ExecuteCommand(command);
        // }

        public void SetStream(GCommandStream stream)
        {
            _buffer?.Dispose();
            _buffer = new GCodeBuffer(stream);
        }

        /// <summary>
        /// 开始打印命令流
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
            _ts = new CancellationTokenSource();
            try
            {
                _printLoop = Task.Run<long>(() => { return PrintLoop(_ts.Token);}, _ts.Token);
            }
            catch (OperationCanceledException oce) {
                LogInfo(m, "PrintLoop canceled");
                _buffer.Stop();
                SetState(PrinterMainBoardStatus.ECommandState.Canceled);
            }
            LogInfo(m, $"_printLoop.Status: {_printLoop.Status}");
        }

        /// <summary>
        /// 暂停，等待当前命令执行完毕将状态置为pause
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
        /// 停止打印，等待当前命令执行完毕结束printLoop，将状态置为idle
        /// </summary>
        public void Stop()
        {
            const string m = nameof(Stop);
            LogMethod(m);
            _controlSignal = EControlSignal.Stop;
        }
        public override void Init()
        {
            const string m = nameof(Init);
            LogMethod(m);
            Task.Run(Startup);
            // _startup = StartCoroutine(Startup());
            LogInfo(m, $"Stopwatch.IsHighResolution: {Stopwatch.IsHighResolution}, Stopwatch.Frequency: {Stopwatch.Frequency}");
        }

        public override void Clear()
        {
            const string m = nameof(Clear);
            LogInfo(m, $"Status: {Status}, _printLoop.Status: {(_printLoop?.Status.ToString()??"is null")}");
            base.Clear();
            Stop();
            _ts?.Cancel();
            _buffer?.Dispose();
        }

        private async void Startup()
        {
            const string m = nameof(Startup);
            LogMethod(m);
            // if (null != _stepMotorDriverX) _dof["x"] = new DOF("x");
            // if (null != _stepMotorDriverY) _dof["y"] = new DOF("y");
            // if (null != _stepMotorDriverZ) _dof["z"] = new DOF("z");
            // if (null != _stepMotorDriverE) _dof["e"] = new DOF("e");
            await SelfTest();
            SetState(PrinterMainBoardStatus.ECommandState.Idle);
        }

        /// <summary>
        /// 开机自检
        /// </summary>
        /// <returns></returns>
        private async Task SelfTest()
        {
            const string m = nameof(SelfTest);
            LogMethod(m);
            SetState(PrinterMainBoardStatus.ECommandState.SelfTest);
            await Task.Delay(500);
        }

        private async Task<long> PrintLoop(CancellationToken ct)
        {
            const string m = nameof(PrintLoop);
            LogMethod(m);
            SetState(PrinterMainBoardStatus.ECommandState.Waiting);
            if (null == _buffer)
            {
                LogErr(m, $"null == _buffer");
                SetState(PrinterMainBoardStatus.ECommandState.PrintFailed);
                return 0;
            }
            if (null == _buffer.Source)
            {
                LogErr(m, $"null == _buffer.Source");
                SetState(PrinterMainBoardStatus.ECommandState.PrintFailed);
                return 0;
            }
            while (_buffer.Source is { IndexBuilt: false })
            {
                // 等待状态下cancel
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100);
            }
            _status.CommandStreamTotalLine = _buffer.Source.TotalLines;
            SetState(PrinterMainBoardStatus.ECommandState.Printing);
            SetExecutingCommandLineIdx(0);
            _sw.Reset();
            _lastCommandFinishTimeStamp = 0;
            _buffer.Start();
            // var canceled = false;
            while (_status.ExecutingCommandLineIdx < _status.CommandStreamTotalLine)
            {
                ct.ThrowIfCancellationRequested();
                GCommand nextCommand = null;
                // 从buffer取得下一条命令
                while (_buffer.GetGCommand(out nextCommand) >= 0 && null == nextCommand)
                {
                    ct.ThrowIfCancellationRequested();
                    // LogInfo(m, "wait for buffer");
                    await Task.Delay(100);
                    continue;
                }
                if (null == nextCommand)
                {
                    LogErr(m, $"null == nextCommand, _status.ExecutingCommandLineIdx: {_status.ExecutingCommandLineIdx}");
                    SetState(PrinterMainBoardStatus.ECommandState.PrintFailed);
                    break;
                }

                try {
                    await RunGCommand(nextCommand, ct);
                }
                catch (OperationCanceledException oce) {
                    LogInfo(m, "RunGCommand canceled");
                    throw oce;
                }
                
                if (EControlSignal.Pause == _controlSignal)
                {
                    _controlSignal = EControlSignal.None;
                    SetState(PrinterMainBoardStatus.ECommandState.Pause);
                    while (EControlSignal.None == _controlSignal)
                    {
                        // pause状态下cancel
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(100);
                    }
                    if (EControlSignal.Continue == _controlSignal)
                    {
                        _controlSignal = EControlSignal.None;
                        SetState(PrinterMainBoardStatus.ECommandState.Printing);
                    }
                }
                if (EControlSignal.Stop == _controlSignal)
                {
                    SetExecutingCommandLineIdx(-1);
                    break;
                }
            }

            SetState(PrinterMainBoardStatus.ECommandState.Idle);
            _buffer.Stop();
            LogInfo(m, "print task done");
            return _status.ExecutingCommandLineIdx;
        }

        private void SetState(PrinterMainBoardStatus.ECommandState state)
        {
            const string m = nameof(SetState);
            if (_status.CommandState == state) return;
            // LogMethod(m, $"state: {state}");
            _status.CommandState = state;
            _controlSignal = EControlSignal.None;
            if (null != _printLoop) LogInfo(m, $"state: {state}, _printLoop.Status: {_printLoop.Status}");
        }

        private void SetExecutingCommandLineIdx(long idx)
        {
            const string m = nameof(SetExecutingCommandLineIdx);
            // LogMethod(m, $"idx: {idx}");
            if (_status.ExecutingCommandLineIdx == idx) return;
            _status.ExecutingCommandLineIdx = idx;
            _status.ExecutingProgress = 0f;
        }

        private void SetCommandExecuteProgress(float progress)
        {
            const string m = nameof(SetCommandExecuteProgress);
            // LogMethod(m, $"progress: {progress}");
            _status.ExecutingProgress = progress;
        }
    }
}