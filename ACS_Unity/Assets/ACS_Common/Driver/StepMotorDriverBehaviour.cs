using ACS_Common.Base;
using UnityEngine;

namespace ACS_Common.Driver
{
    public abstract class StepMotorDriverBehaviour : ACS_Behaviour, IStepMotorDriver
    {
        public abstract int SubDiv { get; }
        public abstract void SendPulse(int cnt, bool reverse);
    }
}