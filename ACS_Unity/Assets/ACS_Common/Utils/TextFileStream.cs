using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ACS_Common.Utils
{
    /// <summary>
    /// 带索引搜索树的文本流，可提高大文本随机行读取效率
    /// </summary>
    public partial class TextFileStream : IDisposable, IEnumerable<string>
    {
        private const string Tag = nameof(TextFileStream);

        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;
        /// <summary>
        /// 一次读取缓存大小
        /// </summary>
        private static int _byteBufferSize = 1024*1024;
        
        /// <summary>
        /// Start of Line offset
        /// 行首偏移索引节点
        /// </summary>
        private class SOLOffsetNode
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
                Debug.Log(sb.ToString());
                Left?.Print(depth + 1);
                Right?.Print(depth + 1);
            }
        }
        // 索引树根节点
        private SOLOffsetNode _SOLOffsetIdxTreeRoot;
        private Stream _stream;
        private Encoding encoding;
        private byte[] _byteBuffer;
        private StringBuilder _sb;

        private long _totalLine;
        public TextFileStream(string textFilePath)
        {
            LogMethod($"constructor, textFilePath: {textFilePath}");

            FileStream fs = null;
            try
            {
                fs = new FileStream(textFilePath, FileMode.Open);
            }
            catch(IOException e)
            {
                LogErr($"constructor, read file failed with message: \n{e}");
            }

            _stream = fs;
            _byteBuffer = new byte[_byteBufferSize];
            _sb = new StringBuilder();
            BuildLineIdx();
        }
        
        public void Dispose()
        {
            _stream?.Dispose();
            _SOLOffsetIdxTreeRoot = null;
            encoding = default;
            _byteBuffer = null;
            _sb = null;
        }

        private delegate bool ActionEachChunk(long chunkIdx, long bytesRead);
        private delegate bool ActionEachChar(char c, long offset);
        private delegate bool ActionEachLine(long lineIdx, long offset);
        private struct ReadPerCharResult
        {
            public long ChunkCnt;
            public long CharCnt;
            public char LastChar;
            public bool Interrupted;
            public override string ToString()
            {
                return $"[ReadPerCharResult: ChunkCnt: {ChunkCnt}, CharCnt: {CharCnt}, LastChar: [{LastChar}], Interrupted: {Interrupted}]";
            }
        }
        private struct ReadPerLineResult
        {
            public long ChunkCnt;
            public long LineCnt;
            public char LastChar;
            public bool Interrupted;
            public override string ToString()
            {
                return $"[ReadPerCharResult: ChunkCnt: {ChunkCnt}, LineCnt: {LineCnt}, LastChar: [{LastChar}], Interrupted: {Interrupted}]";
            }
        }
        /// <summary>
        /// 逐字符读取steam，不重置steam位置
        /// </summary>
        /// <param name="actionEachChunk"></param>
        /// <param name="actionEachChar"></param>
        /// <returns></returns>
        private ReadPerCharResult ReadEachChar(ActionEachChunk actionEachChunk, ActionEachChar actionEachChar)
        {
            LogMethod($"ReadEachChar, encoding: {encoding}");
            var initPos = _stream.Position;
            var currentChar = NULL;
            int bytesRead;
            var chunkCnt = 0L;
            if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while ((bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length)) > 0)
                {
                    // LogInfo($"ReadEachChar, begin of chunk read, chunkCnt: {chunkCnt}, bytesRead: {bytesRead}");
                    if (null != actionEachChunk && !actionEachChunk.Invoke(chunkCnt, bytesRead))
                        return new ReadPerCharResult()
                        {
                            ChunkCnt = chunkCnt,
                            LastChar = currentChar,
                            Interrupted = true
                        };
                    
                    for (var i = 0; i < bytesRead; i++)
                    {
                        currentChar = (char)_byteBuffer[i];
                        // LogInfo($"ReadEachChar, read {i}'s char [{currentChar}], chunkCnt: {chunkCnt}");
                        if (null != actionEachChar && !actionEachChar.Invoke(currentChar, initPos + chunkCnt * _byteBuffer.Length + i))
                            return new ReadPerCharResult()
                            {
                                ChunkCnt = chunkCnt,
                                CharCnt = chunkCnt * _byteBuffer.Length + i,
                                LastChar = currentChar,
                                Interrupted = true
                            };
                    }
                    chunkCnt++;
                    // LogInfo($"ReadEachChar, chunk read end 0, chunkCnt: {chunkCnt}");
                }
            }
            else
            {
                var charBuffer = new char[_byteBuffer.Length];
                while ((bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length)) > 0)
                {
                    // LogInfo($"ReadEachChar, begin of chunk read, chunkCnt: {chunkCnt}, bytesRead: {bytesRead}");
                    if (null != actionEachChunk && !actionEachChunk.Invoke(chunkCnt, bytesRead))
                        return new ReadPerCharResult()
                        {
                            ChunkCnt = chunkCnt,
                            CharCnt = chunkCnt * _byteBuffer.Length,
                            LastChar = currentChar,
                            Interrupted = true
                        };
                    var charCount = encoding.GetChars(_byteBuffer, 0, bytesRead, charBuffer, 0);
                    for (var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];
                        // LogInfo($"ReadEachChar, read {i}'s char [{currentChar}], chunkCnt: {chunkCnt}");
                        if (null != actionEachChar && !actionEachChar.Invoke(currentChar, initPos + chunkCnt * _byteBuffer.Length + i))
                            return new ReadPerCharResult()
                            {
                                ChunkCnt = chunkCnt,
                                CharCnt = chunkCnt * _byteBuffer.Length + i,
                                LastChar = currentChar,
                                Interrupted = true
                            };
                    }
                    chunkCnt++;
                    // LogInfo($"ReadEachChar, chunk read end 1, chunkCnt: {chunkCnt}");
                }
            }
            // 读到stream结束
            return new ReadPerCharResult()
            {
                ChunkCnt = chunkCnt,
                CharCnt = _stream.Position - initPos,
                LastChar = currentChar,
                Interrupted = false
            };
        }

        /// <summary>
        /// 逐行读取steam，不重置steam位置
        /// 每行内容存放在_sb中，每行内容仅在ActionEachLine回调中有效
        /// </summary>
        /// <param name="onChunkStart">每个buffer开始时调用</param>
        /// <param name="onLineStart">每行第一个字符调用</param>
        /// <param name="onLineEnd">每行EOL调用</param>
        /// <returns></returns>
        private ReadPerLineResult ReadEachLine(ActionEachChunk onChunkStart, ActionEachLine onLineStart, ActionEachLine onLineEnd)
        {
            LogMethod($"ReadEachLine, encoding: {encoding}");
            var lineCount = 0L;
            var detectedEOL = NULL;
            _sb.Clear();
            var actionEachChar = new ActionEachChar((char c, long offset) =>
            {
                // 每行第一个有效字符
                if (_sb.Length == 0 && (c == detectedEOL || c is not (LF or CR or NULL)) &&
                    null != onLineStart && !onLineStart.Invoke(lineCount, offset)) return false;
                // LogInfo($"ReadEachLine, ActionEachChar, c [{c}], offset: {offset}");
                if (c is not (LF or CR or NULL)) _sb.Append(c);
                if (detectedEOL != NULL)
                {
                    if (c == detectedEOL)
                    {
                        if (null != onLineEnd && !onLineEnd(lineCount, offset)) return false;
                        lineCount++;
                        _sb.Clear();
                        // LogInfo($"ReadEachLine, ActionEachChar, line end, offset: {offset}");
                    }
                }
                else if (c is LF or CR)
                {
                    if (null != onLineEnd && !onLineEnd(lineCount, offset)) return false;
                    detectedEOL = c;
                    lineCount++;
                    _sb.Clear();
                    // LogInfo($"ReadEachLine, EOL detected, detectedEOL: [{detectedEOL}]");
                }
                return true;
            });
            var readPerCharResult = ReadEachChar(onChunkStart, actionEachChar);
            if (readPerCharResult.LastChar is not (LF or CR or NULL))
            {
                if (null != onLineEnd) onLineEnd(lineCount, readPerCharResult.CharCnt);
                lineCount++;
            }
            return new ReadPerLineResult()
            {
                ChunkCnt = readPerCharResult.ChunkCnt,
                LineCnt = lineCount,
                LastChar =  readPerCharResult.LastChar,
                Interrupted = readPerCharResult.Interrupted,
            };
        }
        
        #region index
        /// <summary>
        /// 构建行号索引
        /// </summary>
        private void BuildLineIdx()
        {
            LogMethod($"BuildLineIdx");
            if (null == _stream)
            {
                LogErr($"BuildLineIdx, null == _stream");
                return;
            }
            _SOLOffsetIdxTreeRoot = new SOLOffsetNode
            {
                Line = -1L,
                SOLOffset = 0L
            };
            var node = _SOLOffsetIdxTreeRoot;
            // // 上一个chunk的最后一行记录
            var prevChunkLastLineIdx = 0L;
            var prevChunkLastLineStartOffset = 0L;

            var actionEachChunk = new ActionEachChunk((long chunkIdx, long bytesRead) =>
            {
                if (0 != prevChunkLastLineStartOffset)
                {
                    if (prevChunkLastLineIdx != node.Line)
                    {
                        // LogInfo($"add new idx node, LineIdx: {prevChunkLastLineIdx}, Offset: {prevChunkLastLineStartOffset}");
                        node.Right = new SOLOffsetNode
                        {
                            Line = prevChunkLastLineIdx,
                            SOLOffset = prevChunkLastLineStartOffset
                        };
                        node = node.Right;
                    }
                    // else LogInfo($"wow, such a big line.");
                }
                return true;
            });

            var onLineStart = new ActionEachLine((long lineIdx, long offset) =>
            {
                prevChunkLastLineIdx = lineIdx;
                prevChunkLastLineStartOffset = offset;
                return true;
            });
            
            var readPerLineResult = ReadEachLine(actionEachChunk, onLineStart, null);
            _totalLine = readPerLineResult.LineCnt;
            // LogInfo($"BuildLineIdx, chunkCnt: {readPerLineResult.ChunkCnt}, lineCount: {readPerLineResult.LineCnt}, LastChar: {readPerLineResult.LastChar}");
            _SOLOffsetIdxTreeRoot.Print();
            // 将_lineIdxRootNode转为BST
            _SOLOffsetIdxTreeRoot = LinkNodes2BST(_SOLOffsetIdxTreeRoot);
            _SOLOffsetIdxTreeRoot.Print();
        }

        /// <summary>
        /// 将node链表转为平衡搜索二叉树
        /// </summary>
        /// <param name="head"></param>
        private SOLOffsetNode LinkNodes2BST(SOLOffsetNode head, int depth = 0)
        {
            LogMethod($"LinkNodes2BST, depth: {depth}, head: {head}");
            if (null == head)
            {
                LogInfo($"{Tag} LinkNodes2BST, head is null");
                return null;
            }

            var result = FindMiddleNode(head, out var middlePrev, out var middle, out var end);
            // LogInfo($"LinkNodes2BST, result: {result}, depth: {depth}, middlePrev: {middlePrev}, middle: {middle}, end: {end}");
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
            LogMethod($"FindMiddleNode, head: {head}");
            middlePrev = null;
            middle = null;
            end = null;
            if (null == head)
            {
                LogErr($"FindMiddleNode, null == headNode");
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
            LogMethod($"SearchStartOfLineOffset, lineIdx: {lineIdx}");
            if (null == _SOLOffsetIdxTreeRoot)
            {
                LogErr($"SearchStartOfLineOffset, idx not built");
                return -1;
            }
            if (lineIdx > _totalLine)
            {
                LogErr($"SearchStartOfLineOffset, lineIdx {lineIdx} out of range, total line: {_totalLine}");
                return -1;
            }
            if (lineIdx < 0)
            {
                LogErr($"SearchStartOfLineOffset, lineIdx {lineIdx} out of range, Non-negative number required.");
                return -1;
            }
            var node = _SOLOffsetIdxTreeRoot;
            var prev = node;
            // step 1 搜索比lineIdx小的行的偏移作为搜索起始位置
            // var prevLineIdx = lineIdx - 1;
            while (true)
            {
                if (node.Line < lineIdx)
                {
                    if (null == node.Right)
                    {
                        break;
                    }
                    prev = node;
                    node = node.Right;
                }
                else if (node.Line > lineIdx)
                {
                    if (null == node.Left)
                    {
                        node = prev;
                        break;
                    }
                    prev = node;
                    node = node.Left;
                }
                // equal
                else break;
            }
            LogInfo($"SearchStartOfLineOffset, step 1 finish, node: {node}");
            
            // step 2 往后找到第lineIdx的位置
            _stream.Position = node.SOLOffset;
            var pos = node.SOLOffset;
            if (node.Line != lineIdx)
            {
                var readEachLineResult = ReadEachLine(null, new ActionEachLine((long idx, long offset) =>
                {
                    LogInfo($"SearchStartOfLineOffset step 2 ActionEachLine, idx: {idx}, offset: {offset}");
                    pos = offset;
                    if (idx == lineIdx)
                    {
                        LogInfo(
                            $"SearchStartOfLineOffset, ActionEachLine, find target, lineIdx: {idx}, offset: {offset}");
                    }
                    return idx < lineIdx;
                }), null);
                if (!readEachLineResult.Interrupted)
                {
                    LogErr($"SearchStartOfLineOffset something wrong");
                    return -1;
                }
            }
            LogInfo($"SearchStartOfLineOffset, step 2 finish, pos: {pos}");
            return pos;
        }
        
        #endregion // index
        
        #region IEnumerator

        public IEnumerator<string> GetEnumerator()
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

        /// <summary>
        /// 从特定行开始获得迭代器
        /// </summary>
        /// <param name="lineIdxOffset"></param>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator(long startLine)
        {
            LogMethod($"GetEnumerator, startLine: {startLine}");
            if (null == _SOLOffsetIdxTreeRoot)
            {
                LogErr($"SearchStartOfLineOffset, idx not built");
                yield break;
            }
            if (startLine > _totalLine)
            {
                LogErr($"SearchStartOfLineOffset, startLine {startLine} out of range, total line: {_totalLine}");
                yield break;
            }
            if (startLine < 0)
            {
                LogErr($"SearchStartOfLineOffset, startLine {startLine} out of range, Non-negative number required.");
                yield break;
            }
            var offset = SearchStartOfLineOffset(startLine);
            if (offset < 0)
            {
                LogErr($"SearchStartOfLineOffset, startLine {startLine}, indexing failed.");
                yield break;
            }
            _stream.Position = offset;
            LogInfo($"GetEnumerator, _stream.Position: {_stream.Position}");
            var sr = new StreamReader(_stream);
            while (true)
            {
                var line = sr.ReadLine();
                if (null == line) yield break;
                yield return line;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion // IEnumerator

        protected void LogMethod(string s)
        {
            Debug.Log($"{Tag} {s} //--------------------------------------------------------------------------");
        }
        
        protected void LogInfo(string s)
        {
            Debug.Log($"{Tag} {s}");
        }

        protected void LogErr(string s)
        {
            LogErr($"{Tag} {s}");
        }
        
        protected void LogWarn(string s)
        {
            Debug.LogWarning($"{Tag} {s}");
        }
    }
}