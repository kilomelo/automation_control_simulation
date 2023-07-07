using System;
using ACS_Common.MainBoard;
using UnityEngine;
using UnityEngine.UI;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// 打印机的GCodeViewScrollBar，可指示当前打印任务运行的指令行
    /// </summary>
    public class PrinterGCodeViewScrollBar : GCodeViewScrollBar
    {
        // 当前运行指令标志
        [SerializeField] private Image _currentLineIndicator;

        private RectTransform _currentLineIndicatorRectTrans;

        protected override void Init()
        {
            const string m = nameof(Init);
            base.Init();
            if (null == _currentLineIndicator) LogErr(m, "null == _currentLineIndicator");
            _currentLineIndicatorRectTrans = _currentLineIndicator.transform as RectTransform;
        }

        /// <summary>
        /// 当前任务执行行号发生变化时调用，用于更新指示标识的显示
        /// </summary>
        /// <param name="currentExecuteLineIdx">当前执行行号</param>
        /// <param name="deltaLineIdx">当前执行行号与展示的第一行的插值</param>
        /// <param name="executingLineInDisplayRange">当前执行行是否在展示范围中</param>
        public void OnPrintProgressUpdate(long currentExecuteLineIdx, long deltaLineIdx, bool executingLineInDisplayRange)
        {
            const string m = nameof(OnPrintProgressUpdate);
            LogMethod(m, $"currentExecuteLineIdx: {currentExecuteLineIdx}, deltaLineIdx: {deltaLineIdx}, executingLineInDisplayRange: {executingLineInDisplayRange}");
            if (null == _rectTransform) return;
            if (null == _currentLineIndicator) return;
            if (null == _handler) return;
            if (executingLineInDisplayRange)
            {
                var localPos = _currentLineIndicatorRectTrans.localPosition;
                localPos.y = _handler.transform.localPosition.y - deltaLineIdx * _handlerHeight / _content;
                _currentLineIndicatorRectTrans.localPosition = localPos;
                var size = _currentLineIndicatorRectTrans.sizeDelta;
                size.y = Math.Max(1f, _handlerHeight / _content);
                _currentLineIndicatorRectTrans.sizeDelta = size;
            }
            else
            {
                if (deltaLineIdx < 0)
                {
                    var localPos = _currentLineIndicatorRectTrans.localPosition;
                    localPos.y = currentExecuteLineIdx * (_rectTransform.sizeDelta.y - _handlerHeight) / _content;
                    // LogInfo(m, $"localPos.y: {localPos.y}");
                    _currentLineIndicatorRectTrans.localPosition = localPos;
                }
                else
                {
                    var localPos = _currentLineIndicatorRectTrans.localPosition;
                    localPos.y = -_rectTransform.rect.height;// + deltaLineIdx * _rectTransform.sizeDelta.y / _content;
                    LogInfo(m, $"localPos.y: {localPos.y}, _rectTransform.rect: {_rectTransform.rect}");
                    _currentLineIndicatorRectTrans.localPosition = localPos;
                }
                var size = _currentLineIndicatorRectTrans.sizeDelta;
                size.y = Math.Max(2f, (_rectTransform.sizeDelta.y - _handlerHeight) / _total);
                _currentLineIndicatorRectTrans.sizeDelta = size;
            }
        }

        public void OnStateChange(PrinterMainBoard.PrinterMainBoardStatus status)
        {
            switch (status.State)
            {
                case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Idle:
                    if (null != _currentLineIndicator) _currentLineIndicator.gameObject.SetActive(false);
                    break;
                case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Printing:
                    if (null != _currentLineIndicator) _currentLineIndicator.gameObject.SetActive(true);
                    break;
            }
        }
    }
}