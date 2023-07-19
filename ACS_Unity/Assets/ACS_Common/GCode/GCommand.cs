using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            public char Name;
            public float Value;
            public string RawStr;

            public Param(string str)
            {
                const string m = nameof(Param);
                if (string.IsNullOrEmpty(str))
                {
                    LogErrStatic(m, m, $"invalid raw string, raw string is: {str}");
                    return;
                }
                RawStr = str;
                Name = str[0];
                if (!(Name is <= 'Z' and >= 'A' || Name is <= 'z' and >= 'a'))
                {
                    LogErrStatic(m, m, $"invalid param name, raw string is: {str}");
                    return;
                }
                if (str.Length > 1 && !float.TryParse(str[1..], out Value))
                {
                    LogErrStatic(m, m, $"invalid param value, raw string is: {str}");
                }
            }

            public override string ToString()
            {
                return $"[Name: {Name}, Value: {Value}]";
            }
        }
        
        /// <summary>
        /// 类型 G/M
        /// </summary>
        public Def.EGCommandType CommandType { get; private set; }
        /// <summary>
        /// 号码
        /// </summary>
        public int Number { get; private set; }
        /// <summary>
        /// 参数列表， 可能为空
        /// </summary>
        public Param[] Params { get; private set; }
        /// <summary>
        /// 注释，有可能为空
        /// </summary>
        public string Comment { get; private set; }
        /// <summary>
        /// 原始字符串
        /// </summary>
        public string RawStr { get; private set; }
        /// <summary>
        /// 在命令流中的序号
        /// </summary>
        public long Index { get; set; }
        /// <summary>
        /// 执行时间（毫秒）
        /// 这是一个缓存值，不一定能获取到
        /// </summary>
        public long ExecuteTimeMilliSec { get; set; }
        /// <summary>
        /// 从字符串构造G命令
        /// </summary>
        /// <param name="str"></param>
        public GCommand(string str)
        {
            const string m = nameof(GCommand);
            // LogInfoStatic(m, m, $"constructor, str: [{str}]");
            RawStr = str;

            // 搜索CommandType
            const int searchCommandType = 0;
            // 搜索CommandNumber结尾
            const int searchCommandNumber = 1;
            // 搜索params
            const int searchParams = 2;
            // 提取comment
            const int getComment = 3;
            var progress = 0;
            var startOfCommandNumber = 0;
            var startOfComment = 0;
            var startOfParams = 0;
            for (var i = 0; i < str.Length; i++)
            {
                switch (progress)
                {
                    // 搜索CommandType
                    case searchCommandType:
                    {
                        switch (str[i])
                        {
                            case 'G':
                            case 'g':
                                progress = searchCommandNumber;
                                startOfCommandNumber = i + 1;
                                CommandType = Def.EGCommandType.G;
                                // LogInfoStatic(m, m, $"a, i: {i}, change progress from searchCommandType to searchCommandNumber");
                                break;
                            case 'M':
                            case 'm':
                                progress = searchCommandNumber;
                                startOfCommandNumber = i + 1;
                                CommandType = Def.EGCommandType.M;
                                // LogInfoStatic(m, m, $"b, i: {i}, change progress from searchCommandType to searchCommandNumber");
                                break;
                            case ' ':
                                break;
                            case ';':
                                startOfComment = i + 1;
                                progress = getComment;
                                // LogInfoStatic(m, m, $"c, i: {i}, change progress from searchCommandType to getComment");
                                break;
                            default:
                                LogErrStatic(m, m, $"invalid command type, raw string is: {RawStr}");
                                CommandType = Def.EGCommandType.Invalid;
                                return;
                        }
                        break;
                    }
                    // 搜索CommandNumber结尾
                    case searchCommandNumber:
                    {
                        if (str[i] == ' ' || str[i] == ';')
                        {
                            var numberStr = str.Substring(startOfCommandNumber, i - startOfCommandNumber);
                            // LogInfoStatic(m, m, $"numberStr: [{numberStr}]");
                            if (!int.TryParse(numberStr, out var number))
                            {
                                LogErrStatic(m, m, $"invalid command number, raw string is: {RawStr}");
                                CommandType = Def.EGCommandType.Invalid;
                                return;
                            }
                            Number = number;
                            if (str[i] == ';')
                            {
                                startOfComment = i + 1;
                                progress = getComment;
                                // LogInfoStatic(m, m, $"d, i: {i}, change progress from searchCommandNumberto getComment");
                            }
                            else
                            {
                                startOfParams = i + 1;
                                progress = searchParams;
                                // LogInfoStatic(m, m, $"e, i: {i}, change progress from searchCommandNumber to searchParams, startOfParams: {startOfParams}");
                            }
                        }
                        break;
                    }
                    // 搜索params
                    case searchParams:
                    {
                        if (str[i] == ';')
                        {
                            // LogInfoStatic(m, m, $"searchParams, find ';', i: {i}");
                            startOfComment = i + 1;
                            if (startOfParams < i)
                            {
                                SplitParams(str.Substring(startOfParams, i - startOfParams));
;                            }
                            progress = getComment;
                            // LogInfoStatic(m, m, $"e, i: {i}, change progress from searchParams to getComment");
                        }
                        break;
                    }
                    // 提取comment
                    case getComment:
                    {
                        Comment = str[startOfComment..];
                        // LogInfoStatic(m, m, $"comment: {Comment}");
                        return;
                    }
                }
            }

            if (searchCommandNumber == progress)
            {
                // var numberStr = str[indicatorA..];//.Substring(indicatorA, i - indicatorA);
                if (!int.TryParse(str[startOfCommandNumber..], out var number))
                {
                    LogErrStatic(m, m, $"invalid command number, raw string is: {RawStr}");
                    CommandType = Def.EGCommandType.Invalid;
                    return;
                }
                Number = number;
            }
            else if (searchParams == progress)
            {
                SplitParams(str[startOfParams..]);
            } 
        }

        private void SplitParams(string paramStr)
        {
            const string m = nameof(SplitParams);
            // LogMethod(m, $"paramStr: [{paramStr}]");
            var paramStrArray = paramStr.Split(' ');
            Params = new Param[paramStrArray.Count(p => !string.IsNullOrEmpty(p))];
            var validParamCnt = 0;
            foreach (var p in paramStrArray)
            {
                if (string.IsNullOrEmpty(p)) continue;
                Params[validParamCnt++] = new Param(p);
            }
        }

        private static readonly StringBuilder _sb = new StringBuilder();
        public override string ToString()
        {
            return RawStr;
        }
    }

    /// <summary>
    /// 流式 GCode 命令集合
    /// </summary>
    public class GCommandStream : TextFileStream, IEnumerable<GCommand>
    {
        // protected override bool LogInfoEnable => false;
        private const int DefaultCacheCapacity = 200;
        /// <summary>
        /// 缓存大小
        /// </summary>
        public int CacheCapacity { get; set; }
        /// <summary>
        /// 命令缓存字典，key为行号
        /// </summary>
        private Dictionary<long, GCommand> _commandCache = new Dictionary<long, GCommand>();
        /// <summary>
        /// 缓存队列，行号
        /// </summary>
        private Queue<long> _commandCacheQueue = new Queue<long>();

        /// <summary>
        /// 从文本文件构造GCommandStream
        /// </summary>
        /// <param name="textFilePath"></param>
        public GCommandStream(string textFilePath) : base(textFilePath)
        {
            const string m = nameof(GCommandStream);
            LogInfoStatic(m, m, $"textFilePath: {textFilePath}");
            CacheCapacity = DefaultCacheCapacity;
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
            const string m = nameof(PreserveCacheCapacity);
            // LogMethod(m);
            while (_commandCacheQueue.Count > CacheCapacity)
            {
                var removeCacheLineIdx = _commandCacheQueue.Dequeue();
                _commandCache.Remove(removeCacheLineIdx);
            }
        }

        /// <summary>
        /// 获取任意一条指令
        /// </summary>
        /// <param name="commandLineIdx"></param>
        /// <returns></returns>
        public GCommand GetCommand(long commandLineIdx)
        {
            using var itr = GetEnumerator(commandLineIdx);
            return itr.MoveNext() ? itr.Current : null;
        }

        /// <summary>
        /// 获取一条已缓存指令，如果未缓存过或缓存过期则返回空
        /// </summary>
        /// <param name="commandLineIdx"></param>
        /// <returns></returns>
        public GCommand GetCachedCommand(long commandLineIdx)
        {
            return _commandCache.TryGetValue(commandLineIdx, out var command) ? command : null;
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
            // LogMethod(m, $"startLine: {startLine}, cacheCount: {_commandCache.Count()}");
            var idx = startLine;
            while (idx < TotalLines)
            {
                if (_commandCache.TryGetValue(idx, out var command))
                {
                    // LogInfo(m, $"get command from cache, idx: {idx} command: {command}");
                    yield return command;
                }
                else break;
                idx++;
            }
            using var itr = base.GetEnumerator(idx);
            while (idx < TotalLines)
            {
                if (_commandCache.TryGetValue(idx, out var command))
                {
                    itr.MoveNext();
                    // LogInfo(m, $"get command from cache, idx: {idx} command: {command}");
                    yield return command;
                }
                else
                {
                    if (itr.MoveNext())
                    {
                        var newCommand = new GCommand(itr.Current);
                        newCommand.Index = idx;
                        // LogInfo(m, $"get command from stream, idx: {idx}, newCommand: {newCommand}");
                        Cache(idx, newCommand, true);
                        yield return newCommand;
                    }
                    else
                    {
                        yield break;
                    }
                }
                idx++;
            }
        }
    }
}