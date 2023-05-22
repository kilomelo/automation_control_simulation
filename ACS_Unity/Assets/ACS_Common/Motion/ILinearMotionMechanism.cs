using System;

namespace ACS_Common.Motion
{
    /// <summary>
    /// 直线运动机构
    /// </summary>
    public interface ILinearMotionMechanism
    {
        /// <summary>
        /// 运动回调，参数为距离变化量，单位微米
        /// </summary>
        public Action<int> OnMotion { get; set; }
        
        /// <summary>
        /// 限位回调，参数为限位位置，0最新1最大
        /// </summary>
        public Action<int> OnLimit { get; set; }
    }
}
