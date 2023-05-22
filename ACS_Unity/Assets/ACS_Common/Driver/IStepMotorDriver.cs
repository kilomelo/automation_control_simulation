namespace ACS_Common.Driver
{
    public interface IStepMotorDriver
    {
        /// <summary>
        /// 细分数
        /// </summary>
        public int SubDiv { get; }
        /// <summary>
        /// 向电机发送脉冲信号
        /// </summary>
        public void SendPulse(int cnt, bool reverse);
    }
}