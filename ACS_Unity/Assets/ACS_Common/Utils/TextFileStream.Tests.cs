using System.Collections;

namespace ACS_Common.Utils
{
    public partial class TextFileStream
    {
        public IEnumerator TestReadEachChar()
        {
            LogMethod("TestReadEachChar");
            return ReadEachChar(0, (c, offset) =>
            {
                // LogInfo($"TestReadEachChar, actionEachChar, c: [{c}], offset: {offset}");
                return true;
            });
        }

        public IEnumerator TestReadEachLine()
        {
            LogMethod("TestReadEachLine");
            while (null == _SOLOffsetIdxTreeRoot)
            {
                LogInfo("TestReadEachLine, wait for index build");
                yield return 0;
            }
            LogInfo("TestReadEachLine, index build finished");
            // var limit = 50000;
            var i = 0;
            using var itr = ReadEachLine(0, null, (lineIdx, offset) =>
            {
                i++;
                // LogInfo($"TestReadEachLine, onLineEnd, lineIdx: {lineIdx}, offset: {offset}, str: [{_sb}], str.length: {_sb.Length}");
                // return i < limit;
                return true;
            });
            var itorCnt = 0;
            while (itr.MoveNext())
            {
                LogInfo($"TestReadEachLine one itor");
                itorCnt++;
                yield return 0;
            }
            LogInfo($"TestReadEachLine finish, itorCnt: {itorCnt}");
        }
    }
}