using System;
using ACS_Common.MainBoard;
using TMPro;
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
        // 跳转到当前运行行按钮
        [SerializeField] private Button _currentLineJumpBtn;

        public Action JumpBtnOnClick;
        private TextMeshProUGUI _currentLineJumpBtnTxt;
        private RectTransform _currentLineIndicatorRectTrans;

        protected override void Init()
        {
            const string m = nameof(Init);
            base.Init();
            if (null == _currentLineIndicator) LogErr(m, "null == _currentLineIndicator");
            _currentLineIndicatorRectTrans = _currentLineIndicator.transform as RectTransform;
            if (null != _currentLineJumpBtn)
            {
                _currentLineJumpBtn.onClick.AddListener(() =>
                {
                    JumpBtnOnClick?.Invoke();
                });
                _currentLineJumpBtnTxt = _currentLineJumpBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (null == _currentLineJumpBtnTxt) LogErr(m, "_currentLineJumpBtn has no TextMeshProUGUI component");
            }
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
            // LogMethod(m, $"currentExecuteLineIdx: {currentExecuteLineIdx}, deltaLineIdx: {deltaLineIdx}, executingLineInDisplayRange: {executingLineInDisplayRange}");
            if (null == _rectTransform) return;
            if (null == _currentLineIndicator) return;
            if (null == _handler) return;
            if (null != _currentLineJumpBtn) _currentLineJumpBtn.gameObject.SetActive(!executingLineInDisplayRange);
            var localPos = _currentLineIndicatorRectTrans.localPosition;
            if (executingLineInDisplayRange)
            {
                var size = _currentLineIndicatorRectTrans.sizeDelta;
                size.y = Math.Max(1f, _handlerHeight / _content);
                _currentLineIndicatorRectTrans.sizeDelta = size;
                localPos.y = CalcLerpPos(_handler.localPosition.y,
                    _handler.localPosition.y - (_handler.rect.height - size.y),
                    _content - 1, deltaLineIdx);
            }
            else
            {
                var size = _currentLineIndicatorRectTrans.sizeDelta;
                // LogInfo(m, $"size: {size}");
                var realSize = (_rectTransform.rect.height - _handlerHeight) / (_total - _content);
                size.y = Math.Max(1f, realSize);
                // LogInfo(m, $"_rectTransform.rect.height: {_rectTransform.rect.height}, _handlerHeight: {_handlerHeight}, _rectTransform.sizeDelta.y - _handlerHeight: {_rectTransform.rect.height - _handlerHeight}, _total - _content: {_total - _content}, size.y: {size.y}");
                _currentLineIndicatorRectTrans.sizeDelta = size;
                localPos.y = deltaLineIdx < 0 ? -currentExecuteLineIdx * realSize :
                    -(_rectTransform.rect.height - (_total - currentExecuteLineIdx) * realSize);
                if (null != _currentLineJumpBtnTxt) _currentLineJumpBtnTxt.text = currentExecuteLineIdx.ToString();
            }
            _currentLineIndicatorRectTrans.localPosition = localPos;
            if (null != _currentLineJumpBtn)
            {
                var btnPos = _currentLineJumpBtn.transform.localPosition;
                btnPos.y = localPos.y;
                var btnHeight = 24f;
                if (-btnPos.y > _rectTransform.rect.height - btnHeight) btnPos.y = -(_rectTransform.rect.height - btnHeight);
                _currentLineJumpBtn.transform.localPosition = btnPos;
            }
        }

        public void OnStateChange(PrinterMainBoard.PrinterMainBoardStatus status)
        {
            switch (status.CommandState)
            {
                case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Idle:
                    if (null != _currentLineIndicator) _currentLineIndicator.gameObject.SetActive(false);
                    if (null != _currentLineJumpBtn) _currentLineJumpBtn.gameObject.SetActive(false);
                    break;
                case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Printing:
                    if (null != _currentLineIndicator) _currentLineIndicator.gameObject.SetActive(true);
                    break;
            }
        }
    }
}