using System;
using ACS_Common.Base;
using UnityEngine;

namespace ACS_Common.Motion.LinearMotion
{
    /// <summary>
    /// 同步带总成，包括驱动齿轮、移动平台
    /// </summary>
    public class SyncBelt : ACS_Behaviour, ILinearMotionMechanism
    {
        /// <summary>
        /// 齿距，单位微米
        /// </summary>
        [SerializeField] private int _toothPitchUm = 2000;

        /// <summary>
        /// 驱动齿轮齿数
        /// </summary>
        [SerializeField] private int _toothCnt = 20;

        /// <summary>
        /// 齿轮驱动机构
        /// </summary>
        [SerializeField] private MotionBehaviour _driveWheelSource;

        /// <summary>
        /// 有效行程，单位微米
        /// </summary>
        [SerializeField] private int _effectiveLength;
        /// <summary>
        /// 初始位置，单位微米
        /// </summary>
        [SerializeField] private int _initialPos;

        /// <summary>
        /// 当前位置，单位微米
        /// </summary>
        private int _curPos;

        /// <summary>
        /// 运动回调，参数为距离变化量，单位微米
        /// </summary>
        public Action<int> OnMotion { get; set; }

        /// <summary>
        /// 限位回调，参数为限位位置，0最新1最大
        /// </summary>
        public Action<int> OnLimit { get; set; }
        protected override void Init()
        {
            LogInfo("todo method name", $"Init");
            if (null != _driveWheelSource) _driveWheelSource.AsRotaryMotion.OnMotion += OnDriveWheelSourceMotion;
            _curPos = _initialPos;
        }

        private void OnDestroy()
        {
            LogInfo("todo method name", $"OnDestroy");
            if (null != _driveWheelSource) _driveWheelSource.AsRotaryMotion.OnMotion -= OnDriveWheelSourceMotion;
        }

        public void SetDriveWheelSource(MotionBehaviour motionBehaviour)
        {
            LogInfo("todo method name", $"SetDriveWheelSource, motionBehaviour: {motionBehaviour}");
            if (null == motionBehaviour)
            {
                LogErr("todo method name", $"SetDriveWheelSource failed, monoMotion is null");
                return;
            }
            if (!motionBehaviour.IsRotaryMotion)
            {
                LogErr("todo method name", $"SetDriveWheelSource failed, monoMotion is not a RotaryMotion");
                return;
            }
            _driveWheelSource = motionBehaviour;
            _driveWheelSource.AsRotaryMotion.OnMotion += OnDriveWheelSourceMotion;
        }

        public void UnsetDriveWheelSource()
        {
            LogInfo("todo method name", $"UnsetDriveWheelSource， _driveWheelSource： {_driveWheelSource}");
            if (null == _driveWheelSource) return;
            _driveWheelSource.AsRotaryMotion.OnMotion -= OnDriveWheelSourceMotion;
            _driveWheelSource = null;
        }

        private void OnDriveWheelSourceMotion(int deltaArcSec)
        {
            var deltaTangentDistance = _toothCnt * _toothPitchUm * deltaArcSec / (360 * 60 * 60);
            LogInfo("todo method name", $"OnDriveWheelSourceMotion, deltaArcSec: {deltaArcSec}, deltaTangentDistance: {deltaTangentDistance}");
            if (_curPos + deltaTangentDistance < 0)
            {
                deltaTangentDistance = -_curPos;
                OnMotion?.Invoke(deltaTangentDistance);
                OnLimit?.Invoke(0);
            }
            else if (_curPos + deltaTangentDistance > _effectiveLength)
            {
                deltaTangentDistance = _effectiveLength - _curPos;
                OnMotion?.Invoke(deltaTangentDistance);
                OnLimit?.Invoke(1);
            }
            else
            {
                OnMotion?.Invoke(deltaTangentDistance);
            }
            LogInfo("todo method name", $"OnDriveWheelSourceMotion, final deltaTangentDistance: {deltaTangentDistance}");
        }
    }
}