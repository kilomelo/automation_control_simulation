using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ACS_Common.GCodeParser
{
    public static class GTools
    {
        private const string Tag = nameof(GTools);

        /// <summary>
        /// 文本分割为行 正则表达式
        /// </summary>
        private const string RegexLinesSplit = @"(?<=^|\r|\n|\r\n)\S.*(?=$|\r|\n|\r\n)";
        /// <summary>
        /// GCode 命令正则表达式
        /// </summary>
        private const string RegexGCodeCommandCheck = @"[GgMm]([1-9]+[0-9]*|0)(?!\S)\s*.*?(?=\s*($|;))";
        /// <summary>
        /// GCode 注释正则表达式
        /// </summary>
        private const string RegexGCodeCommentCheck = @"(?<=;\s*)([^\s].+[^\s]|\S?\S)";
        /// <summary>
        /// 从文本生成GCode命令集
        /// </summary>
        /// <param name="text">包含gcode的文本</param>
        /// <returns></returns>
        public static GCommandSet GCommandsFromText(string text)
        {
            Debug.Log($"{Tag} GCommandsFromString, len: {text.Length}, text: {text}");
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError($"{Tag} GCommandFromText, text is null or empty");
                return null;
            }
            var lines = Regex.Matches(text, RegexLinesSplit);
            Debug.Log($"{Tag} GCommandsFromString, total lines count: {lines.Count}");

            List<string> rawTextLiens = new List<string>();
            Dictionary<int, GCommand> commands = new Dictionary<int, GCommand>();
            Dictionary<int, string> comments = new Dictionary<int, string>();
            var i = 0;
            foreach (var line in lines)
            {
                var str = line.ToString();
                Debug.Log($"{Tag} GCommandsFromString [{i}] {line}");
                rawTextLiens.Add(str);
                var command = Regex.Matches(str, RegexGCodeCommandCheck);
                if (command.Count > 0)
                {
                    if (command.Count > 1)
                    {
                        Debug.LogError($"{Tag} GCommandsFromString, line [{i}] contains multiple GCode? what happened?");
                    }
                    else
                    {
                        Debug.Log($"{Tag} GCommandsFromString, line [{i}] contains GCode");
                        commands.Add(i, new GCommand(command[0].ToString()));
                    }
                }
                else
                {
                    Debug.Log($"{Tag} GCommandsFromString, line [{i}] NOT contains GCode");
                }
                var comment = Regex.Matches(str, RegexGCodeCommentCheck);
                if (comment.Count > 0)
                {
                    if (comment.Count > 1)
                    {
                        Debug.LogError($"{Tag} GCommandsFromString, line [{i}] contains multiple comments? what happened?");
                    }
                    else
                    {
                        Debug.Log($"{Tag} GCommandsFromString, line [{i}] contains comment");
                        comments.Add(i, comment[0].ToString());
                    }
                }
                else
                {
                    Debug.Log($"{Tag} GCommandsFromString, line [{i}] NOT contains comment");
                }
                i++;
            }
            return new GCommandSet(rawTextLiens, commands, comments);
        }
    }
}