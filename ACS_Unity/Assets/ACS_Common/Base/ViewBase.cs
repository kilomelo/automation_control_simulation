using UnityEngine;

namespace ACS_Common.Base
{
    public class ViewBase : ACS_Behaviour
    {
        protected RectTransform _rectTransform;

        protected override void Init()
        {
            _rectTransform = transform as RectTransform;
        }
        /// <summary>
        /// 将点事件数据转为本地坐标
        /// </summary>
        /// <param name="eventPosY"></param>
        /// <returns></returns>
        protected float EventPosY2LocalPosY(float eventPosY)
        {
            const string m = nameof(EventPosY2LocalPosY);
            // LogMethod(m, $"eventPosY: {eventPosY}");
            if (null == _rectTransform)
            {
                LogErr(m, "_rectTransform is null");
                return 0f;
            }
            return _rectTransform.InverseTransformPoint(Vector2.up * eventPosY).y;
        }
    }
}