using System.Collections.Generic;

namespace ACS_Common.GCodeParser
{
    /// <summary>
    /// GCode 单条命令
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

        /// <summary>
        /// 从字符串构造G命令
        /// </summary>
        /// <param name="str"></param>
        public GCommand(string str)
        {
            
        }

        public override string ToString()
        {
            return $"[GCommandType: {_commandType}, CommandNumber: {_commandNumber}, Parameters: {_parameters}]";
        }
    }

    /// <summary>
    /// GCode 命令集合
    /// </summary>
    public class GCommandSet
    {
        /// <summary>
        /// 原始文本数据
        /// </summary>
        private List<string> _rawTextLiens;
        /// <summary>
        /// 有效命令
        /// </summary>
        private Dictionary<int, GCommand> _commands;
        /// <summary>
        /// 注释
        /// </summary>
        private Dictionary<int, string> _comments;

        public GCommandSet(List<string> rawTextLiens, Dictionary<int, GCommand> commands, Dictionary<int, string> comments)
        {
            _rawTextLiens = rawTextLiens;
            _commands = commands;
            _comments = comments;
        }
    }
}

