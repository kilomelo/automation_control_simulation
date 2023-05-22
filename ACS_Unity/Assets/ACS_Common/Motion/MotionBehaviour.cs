using ACS_Common.Base;
using UnityEngine;

namespace ACS_Common.Motion
{
    public class MotionBehaviour : ACS_Behaviour
    {
        public bool IsLinearMotion => this is ILinearMotionMechanism;
        public bool IsRotaryMotion => this is IRotaryMotionMechanism;
        
        public ILinearMotionMechanism AsLinearMotion => this as ILinearMotionMechanism;
        public IRotaryMotionMechanism AsRotaryMotion => this as IRotaryMotionMechanism;
    }
}
