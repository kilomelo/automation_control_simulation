using ACS_Common.MainBoard;
using UnityEngine;
using UnityEngine.UI;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// 打印机的GCodeView
    /// </summary>
    public class PrinterGCodeView : GCodeView
    {
        [SerializeField] private Image _currentLineIndicator;
        [SerializeField] private Image _currentLineProgress;

        private PrinterMainBoard _printerMainBoard;
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

        protected override void Clear()
        {
            base.Clear();
            if (null != _printerMainBoard)
            {
                _printerMainBoard.OnPrintProgressUpdate -= OnPrintProgressUpdate;
                _printerMainBoard.OnStateChange -= OnStateChange;
            }
        }

        private void OnPrintProgressUpdate()
        {
            const string m = nameof(OnPrintProgressUpdate);
            // LogMethod(m);
            if (null == _printerMainBoard)
            {
                LogErr(m, "null == _printerMainBoard");
                return;
            }

            if (null != _currentLineIndicator)
            {
                var deltaLine = _printerMainBoard.Status.ExecutingCommandLineIdx - DisplayLineIdx;
                var executingLineInDisplayRange = deltaLine >= 0 && deltaLine < _displayLineCnt;
                if (_locking && !executingLineInDisplayRange)
                {
                    DisplayPrintLine();
                }
                else
                {
                    // 如果非跟随状态下当前执行行进入显示范围，则显示状态变为跟随
                    _locking |= executingLineInDisplayRange;
                    _currentLineIndicator.gameObject.SetActive(deltaLine >= 0 && deltaLine < _displayLineCnt);
                    // LogInfo(m, $"deltaLine: {deltaLine}");
                    var localPos = _currentLineIndicator.transform.localPosition;
                    localPos.y = -deltaLine * _textField.fontSize;
                    // LogInfo(m, $"localPos: {localPos}");
                    _currentLineIndicator.transform.localPosition = localPos;
                }
            }
            if (null != _currentLineProgress)
            {
                // LogInfo(m, $"set _currentLineProgress.fillAmount to {_printerMainBoard.Status.ExecutingProgress}");
                _currentLineProgress.fillAmount = _printerMainBoard.Status.ExecutingProgress;
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
            if (null == _printerMainBoard)
            {
                LogErr(m, "null == _printerMainBoard");
                return;
            }
            var deltaLine = _printerMainBoard.Status.ExecutingCommandLineIdx - DisplayLineIdx;
            // LogInfo(m, $"deltaLine: {deltaLine}");
            if (deltaLine >= 0 && deltaLine < _displayLineCnt) return;
            if (deltaLine < 0)
            {
                var targetLineIdx = _printerMainBoard.Status.ExecutingCommandLineIdx + 1;
                UpdateTextField(targetLineIdx);
                ForceSetScrollBarPos(targetLineIdx);
            }
            else
            {
                var targetLineIdx = _printerMainBoard.Status.ExecutingCommandLineIdx - _displayLineCnt + 1;
                UpdateTextField(targetLineIdx);
                ForceSetScrollBarPos(targetLineIdx);
            }
        }
    }
}