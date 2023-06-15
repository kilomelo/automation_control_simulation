using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ACS_Common.GCodeParser
{
    public static class GTools
    {
        private const string Tag = nameof(GTools);

        /// <summary>
        /// 文本分割为行 正则
        /// </summary>
        private const string RegexLinesSplit = @"(?<=^|\r|\n|\r\n)\S.*(?=$|\r|\n|\r\n)";
        /// <summary>
        /// GCode 命令正则
        /// </summary>
        private const string RegexGCodeCommandCheck = @"(?<=^)[GgMm][0-9]+(?!\S)\s*?.*?(?=\s*($|;))";
        /// <summary>
        /// GCode 注释正则
        /// </summary>
        private const string RegexGCodeCommentCheck = @"(?<=;\s*)([^\s].+[^\s]|\S?\S)";
        /// <summary>
        /// GCode 命令类型正则
        /// </summary>
        public const string RegexGCodeCommentType = @"[GgMm]";
        /// <summary>
        /// GCode 命令数字正则
        /// </summary>
        public const string RegexGCodeCommentNumber = @"(?<=[GgMm])[0-9]+(?=\s|$|;)";
        /// <summary>
        /// GCode 参数正则
        /// </summary>
        public const string RegexGCodeCommentParam = @"(?<=\s)\S+(?=$|\s)";
        /// <summary>
        /// GCode 参数名正则
        /// </summary>
        public const string RegexGCodeCommentParamName = @"(?<=^)[A-Z|a-z]";
        /// <summary>
        /// GCode 参数值正则
        /// </summary>
        public const string RegexGCodeCommentParamValue = @"(?<=^[A-Z|a-z])-?[0-9]*.?[0-9]*(?=$)";
    }
}