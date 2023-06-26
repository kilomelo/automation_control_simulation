using ACS_Common.Base;
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
            LogInfo("todo method name", $"SendPulse, cnt: {cnt}, reverse: {reverse}");
            if (null == _stepMotor)
            {
                LogErr("todo method name", $"_stepMotor is null");
                return;
            }
            _stepMotor.Drive(cnt, reverse, _subDiv);
        }

        protected override void Init()
        {
            base.Init();
            LogInfo("todo method name", $"Init, _stepMotorBehaviour: {_stepMotorBehaviour}");
            if (null == _stepMotorBehaviour)
            {
                LogWarn("todo method name", $"_stepMotorBehaviour is null");
                return;
            }
            _stepMotor = _stepMotorBehaviour as IStepMotor;
            if (null == _stepMotor)
            {
                LogErr("todo method name", $"_stepMotorBehaviour is not stepMotor");
                return;
            }
        }
    }
}