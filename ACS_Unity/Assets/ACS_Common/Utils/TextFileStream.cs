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
    public class TextFileStream : IDisposable, IEnumerable<string>
    {
        private const string Tag = nameof(TextFileStream);

        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;
        /// <summary>
        /// 一次读取缓存大小
        /// </summary>
        private static int _byteBufferSize = 5;
        
        /// <summary>
        /// 行尾偏移索引节点
        /// </summary>
        private class EOFOffsetNode
        {
            public long Line;
            public long EOFOffset;
            public EOFOffsetNode Left;
            public EOFOffsetNode Right;

            public override string ToString()
            {
                return $"[Line: {Line}, EOFOffset: {EOFOffset}, Left: {(null == Left ? "NONE" : "Node")}, Right: {(null == Right ? "NONE" : "Node")}]";
            }

            public void Print(int depth = 0)
            {
                const string tab = "    ";
                var sb = new StringBuilder();
                for (var i = 0; i < depth; i++) sb.Append(tab);
                sb.Append($"LineIdx: {Line}, Offset: {EOFOffset}");
                if (null == Left) sb.Append(", left is none");
                if (null == Right) sb.Append(", right is none");
                Debug.Log(sb.ToString());
                Left?.Print(depth + 1);
                Right?.Print(depth + 1);
            }
        }
        // 索引树根节点
        private EOFOffsetNode _EOFOffsetIdxTreeRoot;
        private Stream _stream;
        private Encoding encoding;
        private byte[] _byteBuffer;
        private StringBuilder _sb;

        private long _totalLine;
        public TextFileStream(string textFilePath)
        {
            Debug.Log($"{Tag} constructor, textFilePath: {textFilePath}");

            FileStream fs = null;
            try
            {
                fs = new FileStream(textFilePath, FileMode.Open);
            }
            catch(IOException e)
            {
                Debug.LogError($"{Tag} constructor, read file failed with message: \n{e}");
            }

            _stream = fs;
            _byteBuffer = new byte[_byteBufferSize];
            _sb = new StringBuilder();
            BuildLineIdx();
        }
        
        public void Dispose()
        {
            _stream?.Dispose();
            _EOFOffsetIdxTreeRoot = null;
            encoding = default;
            _byteBuffer = null;
            _sb = null;
        }

        private delegate bool ActionEachChunk(long chunkIdx, long bytesRead);
        private delegate bool ActionEachChar(char c, long offset);
        private delegate bool ActionEachLine(long lineIdx, long EOFoffset);
        private struct ReadPerCharResult
        {
            public long ChunkCnt;
            public char LastChar;
            public bool Interrupted;
        }
        private struct ReadPerLineResult
        {
            public long ChunkCnt;
            public long LineCnt;
            public char LastChar;
            public bool Interrupted;
        }
        /// <summary>
        /// 逐字符读取steam，不重置steam位置
        /// </summary>
        /// <param name="actionEachChunk"></param>
        /// <param name="actionEachChar"></param>
        /// <returns></returns>
        private ReadPerCharResult ReadEachChar(ActionEachChunk actionEachChunk, ActionEachChar actionEachChar)
        {
            Debug.Log($"{Tag} ReadEachChar, encoding: {encoding}");
            var currentChar = NULL;
            int bytesRead;
            var chunkCnt = 0L;
            if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while ((bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length)) > 0)
                {
                    Debug.Log($"{Tag} ReadEachChar, begin of chunk read, chunkCnt: {chunkCnt}, bytesRead: {bytesRead}");
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
                        Debug.Log($"{Tag} ReadEachChar, read {i}'s char [{currentChar}], chunkCnt: {chunkCnt}");
                        if (null != actionEachChar && !actionEachChar.Invoke(currentChar, chunkCnt * _byteBuffer.Length + i))
                            return new ReadPerCharResult()
                            {
                                ChunkCnt = chunkCnt,
                                LastChar = currentChar,
                                Interrupted = true
                            };
                    }
                    chunkCnt++;
                    Debug.Log($"{Tag} ReadEachChar, chunk read end, chunkCnt: {chunkCnt}");
                }
            }
            else
            {
                var charBuffer = new char[_byteBuffer.Length];
                while ((bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length)) > 0)
                {
                    Debug.Log($"{Tag} ReadEachChar, begin of chunk read, chunkCnt: {chunkCnt}, bytesRead: {bytesRead}");
                    if (null != actionEachChunk && !actionEachChunk.Invoke(chunkCnt, bytesRead))
                        return new ReadPerCharResult()
                        {
                            ChunkCnt = chunkCnt,
                            LastChar = currentChar,
                            Interrupted = true
                        };
                    var charCount = encoding.GetChars(_byteBuffer, 0, bytesRead, charBuffer, 0);
                    for (var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];
                        Debug.Log($"{Tag} ReadEachChar, read {i}'s char [{currentChar}], chunkCnt: {chunkCnt}");
                        if (null != actionEachChar && !actionEachChar.Invoke(currentChar, chunkCnt * _byteBuffer.Length + i))
                            return new ReadPerCharResult()
                            {
                                ChunkCnt = chunkCnt,
                                LastChar = currentChar,
                                Interrupted = true
                            };
                    }
                }
            }
            // 读到stream结束
            return new ReadPerCharResult()
            {
                ChunkCnt = chunkCnt,
                LastChar = currentChar,
                Interrupted = false
            };
        }

        /// <summary>
        /// 逐行读取steam，不重置steam位置
        /// 每行内容存放在_sb中，每行内容仅在ActionEachLine回调中有效
        /// </summary>
        /// <param name="actionEachChunk"></param>
        /// <param name="actionEachLine"></param>
        /// <returns></returns>
        private ReadPerLineResult ReadEachLine(ActionEachChunk actionEachChunk, ActionEachLine actionEachLine)
        {
            Debug.Log($"{Tag} ReadEachLine, encoding: {encoding}");
            var lineCount = 0L;
            var detectedEOL = NULL;
            _sb.Clear();
            // var chunkCnt = 0L;
            var actionEachChar = new ActionEachChar((char c, long offset) =>
            {
                Debug.Log($"{Tag} ReadEachLine, ActionEachChar, c [{c}], offset: {offset}");
                if (detectedEOL != NULL)
                {
                    if (c == detectedEOL)
                    {
                        if (null != actionEachLine && !actionEachLine(lineCount, offset)) return false;
                        lineCount++;
                        _sb.Clear();
                        Debug.Log($"{Tag} ReadEachLine, ActionEachChar, line end, offset: {offset}");
                    }
                    else _sb.Append(c);
                }
                else if (c is LF or CR)
                {
                    if (null != actionEachLine && !actionEachLine(lineCount, offset)) return false;
                    detectedEOL = c;
                    lineCount++;
                    _sb.Clear();
                    Debug.Log($"{Tag} ReadEachLine, EOL detected, detectedEOL: [{detectedEOL}]");
                }
                else _sb.Append(c);
                return true;
            });
            var readPerCharResult = ReadEachChar(actionEachChunk, actionEachChar);
            if (readPerCharResult.LastChar is not (LF or CR or NULL))
            {
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
            Debug.Log($"{Tag} BuildLineIdx");
            if (null == _stream)
            {
                Debug.LogError($"{Tag} BuildLineIdx, null == _stream");
                return;
            }
            _EOFOffsetIdxTreeRoot = new EOFOffsetNode
            {
                Line = -1L,
                EOFOffset = 0L
            };
            var node = _EOFOffsetIdxTreeRoot;
            // 上一个chunk的最后一行记录
            var prevChunkLastLineIdx = 0L;
            var prevChunkLastEOFOffset = 0L;

            var actionEachChunk = new ActionEachChunk((long chunkIdx, long bytesRead) =>
            {
                if (0 != prevChunkLastEOFOffset)
                {
                    if (prevChunkLastLineIdx != node.Line)
                    {
                        Debug.Log($"{Tag} add new idx node, LineIdx: {prevChunkLastLineIdx}, EOFOffset: {prevChunkLastEOFOffset}");
                        node.Right = new EOFOffsetNode
                        {
                            Line = prevChunkLastLineIdx,
                            EOFOffset = prevChunkLastEOFOffset
                        };
                        node = node.Right;
                    }
                    else Debug.Log($"{Tag} wow, such a big line.");
                }
                return true;
            });

            var actionEachLine = new ActionEachLine((long lineIdx, long EOFoffset) =>
            {
                prevChunkLastLineIdx = lineIdx;
                prevChunkLastEOFOffset = EOFoffset;
                return true;
            });
            
            var readPerLineResult = ReadEachLine(actionEachChunk, actionEachLine);
            _totalLine = readPerLineResult.LineCnt;
            Debug.Log($"{Tag} BuildLineIdx, chunkCnt: {readPerLineResult.ChunkCnt}, lineCount: {readPerLineResult.LineCnt}, LastChar: {readPerLineResult.LastChar}");
            _EOFOffsetIdxTreeRoot.Print();
            // 将_lineIdxRootNode转为BST
            _EOFOffsetIdxTreeRoot = LinkNodes2BST(_EOFOffsetIdxTreeRoot);
            _EOFOffsetIdxTreeRoot.Print();
        }

        /// <summary>
        /// 将node链表转为平衡搜索二叉树
        /// </summary>
        /// <param name="head"></param>
        private EOFOffsetNode LinkNodes2BST(EOFOffsetNode head, int depth = 0)
        {
            Debug.Log($"{Tag} LinkNodes2BST, depth: {depth}, head: {head}");
            if (null == head)
            {
                Debug.Log($"{Tag} LinkNodes2BST, head is null");
                return null;
            }

            var result = FindMiddleNode(head, out var middlePrev, out var middle, out var end);
            Debug.Log($"{Tag} LinkNodes2BST, result: {result}, depth: {depth}, middlePrev: {middlePrev}, middle: {middle}, end: {end}");
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
        private int FindMiddleNode(EOFOffsetNode head, out EOFOffsetNode middlePrev, out EOFOffsetNode middle, out EOFOffsetNode end)
        {
            Debug.Log($"{Tag} FindMiddleNode, head: {head}");
            middlePrev = null;
            middle = null;
            end = null;
            if (null == head)
            {
                Debug.LogError($"{Tag} FindMiddleNode, null == headNode");
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
            Debug.Log($"{Tag} SearchStartOfLineOffset, lineIdx: {lineIdx}");
            if (null == _EOFOffsetIdxTreeRoot)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, idx not built");
                return -1;
            }
            if (lineIdx > _totalLine)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, lineIdx {lineIdx} out of range, total line: {_totalLine}");
                return -1;
            }
            if (lineIdx < 0)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, lineIdx {lineIdx} out of range, Non-negative number required.");
                return -1;
            }
            var node = _EOFOffsetIdxTreeRoot;
            var prev = node;
            // step 1 搜索比lineIdx小的行的EOF偏移作为搜索起始位置
            var prevLineIdx = lineIdx - 1;
            while (true)
            {
                if (node.Line < prevLineIdx)
                {
                    if (null == node.Right)
                    {
                        break;
                    }
                    prev = node;
                    node = node.Right;
                }
                else if (node.Line > prevLineIdx)
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
            Debug.Log($"{Tag} SearchStartOfLineOffset, step 1 finish, node: {node}");
            
            // step 2 往后找到第prevLineIdx的EOF位置
            _stream.Position = node.EOFOffset;
            if (node.Line != prevLineIdx)
            {
                var pos = node.EOFOffset;
                var readEachLineResut = ReadEachLine(null, new ActionEachLine((long idx, long EOFOffset) =>
                {
                    Debug.Log($"{Tag} SearchStartOfLineOffset step 2 ActionEachLine, idx: {idx}, EOFOffset: {EOFOffset}");
                    pos = EOFOffset;
                    if (idx >= prevLineIdx)
                    {
                        Debug.Log(
                            $"{Tag} SearchStartOfLineOffset, ActionEachLine, find prevLineIdx, lineIdx: {idx}, EOFoffset: {EOFOffset}");
                    }

                    return idx < prevLineIdx;
                }));
                if (readEachLineResut.Interrupted) _stream.Position = pos;
                else
                {
                    Debug.Log($"{Tag} SearchStartOfLineOffset something wrong");
                    return -1;
                }
            }
            
            Debug.Log($"{Tag} SearchStartOfLineOffset, step 2 finish, _stream.Position: {_stream.Position}");

            // step 3 找到line行的第一个字符位置
            var detectedEOL = NULL;
            var finalOffset = _stream.Position;
            ReadEachChar(null, new ActionEachChar((c, offset) =>
            {
                Debug.Log($"{Tag} SearchStartOfLineOffset step 3 ActionEachChar, c: [{c}], offset: {offset}");
                if (detectedEOL != NULL)
                {
                    if (c == detectedEOL)
                    {
                        finalOffset += offset;
                        return false;
                    }
                    finalOffset += offset + 1;
                    return false;
                }
                else if (c is LF or CR)
                {
                    detectedEOL = c;
                    Debug.Log($"{Tag} SearchStartOfLineOffset, EOL detected, detectedEOL: [{detectedEOL}]");
                }
                return true;
            }));
            Debug.Log($"{Tag} SearchStartOfLineOffset, step 3 finish, finalOffset: {finalOffset}");

            // var actionEachChar = new ActionEachChar((char c, long offset) =>
            // {
            //     Debug.Log($"{Tag}, SearchStartOfLineOffset, ActionEachChar, c [{c}], offset: {offset}");
            //     return true;
            // });
            // var readEachCharResult = ReadEachChar(null, actionEachChar);
            // 超过了总行数
            return finalOffset;
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
            Debug.Log($"{Tag} GetEnumerator, startLine: {startLine}");
            if (null == _EOFOffsetIdxTreeRoot)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, idx not built");
                yield break;
            }
            if (startLine > _totalLine)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, startLine {startLine} out of range, total line: {_totalLine}");
                yield break;
            }
            if (startLine < 0)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, startLine {startLine} out of range, Non-negative number required.");
                yield break;
            }
            var offset = SearchStartOfLineOffset(startLine);
            if (offset < 0)
            {
                Debug.LogError($"{Tag} SearchStartOfLineOffset, startLine {startLine}, indexing failed.");
                yield break;
            }
            _stream.Position = offset;
            Debug.Log($"{Tag} GetEnumerator, _stream.Position: {_stream.Position}");
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
    }
}