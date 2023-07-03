
using ACS_Common.Base;

namespace ACS_Common.GCode
{
    public static class GTools
    {

        public static class Regex
        {
            /// <summary>
            /// 文本分割为行 正则
            /// </summary>
            private const string LinesSplit = @"(?<=^|\r|\n|\r\n)\S.*(?=$|\r|\n|\r\n)";

            public static class GCodeCommand
            {

                /// <summary>
                /// GCode 命令正则
                /// </summary>
                public const string Check = @"(?<=^)[GgMm][0-9]+(?!\S)\s*?.*?(?=\s*($|;))";

                /// <summary>
                /// GCode 注释正则
                /// </summary>
                public const string Comment = @"(?<=;\s*)([^\s].+[^\s]|\S?\S)";

                /// <summary>
                /// GCode 命令类型正则
                /// </summary>
                public const string Type = @"[GgMm]";

                /// <summary>
                /// GCode 命令数字正则
                /// </summary>
                public const string Number = @"(?<=[GgMm])[0-9]+(?=\s|$|;)";

                /// <summary>
                /// GCode 参数正则
                /// </summary>
                public const string Param = @"(?<=\s)\S+(?=$|\s)";

                /// <summary>
                /// GCode 参数名正则
                /// </summary>
                public const string ParamName = @"(?<=^)[A-Z|a-z]";

                /// <summary>
                /// GCode 参数值正则
                /// </summary>
                public const string ParamValue = @"(?<=^[A-Z|a-z])-?[0-9]*.?[0-9]*(?=$)";
            }
        }
    }
}