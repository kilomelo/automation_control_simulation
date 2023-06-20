using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ACS_Common.GCode.View
{
    [RequireComponent(typeof(RectTransform))]
    public class GCodeViewScrollBar : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler
    {
        private const string Tag = nameof(GCodeViewScrollBar);
        [SerializeField] private RectTransform _handler;
        public bool SnapHandler = false;
        public float MinHandlerHeight = 1f;
        private RectTransform _rectTransform;
        public Action<float> OnPosPercentage;
        public Action<long> OnPosIndex;

        private long _total;
        private long _content;
        private float _handlerHeight => null == _handler ? 0f : _handler.rect.height;
        public void SetConfig(long total, long content)
        {
            const string m = nameof(SetConfig);
            _total = Math.Max(content, total);
            _content = content;
            if (null != _handler && null != _rectTransform)
            {
                var handlerSize = _handler.sizeDelta;
                var rect = _rectTransform.rect;
                var newSize = new Vector2(handlerSize.x, Math.Max(rect.height * ((float)_content / _total), MinHandlerHeight));
                LogInfo(m, $"new size: {newSize}, (float)_content / _total: {(float)_content / _total}, rect.height: {rect.height}");
                _handler.sizeDelta = newSize;
            }
        }
        
        void Awake()
        {
            _rectTransform = transform as RectTransform;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            const string m = nameof(OnPointerDown);
            LogMethod(m, $"eventData: {eventData}");
            
            var inHandler = PointInHandler(eventData.position);
            if (!inHandler)
            {
                var localY = EventPosY2LocalPosY(eventData.position.y) + _handlerHeight * 0.5f;
                LogInfo(m, $"localY: {localY}, percent: {Position2Percentage(localY)}");
                OnScrollPosChanged(Position2Percentage(localY));
                _dragPointInHandlerY = _handlerHeight * 0.5f;
            }
            else
            {
                _dragPointInHandlerY = 0f;
                if (null == _handler) return;
                if (null == _rectTransform) return;
                _dragPointInHandlerY = -_handler.InverseTransformPoint(eventData.position).y;
                LogInfo(m, $"_dragPointInHandlerY: {_dragPointInHandlerY}");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            const string m = nameof(OnDrag);
            LogMethod(m, $"eventData: {eventData}, _dragPointInHandlerY: {_dragPointInHandlerY}");
            var localY = EventPosY2LocalPosY(eventData.position.y) + _dragPointInHandlerY;
            OnScrollPosChanged(Position2Percentage(localY));
        }

        private float _dragPointInHandlerY;
        public void OnBeginDrag(PointerEventData eventData)
        {
            const string m = nameof(OnBeginDrag);
            LogMethod(m, $"eventData: {eventData}");
        }
        
        private void OnScrollPosChanged(float scrollPosPercentage)
        {
            const string m = nameof(OnScrollPosChanged);
            LogMethod(m, $"scrollPosPercentage: {scrollPosPercentage}, _total: {_total}, _content: {_content}");
            OnPosPercentage?.Invoke(scrollPosPercentage);
            var index = Math.Clamp((long)((_total - _content + 1) * scrollPosPercentage), 0L, _total - _content);
            OnPosIndex?.Invoke(index);
            if (null != _handler)
            {
                // LogInfo(m, $"_handler.pos: {_handler.localPosition}");
                var rect = _rectTransform.rect;
                var pos = _handler.localPosition;
                pos.y = SnapHandler ?
                        -(rect.height - _handler.rect.height) * (float)index / Math.Max(_total - _content, 1L) :
                        -(rect.height - _handler.rect.height) * scrollPosPercentage;
                _handler.localPosition = pos;
            }
        }

        private bool PointInHandler(Vector2 point)
        {
            const string m = nameof(OnScrollPosChanged);
            if (null == _handler) return false;
            if (null == _rectTransform) return false;
            var handlerSize = _handler.sizeDelta;
            var p = _handler.InverseTransformPoint(point);
            return -p.y > 0f && -p.y <= handlerSize.y;
        }

        /// <summary>
        /// 将点事件数据转为本地坐标
        /// </summary>
        /// <param name="eventPosY"></param>
        /// <returns></returns>
        private float EventPosY2LocalPosY(float eventPosY)
        {
            const string m = nameof(EventPosY2LocalPosY);
            LogMethod(m, $"eventPosY: {eventPosY}");
            if (null == _rectTransform)
            {
                LogErr(m, "_rectTransform is null");
                return 0f;
            }
            return _rectTransform.InverseTransformPoint(Vector2.up * eventPosY).y;
        }
        /// <summary>
        /// 将点事件数据转为本地坐标比例
        /// </summary>
        /// <param name="localPosY"></param>
        /// <returns></returns>
        private float Position2Percentage(float localPosY)
        {
            const string m = nameof(Position2Percentage);
            LogMethod(m, $"localPosY: {localPosY}");
            var rect = _rectTransform.rect;
            return Math.Clamp(-localPosY / (rect.height - _handlerHeight), 0f, 1f);
        }
        
        protected void LogMethod(string methodName, string info = null)
        {
            // Debug.Log($"# {Tag} # <{methodName}> {info} //--------------------------------------------------------------------------");
        }
        
        protected void LogInfo(string methodName, string info)
        {
            // Debug.Log($"# {Tag} # <{methodName}> {info}");
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