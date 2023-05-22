namespace ACS_Common.Motion.RotaryMotor.StepMotor
{
    public interface IStepMotor
    {
        public void Drive(int cnt, bool reverse, int subDiv = 1);
    }
}