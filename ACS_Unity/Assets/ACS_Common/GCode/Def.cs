namespace ACS_Common.GCode
{
    /// <summary>
    /// G Code 解释器 定义
    /// </summary>
    public static class Def
    {
        public enum EGCommandType : byte
        {
            Invalid = 0,
            G = 2,
            M = 3,
        }
    }
}