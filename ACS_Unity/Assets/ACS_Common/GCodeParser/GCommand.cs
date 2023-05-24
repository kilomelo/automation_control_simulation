namespace ACS_Common.GCodeParser
{
    /// <summary>
    /// GCode 命令
    /// </summary>
    public class GCommand
    {
        private const string Tag = nameof(GCommand);
        /// <summary>
        /// GCode 命令的参数
        /// </summary>
        public class Param
        {
            
        }
        
        // 类型 G/M
        private Def.EGCommandType _commandType;
        // 号码
        private int _commandNumber;
        // 参数列表
        private Param[] _parameters;
        // 注释
        private string _comment;

        /// <summary>
        /// 从字符串构造G命令
        /// </summary>
        /// <param name="str"></param>
        public GCommand(string str)
        {
            
        }

        public override string ToString()
        {
            return $"[GCommandType: {_commandType}, CommandNumber: {_commandNumber}, Parameters: {_parameters}, Comment: {_comment}]";
        }
    }
}

