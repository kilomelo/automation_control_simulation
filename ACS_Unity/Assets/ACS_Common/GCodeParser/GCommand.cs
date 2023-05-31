using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

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
            public string Name;
            public float Value;

            public Param(string str)
            {
                Debug.Log($"{Tag} GCommand.Param constructor, str: [{str}]");
                var paramNameMatches = Regex.Matches(str, GTools.RegexGCodeCommentParamName);
                if (paramNameMatches.Count != 1)
                {
                    Debug.LogError($"{Tag} GCommand.Param constructor failed, invalid param name, raw string is: {str}");
                    return;
                }
                Name = paramNameMatches[0].ToString();

                var paramValueMatches = Regex.Matches(str, GTools.RegexGCodeCommentParamValue);
                if (paramValueMatches.Count != 1 || (paramValueMatches.Count == 1 && !float.TryParse(paramValueMatches[0].ToString(), out Value)))
                {
                    Debug.LogError($"{Tag} GCommand.Param constructor failed, invalid param value, raw string is: {str}");
                }
            }

            public override string ToString()
            {
                return $"[Name: {Name}, Value: {Value}]";
            }
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
            Debug.Log($"{Tag} GCommand constructor, str: [{str}]");
            
            var commandTypeMatches = Regex.Matches(str, GTools.RegexGCodeCommentType);
            if (commandTypeMatches.Count != 1)
            {
                Debug.LogError($"{Tag} GCommand constructor failed, invalid command type, raw string is: {str}");
                return;
            }
            if (!Enum.TryParse(commandTypeMatches[0].ToString(), out _commandType))
            {
                _commandType = Def.EGCommandType.Invalid;
            }
            
            var commandNumberMatches = Regex.Matches(str, GTools.RegexGCodeCommentNumber);
            if (commandNumberMatches.Count != 1 || !int.TryParse(commandNumberMatches[0].ToString(), out _commandNumber))
            {
                Debug.LogError($"{Tag} GCommand constructor failed, invalid command number, raw string is: {str}");
                return;
            }
                
            var commandParamMatches = Regex.Matches(str, GTools.RegexGCodeCommentParam);
            Debug.Log($"{Tag} GCommand constructor, commandParams count: {commandParamMatches.Count}");

            var i = 0;
            _parameters = new Param[commandParamMatches.Count];
            foreach (var param in commandParamMatches)
            {
                Debug.Log($"{Tag} GCommand constructor, param[{i}]: {param}");
                _parameters[i] = new Param(param.ToString());
                i++;
            }
        }

        private static readonly StringBuilder _sb = new StringBuilder();
        public override string ToString()
        {
            _sb.Clear();
            foreach (var param in _parameters)
            {
                _sb.Append(param);
            }
            return $"[GCommandType: {_commandType}, CommandNumber: {_commandNumber}, Parameters: {_sb}]";
        }
    }

    /// <summary>
    /// 流式 GCode 命令集合
    /// </summary>
    public class GCommandStream
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

        public GCommandStream(List<string> rawTextLiens, Dictionary<int, GCommand> commands, Dictionary<int, string> comments)
        {
            _rawTextLiens = rawTextLiens;
            _commands = commands;
            _comments = comments;
        }
    }
}

