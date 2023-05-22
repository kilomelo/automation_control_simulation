using System;
using UnityEngine;

namespace ACS_Common.Motion.RotaryMotor.StepMotor
{
    /// <summary>
    /// 步进电机
    /// </summary>
    public class StepMotor : MotionBehaviour, IRotaryMotionMechanism, IStepMotor
    {
        private const string Tag = nameof(StepMotor);
        /// <summary>
        /// 步距角，单位角秒
        /// </summary>
        [SerializeField] private int _stepAngleArcSec = 108 * 60;

        /// <summary>
        /// 当前转动位置，单位角秒
        /// </summary>
        private int _rotatePosArcSec = 0;

        /// <summary>
        /// 运动回调，参数为旋转量，单位角秒
        /// </summary>
        /// 
        public Action<int> OnMotion { get; set; }

        /// <summary>
        /// 驱动
        /// </summary>
        /// <param name="cnt">脉冲数</param>
        /// <param name="reverse">是否反转</param>
        /// <param name="subDiv">细分数</param>
        public void Drive(int cnt, bool reverse, int subDiv = 1)
        {
            Debug.Log($"{Tag} Drive, cnt: {cnt}, reverse: {reverse}, subdiv: {subDiv}");
            int deltaAngle = (reverse ? -1 : 1) * cnt * _stepAngleArcSec / subDiv;
            _rotatePosArcSec += deltaAngle;
            OnMotion?.Invoke(deltaAngle);
        }
    }
}
