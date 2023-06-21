using ACS_Common.Base;
using ACS_Common.Motion;
using ACS_Common.Motion.RotaryMotor.StepMotor;
using UnityEngine;

namespace ACS_Common.Driver
{
    /// <summary>
    /// 步进电机驱动
    /// </summary>
    public class StepMotorDriver : StepMotorDriverBehaviour
    {
        [SerializeField] private ACS_Behaviour _stepMotorBehaviour;
        [SerializeField] private IStepMotor _stepMotor;

        public override int SubDiv => _subDiv;
        /// <summary>
        /// 细分数
        /// </summary>
        [SerializeField] private int _subDiv = 1;

        /// <summary>
        /// 向电机发送脉冲信号
        /// </summary>
        public override void SendPulse(int cnt, bool reverse)
        {
            Debug.Log($"{Tag} SendPulse, cnt: {cnt}, reverse: {reverse}");
            if (null == _stepMotor)
            {
                Debug.LogWarning($"{Tag} _stepMotor is null");
                return;
            }
            _stepMotor.Drive(cnt, reverse, _subDiv);
        }

        protected override void Init()
        {
            base.Init();
            Debug.Log($"{Tag} Init, _stepMotorBehaviour: {_stepMotorBehaviour}");
            if (null == _stepMotorBehaviour)
            {
                Debug.LogWarning($"{Tag} _stepMotorBehaviour is null");
                return;
            }
            _stepMotor = _stepMotorBehaviour as IStepMotor;
            if (null == _stepMotor)
            {
                Debug.LogError($"{Tag} _stepMotorBehaviour is not stepMotor");
                return;
            }
        }
    }
}