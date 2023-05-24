using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ACS_Common.GCodeParser
{
    public static class GTools
    {
        private const string Tag = nameof(GTools);
        /// <summary>
        /// GCode 命令正则表达式
        /// </summary>
        private const string GCodeCommandCheckRegex = @"(?<=^|\n|\r)\s*[GgMm]([1-9]+[0-9]*|0)\s[\s\S]*?(?=$|\n|\r)";

        public static List<GCommand> GCommandsFromString(string str)
        {
            Debug.Log($"{Tag} GCommandsFromString, str: {str}");
            var matches = Regex.Matches(str, GCodeCommandCheckRegex);
            if (matches.Count > 0)
            {
                Debug.Log($"{Tag} GCommandsFromString, {matches.Count} matches");
                var i = 0;
                foreach (var match in matches)
                {
                    Debug.Log($"{Tag} GCommandsFromString, match {i++}: [{match}]");
                }
            }
            else
            {
                Debug.LogError($"{Tag} GCommandsFromString, invalid command content");
            }
            return null;
        }
    }
}