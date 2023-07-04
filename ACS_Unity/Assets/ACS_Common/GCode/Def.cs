namespace ACS_Common.GCode
{
    /// <summary>
    /// G Code 解释器 定义
    /// </summary>
    public static class Def
    {
        public enum EGCommandType : byte
        {
            None = 0,
            G = 1,
            M = 2,
            Invalid = 3,
        }
    }
}