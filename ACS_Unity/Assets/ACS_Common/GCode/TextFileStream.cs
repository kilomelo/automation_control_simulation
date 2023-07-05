using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ACS_Common.Base;

namespace ACS_Common.GCode
{
    /// <summary>
    /// 带索引搜索树的文本流，可提高大文本随机行读取效率
    /// </summary>
    public partial class TextFileStream : ACS_Object, IDisposable, IEnumerable<string>
    {
        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;

        /// <summary>
        /// 一次读取缓存大小
        /// </summary>
        private static int _byteBufferSize = 1*1024*1024;

        /// <summary>
        /// 索引是否已建立
        /// </summary>
        public bool IndexBuilt => null != _SOLOffsetIdxTreeRoot;
        /// <summary>
        /// 索引建立进度
        /// </summary>
        public float IndexBuildProgress => _indexBuildProgress;
        private float _indexBuildProgress = 0f;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long Size => _size;

        private long _size;

        /// <summary>
        /// 总行数，这个值在建立索引后有效
        /// </summary>
        public long TotalLines => _totalLines;

        /// <summary>
        /// 目前位置
        /// </summary>
        public long Position
        {
            get
            {
                lock (_streamLock)
                {
                    return _stream?.Position ?? 0;
                }
            }
        }
        
        /// <summary>
        /// Start of Line offset
        /// 行首偏移索引节点
        /// </summary>
        private class SOLOffsetNode : ACS_Object
        {
            public long Line;
            public long SOLOffset;
            public SOLOffsetNode Left;
            public SOLOffsetNode Right;

            public override string ToString()
            {
                return $"[Line: {Line}, EOFOffset: {SOLOffset}, Left: {(null == Left ? "NONE" : "Node")}, Right: {(null == Right ? "NONE" : "Node")}]";
            }

            public void Print(int depth = 0)
            {
                const string tab = "    ";
                var sb = new StringBuilder();
                for (var i = 0; i < depth; i++) sb.Append(tab);
                sb.Append($"LineIdx: {Line}, Offset: {SOLOffset}");
                if (null == Left) sb.Append(", left is none");
                if (null == Right) sb.Append(", right is none");
                LogInfo("Print", sb.ToString());
                Left?.Print(depth + 1);
                Right?.Print(depth + 1);
            }
        }
        // 索引树根节点
        private SOLOffsetNode _SOLOffsetIdxTreeRoot;
        protected Stream _stream;
        private Encoding encoding;
        private byte[] _byteBuffer;
        private StringBuilder _sb;
        // 总行数，这个值在建立索引后有效
        private long _totalLines;

        protected object _streamLock = new object();
        public TextFileStream(string textFilePath)
        {
            const string m = nameof(TextFileStream);
            LogInfoStatic(m, m, $"textFilePath: {textFilePath}");

            FileStream fs = null;
            try
            {
                fs = new FileStream(textFilePath, FileMode.Open);
            }
            catch(IOException e)
            {
                LogErrStatic(m, m, $"read file failed with message: \n{e}");
                return;
            }

            _size = fs.Length;
            LogInfoStatic(m,m, $"file size: {fs.Length}, chunkCnt: {Math.Ceiling((float)fs.Length / _byteBufferSize)}");
            _stream = fs;
            _byteBuffer = new byte[_byteBufferSize];
            _sb = new StringBuilder();
            BuildLineIdxAsync();
            LogInfoStatic(m, m, $"end of constructor");
        }
        
        public void Dispose()
        {
            const string m = nameof(Dispose);;
            LogMethod(m, $"Dispose");
            lock (_streamLock)
            {
                _stream?.Dispose();
                _SOLOffsetIdxTreeRoot = null;
                encoding = default;
                _byteBuffer = null;
                _sb = null;
            }
        }

        /// <summary>
        /// c:字符
        /// offset:从文件开始的的偏移
        /// </summary>
        private delegate bool ActionEachChar(char c, long offset);
        /// <summary>
        /// lineIdx:从读取的第一行开始计算的编号
        /// offset:从文件开始的的偏移
        /// </summary>
        private delegate bool ActionEachLine(long lineIdx, long offset);
        private struct ChunkReadReport
        {
            public long ChunkIdx;
            public long LastCharOffset;
            public char LastChar;
            public override string ToString()
            {
                return $"[ChunkReadReport: ChunkIdx: {ChunkIdx}, LastCharOffset: {LastCharOffset}, LastChar: [{LastChar}]";
            }
        }
        /// <summary>
        /// 逐字符读取steam，不重置steam位置
        /// </summary>
        /// <param name="initialPos"></param>
        /// <param name="actionEachChar"></param>
        /// <returns></returns>
        private IEnumerator<ChunkReadReport> ReadEachChar(long initialPos, ActionEachChar actionEachChar)
        {
            const string m = nameof(ReadEachChar);
            // LogMethod(m, $"initialPos: {initialPos}, actionEachChar: {actionEachChar}, encoding: {encoding}");
            lock (_streamLock)
            {
                // LogInfo(m, $"in lock");
                if (initialPos >= 0)
                {
                    _stream.Position = initialPos;
                }
                else
                {
                    initialPos = _stream.Position;
                }

                var currentChar = NULL;
                int bytesRead;
                var chunkCnt = 0L;
                if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
                {
                    while ((bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length)) > 0)
                    {
                        // LogInfo(m, $"begin of chunk read, chunkCnt: {chunkCnt}, bytesRead: {bytesRead}");
                        for (var i = 0; i < bytesRead; i++)
                        {
                            currentChar = (char)_byteBuffer[i];
                            // LogInfo(m, $"read {i}'s char [{currentChar}], chunkCnt: {chunkCnt}");
                            if (null != actionEachChar && !actionEachChar.Invoke(currentChar,
                                    initialPos + chunkCnt * _byteBuffer.Length + i))
                            {
                                yield break;
                            }
                        }

                        yield return new ChunkReadReport()
                        {
                            ChunkIdx = chunkCnt,
                            LastCharOffset = _stream.Position,
                            LastChar = currentChar,
                        };
                        chunkCnt++;
                        // LogInfo($"ReadEachChar, chunk read end 0, chunkCnt: {chunkCnt}");
                    }

                    LogInfo(m, $"done, chunk cnt: {chunkCnt}");
                }
                else
                {
                    var charBuffer = new char[_byteBuffer.Length];
                    while ((bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length)) > 0)
                    {
                        // LogInfo(m, $"begin of chunk read, chunkCnt: {chunkCnt}, bytesRead: {bytesRead}");
                        var charCount = encoding.GetChars(_byteBuffer, 0, bytesRead, charBuffer, 0);
                        for (var i = 0; i < charCount; i++)
                        {
                            currentChar = charBuffer[i];
                            // LogInfo(m, $"read {i}'s char [{currentChar}], chunkCnt: {chunkCnt}");
                            if (null != actionEachChar && !actionEachChar.Invoke(currentChar,
                                    initialPos + chunkCnt * _byteBuffer.Length + i))
                            {
                                yield break;
                            }
                        }

                        yield return new ChunkReadReport()
                        {
                            ChunkIdx = chunkCnt,
                            LastCharOffset = _stream.Position,
                            LastChar = currentChar,
                        };
                        chunkCnt++;
                        // LogInfo($"ReadEachChar, chunk read end 1, chunkCnt: {chunkCnt}");
                    }

                    LogInfo(m, $"done, chunk cnt: {chunkCnt}");
                }
            }
        }

        /// <summary>
        /// 逐行读取steam，不重置steam位置
        /// 每行内容存放在_sb中，每行内容仅在ActionEachLine回调中有效
        /// </summary>
        /// <param name="initialPos">初始偏移</param>
        /// <param name="onLineStart">每行第一个字符调用</param>
        /// <param name="onLineEnd">每行EOL调用</param>
        /// <returns></returns>
        private IEnumerator<ChunkReadReport> ReadEachLine(long initialPos, ActionEachLine onLineStart, ActionEachLine onLineEnd)
        {
            const string m = nameof(ReadEachLine);
            // LogMethod(m, $"encoding: {encoding}");

            var lineCount = 0L;
            var detectedEOL = NULL;
            _sb.Clear();
            var actionEachChar = new ActionEachChar((char c, long offset) =>
            {
                // 每行第一个有效字符
                if (_sb.Length == 0 && (c == detectedEOL || c is not (LF or CR or NULL)) &&
                    null != onLineStart && !onLineStart.Invoke(lineCount, offset)) return false;
                // LogInfo(m, $"ActionEachChar, c [{c}], offset: {offset}");
                if (c is not (LF or CR or NULL)) _sb.Append(c);
                if (detectedEOL != NULL)
                {
                    if (c == detectedEOL)
                    {
                        if (null != onLineEnd && !onLineEnd(lineCount, offset)) return false;
                        lineCount++;
                        _sb.Clear();
                        // LogInfo(m, $"ActionEachChar, line end, offset: {offset}");
                    }
                }
                else if (c is LF or CR)
                {
                    if (null != onLineEnd && !onLineEnd(lineCount, offset)) return false;
                    detectedEOL = c;
                    lineCount++;
                    _sb.Clear();
                    // LogInfo(m, $"EOL detected, detectedEOL: [{detectedEOL}]");
                }
                return true;
            });
            var itr = ReadEachChar(initialPos, actionEachChar);
            var lastChar = NULL;
            var lastCharOffset = 0L;
            while (itr.MoveNext())
            {
                lastChar = itr.Current.LastChar;
                lastCharOffset = itr.Current.LastCharOffset;
                yield return itr.Current;
            }
            if (lastChar is not (LF or CR or NULL))
            {
                onLineEnd?.Invoke(lineCount, lastCharOffset);
                lineCount++;
            }
            // LogInfo(m, $"done, lastChar: [{lastChar}], lastCharOffset: {lastCharOffset}, lineCount: {lineCount}");
        }
        
        #region index
        /// <summary>
        /// 构建行号索引，同步
        /// </summary>
        private void BuildLineIdx()
        {
            const string m = nameof(BuildLineIdx);
            LogMethod(m);
            var sw = new Stopwatch();
            sw.Start();
            if (null == _stream)
            {
                LogErr(m, $"null == _stream");
                return;
            }
            var root = new SOLOffsetNode
            {
                Line = 0L,
                SOLOffset = 0L
            };
            var node = root;
            // // 上一个chunk的最后一行记录
            var prevChunkLastLineIdx = 0L;
            var prevChunkLastLineStartOffset = 0L;
            var onLineStart = new ActionEachLine((long lineIdx, long offset) =>
            {
                _totalLines++;
                prevChunkLastLineIdx = lineIdx;
                prevChunkLastLineStartOffset = offset;
                return true;
            });
            
            using var itr = ReadEachLine(0, onLineStart, null);
            _totalLines = 0;
            var chunkCnt = 0;
            var totalChunk = Math.Ceiling((float)_size / _byteBufferSize);
            while (itr.MoveNext())
            {
                chunkCnt++;
                _indexBuildProgress = (float)(chunkCnt / totalChunk);
                // LogInfo(m, $"chunk itr, itr.current: {itr.Current}");
                if (0 != prevChunkLastLineStartOffset)
                {
                    if (prevChunkLastLineIdx != node.Line)
                    {
                        // LogInfo(m, $"add new idx node, LineIdx: {prevChunkLastLineIdx}, Offset: {prevChunkLastLineStartOffset}");
                        node.Right = new SOLOffsetNode
                        {
                            Line = prevChunkLastLineIdx,
                            SOLOffset = prevChunkLastLineStartOffset
                        };
                        node = node.Right;
                    }
                    // else LogInfo(m, $"wow, such a big line.");
                }
            }
            LogInfo(m, $"lineCount: {_totalLines}, chunkCnt: {chunkCnt}");
            // LogInfo(m, $"idxTree before transform:");
            // root.Print();
            // 将_lineIdxRootNode转为BST
            root = LinkNodes2BST(root);
            // LogInfo(m, $"idxTree after transform:");
            // root.Print();
            _SOLOffsetIdxTreeRoot = root;
            sw.Stop();
            LogInfo(m, $"done, time elapsed: {sw.Elapsed.Seconds} s {sw.Elapsed.Milliseconds} ms");
        }

        /// <summary>
        /// 构建行号索引，异步
        /// </summary>
        private async void BuildLineIdxAsync()
        {
            const string m = nameof(BuildLineIdxAsync);
            LogMethod(m);
            await Task.Run(() =>
            {
                LogMethod($"Task.Run");
                BuildLineIdx();
            });
        }
        
        /// <summary>
        /// 将node链表转为平衡搜索二叉树
        /// </summary>
        /// <param name="head"></param>
        private SOLOffsetNode LinkNodes2BST(SOLOffsetNode head, int depth = 0)
        {
            const string m = nameof(LinkNodes2BST);
            // LogMethod(m, $"depth: {depth}, head: {head}");
            if (null == head)
            {
                LogErr(m, $"head is null");
                return null;
            }

            var result = FindMiddleNode(head, out var middlePrev, out var middle, out var end);
            // LogInfo(m, $"result: {result}, depth: {depth}, middlePrev: {middlePrev}, middle: {middle}, end: {end}");
            if (0 == result)
            {
                return middle;
            }
            middlePrev.Right = null;
            middle.Left = LinkNodes2BST(head, depth + 1);
            if (2 == result) middle.Right = LinkNodes2BST(middle.Right, depth + 1);
            return middle;
        }

        /// <summary>
        /// 找到链表结构的中间节点
        /// </summary>
        /// <param name="head"></param>
        /// <param name="middlePrev"></param>
        /// <param name="middle"></param>
        /// <param name="end"></param>
        /// <returns>二分后的链表是否还能二分，0左右均不可二分；1左可二分右不可；2左右均可二分</returns>
        private int FindMiddleNode(SOLOffsetNode head, out SOLOffsetNode middlePrev, out SOLOffsetNode middle, out SOLOffsetNode end)
        {
            const string m = nameof(FindMiddleNode);
            // LogMethod(m, $"head: {head}");
            middlePrev = null;
            middle = null;
            end = null;
            if (null == head)
            {
                LogErr(m, $"null == headNode");
                return 0;
            }

            middlePrev = head;
            middle = head;
            end = head;
            while (middle.Right != null)
            {
                if (null == end.Right)
                {
                    break;
                }
                middlePrev = middle;
                middle = middle.Right;
                if (null == end.Right.Right)
                {
                    end = end.Right;
                    break;
                }
                end = end.Right.Right;
            }
            return middle == middlePrev ? 0 : middle == end? 1 : 2;
        }

        /// <summary>
        /// 搜索某一行的起始位置
        /// </summary>
        /// <param name="lineIdx"></param>
        private long SearchStartOfLineOffset(long lineIdx)
        {
            const string m = nameof(SearchStartOfLineOffset);
            // LogMethod(m, $"lineIdx: {lineIdx}");
            if (null == _SOLOffsetIdxTreeRoot)
            {
                LogErr(m, $"idx not built");
                return -1;
            }
            if (lineIdx > _totalLines)
            {
                LogErr(m, $"lineIdx {lineIdx} out of range, total line: {_totalLines}");
                return -1;
            }
            if (lineIdx < 0)
            {
                LogErr(m, $"lineIdx {lineIdx} out of range, Non-negative number required.");
                return -1;
            }
            var node = _SOLOffsetIdxTreeRoot;
            var targetNode = node;
            // step 1 搜索比lineIdx小的行的偏移作为搜索起始位置
            while (true)
            {
                if (node.Line < lineIdx)
                {
                    targetNode = node;
                    if (null == node.Right)
                    {
                        // LogInfo(m, $"step 1, to right, but no right");
                        break;
                    }
                    // LogInfo(m, $"step 1, to right");
                    node = node.Right;
                }
                else if (node.Line > lineIdx)
                {
                    if (null == node.Left)
                    {
                        // LogInfo(m, $"step 1, to left, but no left");
                        break;
                    }
                    // LogInfo(m, $"step 1, to left");
                    node = node.Left;
                }
                // equal
                else
                {
                    targetNode = node;
                    break;
                }
            }
            // LogInfo(m, $"step 1 finish, targetNode: {targetNode}");
            
            // step 2 往后找到第lineIdx的位置
            var pos = targetNode.SOLOffset;
            if (node.Line != lineIdx)
            {
                var itr = ReadEachLine(targetNode.SOLOffset, new ActionEachLine((long idx, long offset) =>
                {
                    // LogInfo(m, $"step 2 ActionEachLine, idx: {idx}, offset: {offset}");
                    pos = offset;
                    if (idx + targetNode.Line == lineIdx)
                    {
                        // LogInfo(m, $"ActionEachLine, find target, lineIdx: {idx}, offset: {offset}");
                    }
                    return idx + targetNode.Line < lineIdx;
                }), null);
                while (itr.MoveNext())
                {
                    // search exact line pos
                }
            }
            // LogInfo(m, $"step 2 finish, pos: {pos}");
            return pos;
        }
        
        #endregion // index
        
        #region IEnumerator

        public IEnumerator<string> GetEnumerator()
        {
            lock (_streamLock)
            {
                _stream.Position = 0;
                var sr = new StreamReader(_stream);
                while (true)
                {
                    var line = sr.ReadLine();
                    if (null == line) yield break;
                    yield return line;
                }
            }
        }

        /// <summary>
        /// 从特定行开始获得迭代器
        /// </summary>
        /// <param name="startLine"></param>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator(long startLine)
        {
            const string m = nameof(GetEnumerator);
            lock (_streamLock)
            {
                // LogMethod(m, $"startLine: {startLine}");
                if (null == _SOLOffsetIdxTreeRoot)
                {
                    LogErr(m, $"idx not built");
                    yield break;
                }

                if (startLine > _totalLines)
                {
                    LogErr(m, $"startLine {startLine} out of range, total line: {_totalLines}");
                    yield break;
                }

                if (startLine < 0)
                {
                    LogErr(m, $"startLine {startLine} out of range, Non-negative number required.");
                    yield break;
                }

                var offset = SearchStartOfLineOffset(startLine);
                if (offset < 0)
                {
                    LogErr(m, $"startLine {startLine}, indexing failed.");
                    yield break;
                }

                _stream.Position = offset;
                // LogInfo(m, $"_stream.Position: {_stream.Position}");
                var sr = new StreamReader(_stream);
                while (true)
                {
                    var line = sr.ReadLine();
                    if (null == line) break;
                    yield return line;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion // IEnumerator
    }
}