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
        [SerializeField] protected TextMeshProUGUI _textField;
        [SerializeField] private GCodeViewScrollBar _scrollBar;
        
        #region color define

        private const string ColorDefautlText = "EEEEEE";
        private const string ColorLineIdx = "B0A597";
        private const string ColorInvalid = "FF6666";
        
        private const string ColorCommandTypeM = "E5DAAA";
        private const string ColorCommandTypeG = "AFEFF6";
        private const string ColorCommandComment = "9BC375";
        private const string ColorCommandParamX = "FFB3A5";
        private const string ColorCommandParamY = "B2E080";
        private const string ColorCommandParamZ = "86C9FF";
        private const string ColorCommandParamE = "80E0CE";
        private const string ColorCommandParamF = "F1EE7F";
        private const string ColorCommandParamS = "E1B1FF";
        
        #endregion
        
        /// <summary>
        /// 展示行数，最小1，最大100
        /// 由UI尺寸自动计算得来
        /// </summary>
        protected int _displayLineCnt = 10;

        private StringBuilder _sb = new StringBuilder();
        private long _displayStartLineIdx = -1L;

        public MonoBehaviour GCommandStreamHolderComp;
        protected IGCommandStreamHolder _streamHolder;
        private GCommandStream _stream => _streamHolder?.Stream;
        // 上一次updateText时读取的最后位置
        private long _lastReadTextPosition;
        protected virtual long DisplayLineIdx
        {
            get => _displayStartLineIdx;
            set => UpdateTextField(value);
        }

        protected virtual void UpdateTextField(long startLineIdx)
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
            // LogInfo(m, $"realDisplayLineCnt: {realDisplayLineCnt}");
            while (i++ < realDisplayLineCnt && itr.MoveNext())
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
            var streamHolder = GCommandStreamHolderComp as IGCommandStreamHolder;
            if (null != streamHolder) SetStreamHolder(GCommandStreamHolderComp as IGCommandStreamHolder);
        }

        protected void SetStreamHolder(IGCommandStreamHolder streamHolder)
        {
            const string m = nameof(SetStreamHolder);
            LogMethod(m, $"streamHolder: {streamHolder}");
            if (null != _streamHolder)
            {
                _streamHolder.OnStreamUpdate -= OnStreamUpdate;
            }
            _streamHolder = streamHolder;
            if (null == _scrollBar)
            {
                LogErr(m, "_scrollBar reference is null");
            }
            else
            {
                _scrollBar.SetConfig(1, 1);
                _scrollBar.OnPosIndex += OnScrollPos;
            }
            if (null != _textField) _textField.text = string.Empty;

            if (null == _streamHolder)
            {
                LogErr(m, "_streamHolder is null");
                return;
            }
            if (null != _streamHolder.Stream) OnStreamUpdate();
            _streamHolder.OnStreamUpdate += OnStreamUpdate;
        }

        protected override void Clear()
        {
            base.Clear();
            if (null != _scrollBar) _scrollBar.OnPosIndex -= OnScrollPos;
            if (null != _streamHolder) _streamHolder.OnStreamUpdate -= OnStreamUpdate;
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

        protected virtual void OnStreamUpdate()
        {
            const string m = nameof(OnStreamUpdate);
            LogMethod(m);
            if (null == _streamHolder)
            {
                LogErr(m, "_streamHolder is null");
                return;
            }
            if (null == _scrollBar)
            {
                LogErr(m, "_scrollBar reference is null");
            }
            else
            {
                if (null == _stream)
                {
                    LogErr(m, "stream of stream holder is null");
                    return;
                }
                if (_stream.IndexBuilt)
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
                _displayStartLineIdx = -1;
            }
            StopAllCoroutines();
            StartCoroutine(CheckStreamIndexBuilt());
        }

        /// <summary>
        /// 每帧检查数据源索引构建情况
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckStreamIndexBuilt()
        {
            const string m = nameof(CheckStreamIndexBuilt);
            LogMethod(m);
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
            if (command.CommandType == Def.EGCommandType.Invalid)
            {
                _sbForCommandRichText.Append($"<color=#{ColorInvalid}>{command.RawStr}</color>");
                return _sbForCommandRichText.ToString();
            }
            else if (command.CommandType != Def.EGCommandType.None)
            {
                var commandTypeColor = ColorDefautlText;
                switch (command.CommandType)
                {
                    case Def.EGCommandType.G:
                        commandTypeColor = ColorCommandTypeG;
                        break;
                    case Def.EGCommandType.M:
                        commandTypeColor = ColorCommandTypeM;
                        break;
                    case Def.EGCommandType.Invalid:
                        commandTypeColor = ColorInvalid;
                        break;
                }
                _sbForCommandRichText.Append($"<color=#{commandTypeColor}>{command.CommandType}{command.Number}</color>");
                if (null != command.Params)
                {
                    foreach (var param in command.Params)
                    {
                        if (null != param)
                        {
                            var paramColor = ColorDefautlText;
                            switch (param.Name)
                            {
                                case 'X':
                                case 'x':
                                    paramColor = ColorCommandParamX;
                                    break;
                                case 'Y':
                                case 'y':
                                    paramColor = ColorCommandParamY;
                                    break;
                                case 'Z':
                                case 'z':
                                    paramColor = ColorCommandParamZ;
                                    break;
                                case 'E':
                                case 'e':
                                    paramColor = ColorCommandParamE;
                                    break;
                                case 'F':
                                case 'f':
                                    paramColor = ColorCommandParamF;
                                    break;
                                case 'S':
                                case 's':
                                    paramColor = ColorCommandParamS;
                                    break;
                            }
                            _sbForCommandRichText.Append($" <color=#{paramColor}>{param.RawStr}</color>");
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
            ForceSetScrollBarPos(targetLineIdx);
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            _dragPointY = 1f;
        }

        protected void ForceSetScrollBarPos(long lineIdx)
        {
            _scrollBar.ScrollIdx = lineIdx;
        }
        #endregion
    }
}