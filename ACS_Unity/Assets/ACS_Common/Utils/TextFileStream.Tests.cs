namespace ACS_Common.Utils
{
    public partial class TextFileStream
    {
        public void TestReadEachChar()
        {
            LogMethod("TestReadEachChar");
            _stream.Position = 0;
            var result = ReadEachChar((chunkIdx, bytesRead) =>
            {
                // LogInfo($"TestReadEachChar, actionEachChunk, chunkIdx: {chunkIdx}, bytesRead: {bytesRead}");
                return true;
            }, (c, offset) =>
            {
                LogInfo($"TestReadEachChar, actionEachChar, c: [{c}], offset: {offset}");
                return true;
            });
            LogInfo($"TestReadEachChar, result: {result}");
        }

        public void TestReadEachLine()
        {
            LogMethod("TestReadEachLine");
            _stream.Position = 0;
            var result = ReadEachLine((chunkIdx, bytesRead) =>
            {
                // LogInfo($"TestReadEachLine, actionEachChunk, chunkIdx: {chunkIdx}, bytesRead: {bytesRead}");
                return true;
            }, (lineIdx, offset) =>
            {
                LogInfo($"TestReadEachLine, onLineStart, lineIdx: {lineIdx}, offset: {offset}");
                return true;
            }, (lineIdx, offset) =>
            {
                LogInfo($"TestReadEachLine, onLineEnd, lineIdx: {lineIdx}, offset: {offset}, str: [{_sb}], str.length: {_sb.Length}");
                return true;
            });
            LogInfo($"TestReadEachLine, result: {result}");
        }
    }
}