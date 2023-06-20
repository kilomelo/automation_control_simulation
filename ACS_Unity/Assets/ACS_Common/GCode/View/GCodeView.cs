using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// unity的gcode vide实现
    /// </summary>
    public class GCodeView : MonoBehaviour
    {
        private const string Tag = nameof(GCodeView);

        [SerializeField] private TextMeshProUGUI _textField;
        [SerializeField] private GCodeViewScrollBar _scrollBar;
        [SerializeField]
        private float _width = 200f;
        [SerializeField]
        private float _height = 200f;
        [SerializeField]
        private float _scrollBarWidth = 100f;
        /// <summary>
        /// 展示行数，最小1，最大100
        /// </summary>
        [SerializeField]
        private int _displayLineCnt = 10;

        private StringBuilder _sb = new StringBuilder();
        private long _displayStartLineIdx = -1L;

        public MonoBehaviour GCommandStreamHolderComp;
        private IGCommandStreamHolder _streamHolder;
        private GCommandStream _stream => _streamHolder?.Stream;
        private long DisplayLineIdx
        {
            set
            {
                UpdateTextField(value);
            }
        }

        private void UpdateTextField(long startLineIdx)
        {
            const string m = nameof(UpdateTextField);
            if (startLineIdx == _displayStartLineIdx) return;
            LogMethod(m, $"startLineIdx: {startLineIdx}");
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
            using var itr = _stream.GetEnumerator(startLineIdx);
            var i = 0;
            var realDisplayLineCnt = Mathf.Clamp(_displayLineCnt, 1, 100);
            while (itr.MoveNext() && i++ < realDisplayLineCnt)
            {
                // LogInfo(m, $"_sb.Append({itr.Current})");
                _sb.Append($"<color=#{{3D8AFF}}>{(i + startLineIdx).ToString().PadRight(maxLineIdxLen)}</color> {itr.Current}\n");
            }
            while (i++ <= realDisplayLineCnt)
            {
                // LogInfo(m, $"_sb.Append(\\n)");
                _sb.Append("\r\n");
            }
            // LogInfo(m, $"display text:\n{_sb}");
            _textField.text = _sb.ToString();
        }

        void Start()
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
            }
            StartCoroutine(CheckStreamIndexBuilt());
        }

        private void OnDestroy()
        {
            if (null != _scrollBar) _scrollBar.OnPosIndex -= OnScrollPos;
        }

        void Update()
        {
            // DisplayLineIdx = Mathf.FloorToInt(Time.realtimeSinceStartup);
        }

        private void OnScrollPos(long index)
        {
            const string m = nameof(OnScrollPos);
            LogMethod(m, $"index: {index}");
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

        private IEnumerator CheckStreamIndexBuilt()
        {
            const string m = nameof(CheckStreamIndexBuilt);
            LogMethod(m);
            if (null == _stream)
            {
                LogErr(m, "stream holder reference is null");
                yield break;
            }

            while (!_stream.IndexBuilt)
            {
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


        protected void LogMethod(string methodName, string info = null)
        {
            Debug.Log($"# {Tag} # <{methodName}> {info} //--------------------------------------------------------------------------");
        }
        
        protected void LogInfo(string methodName, string info)
        {
            Debug.Log($"# {Tag} # <{methodName}> {info}");
        }

        protected void LogErr(string methodName, string info)
        {
            Debug.LogError($"# {Tag} # <{methodName}> {info}");
        }
        
        protected void LogWarn(string methodName, string info)
        {
            Debug.LogWarning($"# {Tag} # <{methodName}> {info}");
        }
    }
}