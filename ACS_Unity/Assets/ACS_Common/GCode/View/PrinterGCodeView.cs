using ACS_Common.MainBoard;
using UnityEngine;
using UnityEngine.UI;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// 打印机的GCodeView，可指示当前打印任务运行的指令行，自动跟随打印任务进度滚动视窗
    /// </summary>
    public class PrinterGCodeView : GCodeView
    {
        // 当前运行指令标志
        [SerializeField] private Image _currentLineIndicator;
        // 当前运行指令进度
        [SerializeField] private Image _currentLineProgress;

        private PrinterMainBoard _printerMainBoard;
        private PrinterGCodeViewScrollBar _printerGCodeViewScrollBar;
        /// <summary>
        /// 是否跟随当前打印进度滚动
        /// </summary>
        private bool _locking = true;

        protected override long DisplayLineIdx
        {
            get => base.DisplayLineIdx;
            set
            {
                var deltaLine = _printerMainBoard.Status.ExecutingCommandLineIdx - value;
                var executingLineInDisplayRange = deltaLine >= 0 && deltaLine < _displayLineCnt;
                _locking = executingLineInDisplayRange;
                base.DisplayLineIdx = value;
            }
        }

        public void SetPrinter(PrinterMainBoard printer)
        {
            const string m = nameof(SetPrinter);
            if (null == printer)
            {
                LogErr(m, "null == printer");
                return;
            }
            SetStreamHolder(printer);
            _printerMainBoard = printer;
            _printerMainBoard.OnPrintProgressUpdate += OnPrintProgressUpdate;
            _printerMainBoard.OnStateChange += OnStateChange;
        }

        protected override void Init()
        {
            base.Init();
            _printerGCodeViewScrollBar = _scrollBar as PrinterGCodeViewScrollBar;
            if (_printerGCodeViewScrollBar != null) _printerGCodeViewScrollBar.JumpBtnOnClick += DisplayPrintLine;
        }

        protected override void Clear()
        {
            base.Clear();
            if (null != _printerMainBoard)
            {
                _printerMainBoard.OnPrintProgressUpdate -= OnPrintProgressUpdate;
                _printerMainBoard.OnStateChange -= OnStateChange;
            }
        }

        private long _lastDeltaLine = long.MaxValue;
        // 设置指示标志的位置和滚动条指示标志的位置
        private void OnPrintProgressUpdate()
        {
            const string m = nameof(OnPrintProgressUpdate);
            // LogMethod(m);
            if (null == _printerMainBoard)
            {
                LogErr(m, "null == _printerMainBoard");
                return;
            }
            if (null != _currentLineProgress)
            {
                // LogInfo(m, $"set _currentLineProgress.fillAmount to {_printerMainBoard.Status.ExecutingProgress}");
                _currentLineProgress.fillAmount = _printerMainBoard.Status.ExecutingProgress;
            }
            var deltaLine = _printerMainBoard.Status.ExecutingCommandLineIdx - DisplayLineIdx;
            // LogInfo(m, $"deltaLine: {deltaLine}");
            if (deltaLine == _lastDeltaLine) return;
            _lastDeltaLine = deltaLine;
            var executingLineInDisplayRange = deltaLine >= 0 && deltaLine < _displayLineCnt;
            // LogInfo(m, $"executingLineInDisplayRange: {executingLineInDisplayRange}");
            if (null != _currentLineIndicator)
            {
                if (_locking && !executingLineInDisplayRange)
                {
                    DisplayPrintLine();
                }
                else
                {
                    // 如果非跟随状态下当前执行行进入显示范围，则显示状态变为跟随
                    _locking |= executingLineInDisplayRange;
                    _currentLineIndicator.gameObject.SetActive(deltaLine >= 0 && deltaLine < _displayLineCnt);
                    var localPos = _currentLineIndicator.transform.localPosition;
                    localPos.y = -deltaLine * _textField.fontSize;
                    // LogInfo(m, $"localPos: {localPos}");
                    _currentLineIndicator.transform.localPosition = localPos;
                    
                    if (null != _printerGCodeViewScrollBar) _printerGCodeViewScrollBar.OnPrintProgressUpdate(_printerMainBoard.Status.ExecutingCommandLineIdx, deltaLine, executingLineInDisplayRange);
                }
            }
        }

        private void OnStateChange()
        {
            const string m = nameof(OnStateChange);
            // LogMethod(m);
            if (null == _printerMainBoard)
            {
                LogErr(m, "null == _printerMainBoard");
                return;
            }
            // LogInfo(m, $"_printerMainBoard.Status.State: {_printerMainBoard.Status.State}");
            switch (_printerMainBoard.Status.State)
            {
                case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Idle:
                    if (null != _currentLineIndicator) _currentLineIndicator.gameObject.SetActive(false);
                    break;
                case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Printing:
                    _locking = true;
                    DisplayPrintLine();
                    break;
            }
            if (null != _printerGCodeViewScrollBar) _printerGCodeViewScrollBar.OnStateChange(_printerMainBoard.Status);
        }

        protected override void OnStreamUpdate()
        {
            base.OnStreamUpdate();
            if (null != _currentLineIndicator)
            {
                _currentLineIndicator.gameObject.SetActive(false);
            }
        }

        protected override void UpdateTextField(long startLineIdx)
        {
            const string m = nameof(UpdateTextField);
            // LogMethod(m, $"startLineIdx: {startLineIdx}, DisplayLineIdx: {DisplayLineIdx}");
            var update = DisplayLineIdx != startLineIdx;
            base.UpdateTextField(startLineIdx);
            // LogInfo(m, $"update: {update}");
            if (update && null != _printerMainBoard &&
                _printerMainBoard.Status.State is PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Printing or
                    PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Pause)
                OnPrintProgressUpdate();
        }

        private void DisplayPrintLine()
        {
            const string m = nameof(DisplayPrintLine);
            // LogMethod(m);
            if (null == _printerMainBoard)
            {
                LogErr(m, "null == _printerMainBoard");
                return;
            }
            var deltaLine = _printerMainBoard.Status.ExecutingCommandLineIdx - DisplayLineIdx;
            // LogInfo(m, $"ExecutingCommandLineIdx: {_printerMainBoard.Status.ExecutingCommandLineIdx}, DisplayLineIdx: {DisplayLineIdx}, deltaLine: {deltaLine}");
            if (deltaLine >= 0 && deltaLine < _displayLineCnt) return;
            if (deltaLine < 0)
            {
                var targetLineIdx = _printerMainBoard.Status.ExecutingCommandLineIdx;
                ForceSetScrollBarPos(targetLineIdx);
                UpdateTextField(targetLineIdx);
            }
            else
            {
                var targetLineIdx = _printerMainBoard.Status.ExecutingCommandLineIdx - _displayLineCnt + 1;
                ForceSetScrollBarPos(targetLineIdx);
                UpdateTextField(targetLineIdx);
            }
        }
    }
}