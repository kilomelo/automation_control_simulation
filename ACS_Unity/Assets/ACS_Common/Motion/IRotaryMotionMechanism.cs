using System;

namespace ACS_Common.Motion
{
    /// <summary>
    /// 旋转运动机构
    /// </summary>
    public interface IRotaryMotionMechanism
    {
        /// <summary>
        /// 运动回调，参数为旋转量，单位角秒
        /// </summary>
        /// 
        public Action<int> OnMotion { get; set; }
    }
}