using System;
using ACS_Common.Base;
using ACS_Common.Driver;
using ACS_Common.GCode;
using ACS_Common.Utils;
using UnityEngine;

namespace ACS_Common.MainBoard
{
    public class MainBoardComp : ACS_Behaviour, IGCommandStreamHolder
    {
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverX;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverY;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverZ;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverE;
        
        public Action OnStreamUpdate { get; set; }
        public Action OnPrintProgressUpdate { get; set; }
        public Action OnStateChange { get; set; }

        public GCommandStream Stream { get; private set; }

        public PrinterMainBoard.PrinterMainBoardStatus Status 
        {
            get
            {
                return null == _mainBoard ? default(PrinterMainBoard.PrinterMainBoardStatus) : _mainBoard.Status;
            }
        }

        private PrinterMainBoard _mainBoard;
        private ValueChangeListener<PrinterMainBoard.PrinterMainBoardStatus.ECommandState> _mainBoardStatusState;
        private ValueChangeListener<float> _mainBoardStatusProgress;
        private ValueChangeListener<long> _mainBoardStatusLineIdx;
        
        /// <summary>
        /// 载入GCode文件
        /// </summary>
        /// <param name="gCodeFilePath"></param>
        public void LoadGCodeFile(string gCodeFilePath)
        {
            const string m = nameof(LoadGCodeFile);
            LogMethod(m, $"gCodeFilePath: {gCodeFilePath}");
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            if (string.IsNullOrEmpty(gCodeFilePath))
            {
                LogErr(m, $"gCodeFilePath is empty");
                return;
            }
            if (_mainBoard.Status.CommandState != PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Idle)
            {
                LogErr(m, $"printer state is not Idle, _mainBoard.Status.CommandState: {_mainBoard.Status.CommandState}");
                return;
            }
            Stream?.Dispose();
            Stream = new GCommandStream(gCodeFilePath);
            Stream.CacheCapacity = 500;
            _mainBoard.SetStream(Stream);
            OnStreamUpdate?.Invoke();
        }

        /// <summary>
        /// 开始打印已载入的GCode文件
        /// </summary>
        public void Execute()
        {
            const string m = nameof(Execute);
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            _mainBoard.Execute();
        }
        /// <summary>
        /// 停止打印
        /// </summary>
        public void Stop()
        {
            const string m = nameof(Stop);
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            _mainBoard.Stop();
        }

        /// <summary>
        /// 继续
        /// </summary>
        public void Continue()
        {
            const string m = nameof(Continue);
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            _mainBoard.Continue();
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            const string m = nameof(Pause);
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            _mainBoard.Pause();
        }
        protected override void Init()
        {
            base.Init();
            _mainBoard = new PrinterMainBoard();
            _mainBoard.Init();
            _mainBoardStatusState = new ValueChangeListener<PrinterMainBoard.PrinterMainBoardStatus.ECommandState>(() => OnStateChange?.Invoke(), _mainBoard.Status.CommandState);
            _mainBoardStatusLineIdx = new ValueChangeListener<long>(() => OnPrintProgressUpdate.Invoke(), _mainBoard.Status.ExecutingCommandLineIdx);
            _mainBoardStatusProgress = new ValueChangeListener<float>(() => OnPrintProgressUpdate.Invoke(), _mainBoard.Status.ExecutingProgress);
        }

        protected override void Clear()
        {
            base.Clear();
            _mainBoard?.Clear();
        }

        protected override void HeartBeat()
        {
            const string m = nameof(HeartBeat);
            // LogMethod(m, $"_mainBoard.Status.CommandState: {_mainBoard.Status.CommandState}");
            base.HeartBeat();
            _mainBoardStatusState.UpdateValue(_mainBoard.Status.CommandState);
            _mainBoardStatusLineIdx.UpdateValue(_mainBoard.Status.ExecutingCommandLineIdx);
            _mainBoardStatusProgress.UpdateValue(_mainBoard.Status.ExecutingProgress);
        }
    }
}