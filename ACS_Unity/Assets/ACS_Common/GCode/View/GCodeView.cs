using System;
using System.Collections;
using System.Text;
using ACS_Common.Base;
using ACS_Common.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// unity的gcode vide实现
    /// </summary>
    public partial class GCodeView : ViewBase, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private TextMeshProUGUI _textField;
        [SerializeField] private GCodeViewScrollBar _scrollBar;
        [SerializeField]
        private float _width = 200f;
        [SerializeField]
        private float _height = 200f;
        [SerializeField]
        private float _scrollBarWidth = 100f;
        
        #region color define

        private const string ColorLineIdx = "BCAA94";
        private const string ColorCommandType = "E5DAAA";
        private const string ColorCommandNumber = "F8EFC9";
        private const string ColorCommandComment = "9BC375";
        private const string ColorCommandParam = "ADD9EF";
        
        #endregion
        
        /// <summary>
        /// 展示行数，最小1，最大100
        /// 由UI尺寸自动计算得来
        /// </summary>
        private int _displayLineCnt = 10;

        private StringBuilder _sb = new StringBuilder();
        private long _displayStartLineIdx = -1L;

        public MonoBehaviour GCommandStreamHolderComp;
        private IGCommandStreamHolder _streamHolder;
        private GCommandStream _stream => _streamHolder?.Stream;
        // 上一次updateText时读取的最后位置
        private long _lastReadTextPosition;
        private long DisplayLineIdx
        {
            set => UpdateTextField(value);
        }

        private void UpdateTextField(long startLineIdx)
        {
            const string m = nameof(UpdateTextField);
            startLineIdx = Math.Clamp(startLineIdx, 0, Math.Max(0, _stream.TotalLines - _displayLineCnt));
            if (startLineIdx == _displayStartLineIdx) return;
            // LogMethod(m, $"startLineIdx: {startLineIdx}");
            if (null == _textField)
            {
                LogErr(m, "text field reference is null");
                return;
            }
            if (null == _stream)
            {
                LogErr(m, "stream holder reference is null");
                return;
            }

            if (!_stream.IndexBuilt)
            {
                LogWarn(m, "stream index not built");
                return;
            }
            _displayStartLineIdx = startLineIdx;
            // 计算行号最大位数
            var lastLineIdx = startLineIdx + _displayLineCnt;
            var maxLineIdxLen = 1;
            // LogInfo(m, $"let's find line idx len, startLineIdx: {startLineIdx}, _displayLineCnt: {_displayLineCnt}, lastLineIdx: {lastLineIdx}");
            while (lastLineIdx > 9)
            {
                lastLineIdx = ((lastLineIdx * 1374389535) >> 37) - (lastLineIdx >> 31);
                // LogInfo(m, $"loop, maxLineIdxLen: {maxLineIdxLen} lastLineIdx: {lastLineIdx}");
                maxLineIdxLen += 2;
            }
            if (lastLineIdx == 0) maxLineIdxLen--;
            // LogInfo(m, $"calc line idx len finish, maxLineIdxLen: {maxLineIdxLen}, lastLineIdx: {lastLineIdx}");
            
            _sb.Clear();
            var i = 0;
            var realDisplayLineCnt = Mathf.Clamp(_displayLineCnt, 1, 9999);
            using var itr = _stream.GetEnumerator(startLineIdx);
            LogInfo(m, $"realDisplayLineCnt: {realDisplayLineCnt}");
            while (itr.MoveNext() && i++ < realDisplayLineCnt)
            {
                // LogInfo(m, $"i: {i}, _sb.Append({itr.Current})");
                // LogInfo(m, $"line idx str: len: {(i + startLineIdx).ToString().PadRight(maxLineIdxLen).Length}, content: [{(i + startLineIdx).ToString().PadRight(maxLineIdxLen)}]");
                _sb.Append($"<i><color=#{ColorLineIdx}>{(i + startLineIdx).ToString().PadRight(maxLineIdxLen)}</color></i>  {CommandRichText(itr.Current)}\n");
            }
            
            // // 补充空行
            // while (i++ <= realDisplayLineCnt)
            // {
            //     // LogInfo(m, $"_sb.Append(\\n)");
            //     _sb.Append("\r\n");
            // }
            // LogInfo(m, $"display text:\n{_sb}");
            _textField.text = _sb.ToString();
        }

        private void Start()
        {
            const string m = nameof(Start);
            
            _streamHolder = GCommandStreamHolderComp as IGCommandStreamHolder;
            if (null == _streamHolder)
            {
                LogErr(m, "GCommandStreamHolderComp is not IGCommandStreamHolder");
            }
            if (null == _scrollBar)
            {
                LogErr(m, "_scrollBar reference is null");
            }
            else
            {
                _scrollBar.OnPosIndex += OnScrollPos;
                if (null == _stream)
                {
                    LogErr(m, "stream of stream holder is null");
                }
                else if (_stream.IndexBuilt)
                {
                    _scrollBar.SetConfig(_displayLineCnt, _stream.TotalLines);
                }
                else
                {
                    _scrollBar.SetConfig(1, 1);
                }
            }
            if (null == _textField)
            {
                LogErr(m, "text field reference is null");
            }
            else
            {
                _textField.text = string.Empty;
                _displayLineCnt = Mathf.FloorToInt(_rectTransform.rect.height / _textField.fontSize);
            }
            StartCoroutine(CheckStreamIndexBuilt());
        }

        private void OnDestroy()
        {
            if (null != _scrollBar) _scrollBar.OnPosIndex -= OnScrollPos;
        }

        private void OnScrollPos(long index)
        {
            const string m = nameof(OnScrollPos);
            // LogMethod(m, $"index: {index}");
            if (null == _stream)
            {
                LogErr(m, "stream holder reference is null");
                return;
            }
            if (_stream.IndexBuilt)
            {
                DisplayLineIdx = index;
            }
        }

        /// <summary>
        /// 每帧检查数据源索引构建情况
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckStreamIndexBuilt()
        {
            const string m = nameof(CheckStreamIndexBuilt);
            LogMethod(m, $"getType.Name: {GetType().Name}");
            if (null == _stream)
            {
                LogErr(m, "stream holder reference is null");
                yield break;
            }

            // StringUtils.Test_ProgressBar();
            while (!_stream.IndexBuilt)
            {
                // LogInfo(m, $"build index progress: {_stream.IndexBuildProgress}");
                if (null != _textField)
                {
                    _textField.text = $"building index [{StringUtils.ProgressBar(_stream.IndexBuildProgress)}]";
                }
                yield return 0;
            }
            if (null == _stream)
            {
                LogErr(m, "stream of stream holder is null");
            }
            else if (_stream.IndexBuilt)
            {
                _scrollBar.SetConfig(_stream.TotalLines, _displayLineCnt);
            }
            DisplayLineIdx = 0;
        }

        private StringBuilder _sbForCommandRichText = new StringBuilder();
        private string CommandRichText(GCommand command)
        {
            if (null == command) return "<color=#{{FF9999}}>null</color>";
            _sbForCommandRichText.Clear();
            if (command.CommandType != Def.EGCommandType.None)
            {
                _sbForCommandRichText.Append($"<color=#{ColorCommandType}>{command.CommandType}</color><color=#{ColorCommandNumber}>{command.Number}</color>");
                if (null != command.Params)
                {
                    foreach (var param in command.Params)
                    {
                        if (null != param)
                        {
                            _sbForCommandRichText.Append($" <color=#{ColorCommandParam}>{param.Name}{param.Value}</color>");
                        }
                    }
                }
                if (null != command.Comment)
                {
                    _sbForCommandRichText.Append(' ');
                }
            }

            if (null != command.Comment)
            {
                _sbForCommandRichText.Append($"<color=#{ColorCommandComment}>;{command.Comment}</color>");
            }
            return _sbForCommandRichText.ToString();
        }
        #region interactive
        private float _dragPointY;
        private long _dragStartDisplayLine;
        private void ScrollDown()
        {
            DisplayLineIdx = _displayStartLineIdx - 1;
        }

        private void ScrollUp()
        {
            DisplayLineIdx = _displayStartLineIdx + 1;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            const string m = nameof(OnPointerDown);
            // LogMethod(m, $"eventData: {eventData}");
            if (null == _textField) return;
            if (!_stream.IndexBuilt) return;
            _dragPointY = EventPosY2LocalPosY(eventData.position.y);
            _dragStartDisplayLine = _displayStartLineIdx;
        }

        public void OnDrag(PointerEventData eventData)
        {
            const string m = nameof(OnDrag);
            // LogMethod(m, $"eventData: {eventData}");
            if (null == _textField) return;
            if (!_stream.IndexBuilt) return;
            if (_dragPointY > 0f) return;
            var deltaY = EventPosY2LocalPosY(eventData.position.y) - _dragPointY;
            // LogInfo(m, $"deltaY: {deltaY}, _textField.fontSize: {_textField.fontSize}");
            var deltaLine = deltaY / _textField.fontSize;
            var targetLineIdx = _dragStartDisplayLine + (long)Math.Floor(deltaLine);
            OnScrollPos(targetLineIdx);
            _scrollBar.ScrollIdx = targetLineIdx;
        }
        #endregion

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragPointY = 1f;
        }
    }
}