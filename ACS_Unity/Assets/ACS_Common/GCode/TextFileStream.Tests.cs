using System.Collections;

namespace ACS_Common.GCode
{
    public partial class TextFileStream
    {
        public IEnumerator TestReadEachChar()
        {
            const string m = nameof(TestReadEachChar);
            LogMethod(m);
            return ReadEachChar(0, (c, offset) =>
            {
                // LogInfo(m, $"actionEachChar, c: [{c}], offset: {offset}");
                return true;
            });
        }

        public IEnumerator TestReadEachLine()
        {
            const string m = nameof(TestReadEachLine);
            LogMethod(m);
            while (null == _SOLOffsetIdxTreeRoot)
            {
                LogInfo(m, "wait for index build");
                yield return 0;
            }
            LogInfo(m, "index build finished");
            // var limit = 50000;
            var i = 0;
            using var itr = ReadEachLine(0, null, (lineIdx, offset) =>
            {
                i++;
                // LogInfo(m, $"onLineEnd, lineIdx: {lineIdx}, offset: {offset}, str: [{_sb}], str.length: {_sb.Length}");
                // return i < limit;
                return true;
            });
            var itorCnt = 0;
            while (itr.MoveNext())
            {
                LogInfo(m, $"one itor");
                itorCnt++;
                yield return 0;
            }
            LogInfo(m, $"finish, itorCnt: {itorCnt}");
        }
    }
}