using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ACS_Common.Utils;
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
        
        /// <summary>
        /// 从文本生成GCode命令集
        /// </summary>
        /// <param name="text">包含gcode的文本</param>
        /// <returns></returns>
        // public static GCommandStream GCommandsFromText(string text)
        // {
        //     Debug.Log($"{Tag} GCommandsFromString, len: {text.Length}, text: {text}");
        //     if (string.IsNullOrEmpty(text))
        //     {
        //         Debug.LogError($"{Tag} GCommandFromText, text is null or empty");
        //         return null;
        //     }
        //     var lines = Regex.Matches(text, RegexLinesSplit);
        //     Debug.Log($"{Tag} GCommandsFromString, total lines count: {lines.Count}");
        //
        //     List<string> rawTextLiens = new List<string>();
        //     Dictionary<int, GCommand> commands = new Dictionary<int, GCommand>();
        //     Dictionary<int, string> comments = new Dictionary<int, string>();
        //     var i = 0;
        //     foreach (var line in lines)
        //     {
        //         var str = line.ToString();
        //         Debug.Log($"{Tag} GCommandsFromString [{i}] {line}");
        //         rawTextLiens.Add(str);
        //         var commandMatches = Regex.Matches(str, RegexGCodeCommandCheck);
        //         if (commandMatches.Count > 0)
        //         {
        //             if (commandMatches.Count > 1)
        //             {
        //                 Debug.LogError($"{Tag} GCommandsFromString, line [{i}] contains multiple GCode? what happened?");
        //             }
        //             else
        //             {
        //                 var command = new GCommand(commandMatches[0].ToString());
        //                 Debug.Log($"{Tag} GCommandsFromString, line [{i}] contains GCode command: {command}");
        //                 commands.Add(i, command);
        //             }
        //         }
        //         else
        //         {
        //             Debug.Log($"{Tag} GCommandsFromString, line [{i}] NOT contains GCode");
        //         }
        //         var comment = Regex.Matches(str, RegexGCodeCommentCheck);
        //         if (comment.Count > 0)
        //         {
        //             if (comment.Count > 1)
        //             {
        //                 Debug.LogError($"{Tag} GCommandsFromString, line [{i}] contains multiple comments? what happened?");
        //             }
        //             else
        //             {
        //                 Debug.Log($"{Tag} GCommandsFromString, line [{i}] contains comment");
        //                 comments.Add(i, comment[0].ToString());
        //             }
        //         }
        //         else
        //         {
        //             Debug.Log($"{Tag} GCommandsFromString, line [{i}] NOT contains comment");
        //         }
        //         i++;
        //     }
        //     return new GCommandStream(rawTextLiens, commands, comments);
        // }

        /// <summary>
        /// 从文件创建流式命令
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static GCommandStream GCommandFromFile(string filePath)
        {
            // using var fs = new StreamReader(filePath);
            try
            {
                // using var sr = new StreamReader(filePath);
                // var content = sr.ReadLine();
                // while(null != content)
                // {
                //     Debug.Log(content);
                //     content = sr.ReadLine();
                // }
                // using var fs = new FileStream(filePath, FileMode.Open);
                // var lineCnt = fs.CountLines();
                // Debug.Log($"{Tag} line cnt: {lineCnt}");

                var cs = new GCommandStream(filePath);
            }
            catch(IOException e)
            {
                Debug.LogError($"{Tag} GCommandFromFile, read file failed with message: \n{e}");
            }
            return null;
        }
    }
}