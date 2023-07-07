using System;
using ACS_Common.Base;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ACS_Common.GCode.View
{
    [RequireComponent(typeof(RectTransform))]
    public class GCodeViewScrollBar : ViewBase, IPointerDownHandler, IDragHandler, IBeginDragHandler
    {
        [SerializeField] protected RectTransform _handler;
        public bool SnapHandler = false;
        public float MinHandlerHeight = 1f;
        public Action<float> OnPosPercentage;
        public Action<long> OnPosIndex;

        /// <summary>
        /// 设置滚动位置
        /// </summary>
        public long ScrollIdx
        {
            set => SetScrollIdx(value);
        }

        protected long _total;
        protected long _content;
        protected float _handlerHeight => null == _handler ? 0f : _handler.rect.height;
        public void SetConfig(long total, long content)
        {
            const string m = nameof(SetConfig);
            // LogMethod(m, $"total: {_total}, content: {content}, getType.Name: {GetType().Name}");
            _total = Math.Max(content, total);
            _content = content;
            if (null != _handler && null != _rectTransform)
            {
                var handlerSize = _handler.sizeDelta;
                var rect = _rectTransform.rect;
                var newSize = new Vector2(handlerSize.x, Math.Max(rect.height * ((float)_content / _total), MinHandlerHeight));
                // LogInfo(m, $"new size: {newSize}, (float)_content / _total: {(float)_content / _total}, rect.height: {rect.height}");
                _handler.sizeDelta = newSize;
                var localPos = _handler.localPosition;
                localPos.y = 0f;
                _handler.localPosition = localPos;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            const string m = nameof(OnPointerDown);
            // LogMethod(m, $"eventData: {eventData}");
            
            var inHandler = PointInHandler(eventData.position);
            if (!inHandler)
            {
                var localY = EventPosY2LocalPosY(eventData.position.y) + _handlerHeight * 0.5f;
                // LogInfo(m, $"localY: {localY}, percent: {Position2Percentage(localY)}");
                OnScrollPosChanged(Position2Percentage(localY));
                _dragPointInHandlerY = _handlerHeight * 0.5f;
            }
            else
            {
                _dragPointInHandlerY = 0f;
                if (null == _handler) return;
                if (null == _rectTransform) return;
                _dragPointInHandlerY = -_handler.InverseTransformPoint(eventData.position).y;
                // LogInfo(m, $"_dragPointInHandlerY: {_dragPointInHandlerY}");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            const string m = nameof(OnDrag);
            // LogMethod(m, $"eventData: {eventData}, _dragPointInHandlerY: {_dragPointInHandlerY}");
            var localY = EventPosY2LocalPosY(eventData.position.y) + _dragPointInHandlerY;
            OnScrollPosChanged(Position2Percentage(localY));
        }

        private float _dragPointInHandlerY;
        public void OnBeginDrag(PointerEventData eventData)
        {
            const string m = nameof(OnBeginDrag);
            // LogMethod(m, $"eventData: {eventData}");
        }
        
        private void OnScrollPosChanged(float scrollPosPercentage)
        {
            const string m = nameof(OnScrollPosChanged);
            // LogMethod(m, $"scrollPosPercentage: {scrollPosPercentage}, _total: {_total}, _content: {_content}");
            OnPosPercentage?.Invoke(scrollPosPercentage);
            var index = Math.Clamp((long)((_total - _content + 1) * scrollPosPercentage), 0L, Math.Max(_total - _content, 1L));
            // LogInfo(m, $"index: {index}");
            if (null != _handler)
            {
                // LogInfo(m, $"_handler.pos: {_handler.localPosition}");
                var rect = _rectTransform.rect;
                var pos = _handler.localPosition;
                pos.y = SnapHandler ?
                    -(rect.height - _handler.rect.height) * (float)index / Math.Max(_total - _content, 1L) :
                    -(rect.height - _handler.rect.height) * scrollPosPercentage;
                // LogInfo(m, $"_handler.pos.y: {_handler.localPosition.y}, new y: {pos.y}");
                _handler.localPosition = pos;
            }
            OnPosIndex?.Invoke(index);
        }

        private void SetScrollIdx(long idx)
        {
            const string m = nameof(SetScrollIdx);
            // LogMethod(m, $"idx: {idx}");
            idx = Math.Clamp(idx, 0L, Math.Max(_total - _content, 1L));
            var percent = (float)idx / Math.Max(1L, _total - _content);
            // LogInfo(m, $"idx: {idx}, percent: {percent}");
            if (null != _handler)
            {
                var rect = _rectTransform.rect;
                var pos = _handler.localPosition;
                pos.y = -(rect.height - _handler.rect.height) * percent;
                // LogInfo(m, $"_handler.pos.y: {_handler.localPosition.y}, new y: {pos.y}");
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
        /// 将点事件数据转为本地坐标比例
        /// </summary>
        /// <param name="localPosY"></param>
        /// <returns></returns>
        private float Position2Percentage(float localPosY)
        {
            const string m = nameof(Position2Percentage);
            // LogMethod(m, $"localPosY: {localPosY}");
            var rect = _rectTransform.rect;
            return Math.Clamp(-localPosY / (rect.height - _handlerHeight), 0f, 1f);
        }
    }
}