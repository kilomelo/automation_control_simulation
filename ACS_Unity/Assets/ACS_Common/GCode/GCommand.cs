using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ACS_Common.Base;

namespace ACS_Common.GCode
{
    /// <summary>
    /// GCode 单条命令
    /// </summary>
    public class GCommand : ACS_Object
    {
        /// <summary>
        /// GCode 命令的参数
        /// </summary>
        public class Param : ACS_Object
        {
            public string Name;
            public float Value;

            public Param(string str)
            {
                const string m = nameof(Param);
                LogInfoStatic(m, m, $"str: [{str}]");
                var paramNameMatches = Regex.Matches(str, GTools.Regex.GCodeCommand.ParamName);
                if (paramNameMatches.Count != 1)
                {
                    LogErrStatic(m, m, $"invalid param name, raw string is: {str}");
                    return;
                }
                Name = paramNameMatches[0].ToString();

                var paramValueMatches = Regex.Matches(str, GTools.Regex.GCodeCommand.ParamValue);
                if (paramValueMatches.Count != 1 || (paramValueMatches.Count == 1 && !float.TryParse(paramValueMatches[0].ToString(), out Value)))
                {
                    LogErrStatic(m, m, $"invalid param value, raw string is: {str}");
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
        private int _number;
        // 参数列表， 可能为空
        private Param[] _parameters;
        // 注释，有可能为空
        private string _comment;

        /// <summary>
        /// 类型 G/M
        /// </summary>
        public Def.EGCommandType CommandType => _commandType;
        /// <summary>
        /// 号码
        /// </summary>
        public int Number => _number;
        /// <summary>
        /// 参数列表， 可能为空
        /// </summary>
        public Param[] Params => _parameters;
        /// <summary>
        /// 注释，有可能为空
        /// </summary>
        public string Comment => _comment;

        /// <summary>
        /// 从字符串构造G命令
        /// </summary>
        /// <param name="str"></param>
        public GCommand(string str)
        {
            const string m = nameof(GCommand);
            LogInfoStatic(m, m, $"constructor, str: [{str}]");
            var validCommands = Regex.Matches(str, GTools.Regex.GCodeCommand.Check);
            // 如果有有效命令
            if (validCommands.Count == 1)
            {
                var validCommandStr = validCommands[0].ToString();
                var commandTypeMatches = Regex.Matches(validCommandStr, GTools.Regex.GCodeCommand.Type);
                if (commandTypeMatches.Count != 1)
                {
                    LogErrStatic(m, m, $"invalid command type, raw string is: {validCommandStr}");
                    return;
                }
                if (!Enum.TryParse(commandTypeMatches[0].ToString(), out _commandType))
                {
                    _commandType = Def.EGCommandType.None;
                }
            
                var commandNumberMatches = Regex.Matches(validCommandStr, GTools.Regex.GCodeCommand.Number);
                if (commandNumberMatches.Count != 1 || !int.TryParse(commandNumberMatches[0].ToString(), out _number))
                {
                    LogErrStatic(m, m, $"invalid command number, raw string is: {validCommandStr}");
                    return;
                }
                
                var commandParamMatches = Regex.Matches(validCommandStr, GTools.Regex.GCodeCommand.Param);
                LogInfoStatic(m, m, $"commandParams count: {commandParamMatches.Count}");

                var i = 0;
                _parameters = new Param[commandParamMatches.Count];
                foreach (var param in commandParamMatches)
                {
                    LogInfoStatic(m, m, $"param[{i}]: {param}");
                    _parameters[i] = new Param(param.ToString());
                    i++;
                }
            }
            var comments = Regex.Matches(str, GTools.Regex.GCodeCommand.Comment);
            if (comments.Count == 1) _comment = comments[0].ToString();
        }

        private static readonly StringBuilder _sb = new StringBuilder();
        public override string ToString()
        {
            _sb.Clear();
            if (null != _parameters)
            {
                foreach (var param in _parameters)
                {
                    _sb.Append(param);
                }
            }
            return $"[GCommandType: {_commandType}, CommandNumber: {_number}, Parameters: [{_sb}], Comment: [{_comment}]]";
        }
    }

    /// <summary>
    /// 流式 GCode 命令集合
    /// </summary>
    public class GCommandStream : TextFileStream, IEnumerable<GCommand>
    {
        /// <summary>
        /// 缓存大小
        /// </summary>
        private int _cacheCapacity = 200;
        /// <summary>
        /// 命令缓存字典，key为行号
        /// </summary>
        private Dictionary<long, GCommand> _commandCache = new Dictionary<long, GCommand>();
        /// <summary>
        /// 缓存队列，行号
        /// </summary>
        private Queue<long> _commandCacheQueue = new Queue<long>();

        // /// <summary>
        // /// 有效命令
        // /// </summary>
        // private Dictionary<int, GCommand> _commands;
        // /// <summary>
        // /// 注释缓存字典，key为行号
        // /// </summary>
        // private Dictionary<long, string> _comments;

        /// <summary>
        /// 从文本文件构造GCommandStream
        /// </summary>
        /// <param name="textFilePath"></param>
        public GCommandStream(string textFilePath) : base(textFilePath)
        {
            const string m = nameof(GCommandStream);
            LogInfoStatic(m, m, $"textFilePath: {textFilePath}");
        }
        
        private void Cache(long lineIdx, GCommand command, bool checkCapacity = false)
        {
            _commandCache[lineIdx] = command;
            _commandCacheQueue.Enqueue(lineIdx);
            if (checkCapacity)
            {
                PreserveCacheCapacity();
            }
        }
        
        /// <summary>
        /// 保持cache容量
        /// </summary>
        private void PreserveCacheCapacity()
        {
            while (_commandCacheQueue.Count > _cacheCapacity)
            {
                var removeCacheLineIdx = _commandCacheQueue.Dequeue();
                _commandCache.Remove(removeCacheLineIdx);
            }
        }

        public new IEnumerator<GCommand> GetEnumerator()
        {
            const string m = nameof(GetEnumerator);
            LogMethod(m);
            return GetEnumerator(0L);
        }

        public new IEnumerator<GCommand> GetEnumerator(long startLine)
        {
            const string m = nameof(GetEnumerator);
            LogMethod(m, $"startLine: {startLine}");
            var idx = startLine;
            IEnumerator<string> itr = null;
            while (idx < TotalLines)
            {
                if (_commandCache.TryGetValue(idx, out var command))
                {
                    itr?.MoveNext();
                    LogInfo(m, $"get command from cache, idx: {idx} command: {command}");
                    yield return command;
                }
                else
                {
                    itr ??= base.GetEnumerator(idx);
                    if (itr.MoveNext())
                    {
                        var newCommand = new GCommand(itr.Current);
                        LogInfo(m, $"get command from stream, idx: {idx}, newCommand: {newCommand}");
                        Cache(idx, newCommand);
                        yield return newCommand;
                    }
                    else
                    {
                        itr.Dispose();
                        PreserveCacheCapacity();
                        yield break;
                    }
                }
                idx++;
            }
        }
    }
}

