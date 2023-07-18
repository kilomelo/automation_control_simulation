using ACS_Common.MainBoard;
using TMPro;
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
        // 没行运行时间信息
        [SerializeField] protected TextMeshProUGUI _executeTimeTextField;

        private static readonly string[] ColorExecuteTimeText = { "6FDB50", "B8DB50", "DBD750", "DBA450", "DB6D50" };
        private static readonly long[] ColorExecuteTimeThreshold = { 125, 250, 500, 1000, long.MaxValue };
        // private const string ColorLineIdx = "B0A597";
        // private const string ColorInvalid = "FF6666";

        private MainBoardComp _mainBoardComp;
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
                // 根据目标展示行号修改跟随显示属性
                var deltaLine = _mainBoardComp.Status.ExecutingCommandLineIdx - value;
                var executingLineInDisplayRange = deltaLine >= 0 && deltaLine < _displayLineCnt;
                _locking = executingLineInDisplayRange;
                base.DisplayLineIdx = value;
            }
        }

        public void SetPrinter(MainBoardComp printer)
        {
            const string m = nameof(SetPrinter);
            if (null == printer)
            {
                LogErr(m, "null == printer");
                return;
            }
            SetStreamHolder(printer);
            _mainBoardComp = printer;
            _mainBoardComp.OnPrintProgressUpdate += OnPrintProgressUpdate;
            _mainBoardComp.OnStateChange += OnStateChange;
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
            if (null != _mainBoardComp)
            {
             _mainBoardComp.OnPrintProgressUpdate -= OnPrintProgressUpdate;
             _mainBoardComp.OnStateChange -= OnStateChange;
            }
        }

        private long _lastDeltaLine = long.MaxValue;
        // 设置指示标志的位置和滚动条指示标志的位置
        private void OnPrintProgressUpdate()
        {
            const string m = nameof(OnPrintProgressUpdate);
            // LogMethod(m);
            if (null == _mainBoardComp)
            {
                LogErr(m, "null == _mainBoardComp");
                return;
            }
            if (null != _currentLineProgress)
            {
                // LogInfo(m, $"set _currentLineProgress.fillAmount to  _mainBoardComp.Status.ExecutingProgress}");
                _currentLineProgress.fillAmount = _mainBoardComp.Status.ExecutingProgress;
            }
            var deltaLine = _mainBoardComp.Status.ExecutingCommandLineIdx - DisplayLineIdx;
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
                    
                    if (null != _printerGCodeViewScrollBar) 
                        _printerGCodeViewScrollBar.OnPrintProgressUpdate(_mainBoardComp.Status.ExecutingCommandLineIdx, deltaLine, executingLineInDisplayRange);
                }
            }
            UpdateExecuteTimeText();
        }

        private void OnStateChange()
        {
            const string m = nameof(OnStateChange);
            // LogMethod(m);
            if (null == _mainBoardComp)
            {
                LogErr(m, "null == _mainBoardComp");
                return;
            }
            // LogInfo(m, $ _mainBoardComp.Status.State:  _mainBoardComp.Status.State}");
            switch  (_mainBoardComp.Status.CommandState)
            {
                case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Idle:
                    if (null != _currentLineIndicator) _currentLineIndicator.gameObject.SetActive(false);
                    break;
                case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Printing:
                    _locking = true;
                    DisplayPrintLine();
                    break;
            }
            if (null != _printerGCodeViewScrollBar) _printerGCodeViewScrollBar.OnStateChange(_mainBoardComp.Status);
            UpdateExecuteTimeText();
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
            if (update && null != _mainBoardComp &&
             _mainBoardComp.Status.CommandState is PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Printing or
                    PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Pause)
                OnPrintProgressUpdate();
        }

        protected override void SetStreamHolder(IGCommandStreamHolder streamHolder)
        {
            base.SetStreamHolder(streamHolder);
            if (null != _executeTimeTextField) _executeTimeTextField.text = string.Empty;
        }

        /// <summary>
        /// 使当前执行命令展示在视野中
        /// </summary>
        private void DisplayPrintLine()
        {
            const string m = nameof(DisplayPrintLine);
            // LogMethod(m);
            if (null == _mainBoardComp)
            {
                LogErr(m, "null == _mainBoardComp");
                return;
            }
            var deltaLine = _mainBoardComp.Status.ExecutingCommandLineIdx - DisplayLineIdx;
            // LogInfo(m, $"ExecutingCommandLineIdx:  _mainBoardComp.Status.ExecutingCommandLineIdx}, DisplayLineIdx: {DisplayLineIdx}, deltaLine: {deltaLine}");
            if (deltaLine >= 0 && deltaLine < _displayLineCnt) return;
            if (deltaLine < 0)
            {
                var targetLineIdx = _mainBoardComp.Status.ExecutingCommandLineIdx;
                ForceSetScrollBarPos(targetLineIdx);
                UpdateTextField(targetLineIdx);
            }
            else
            {
                var targetLineIdx = _mainBoardComp.Status.ExecutingCommandLineIdx - _displayLineCnt + 1;
                ForceSetScrollBarPos(targetLineIdx);
                UpdateTextField(targetLineIdx);
            }
        }

        /// <summary>
        /// 刷新执行时间的显示
        /// </summary>
        private void UpdateExecuteTimeText()
        {
            // 更新执行时间
            if (null != _stream && null != _executeTimeTextField)
            {
                _sb.Clear();
                var i = 0;
                var realDisplayLineCnt = Mathf.Clamp(_displayLineCnt, 1, 9999);
                while (i++ < realDisplayLineCnt && (i + _displayStartLineIdx - 1) <= _mainBoardComp.Status.ExecutingCommandLineIdx)
                {
                    var cachedCommand = _stream.GetCachedCommand(i + _displayStartLineIdx - 1);
                    if (i + _displayStartLineIdx - 1 == _mainBoardComp.Status.ExecutingCommandLineIdx &&
                     _mainBoardComp.Status.CommandState !=
                        PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Pause) break;
                    if (cachedCommand is { ExecuteTimeMilliSec: > 0 })
                    {
                        var level = 0;
                        while (ColorExecuteTimeThreshold[level] < cachedCommand.ExecuteTimeMilliSec) level++;
                        _sb.Append($"<color=#{ColorExecuteTimeText[level]}>{cachedCommand.ExecuteTimeMilliSec}</color>");
                    }
                    _sb.Append('\n');
                }
                _executeTimeTextField.text = _sb.ToString();
            }
        }
    }
}