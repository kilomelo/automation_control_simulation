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
        private static int _byteBufferSize = 1024 * 1024;
        
        /// <summary>
        /// 行号索引节点
        /// </summary>
        private class LineIdxNode
        {
            public long LineIdx;
            public long Offset;
            public LineIdxNode Left;
            public LineIdxNode Right;

            public override string ToString()
            {
                return $"[LineIdx: {LineIdx}, Offset: {Offset}, Left: {(null == Left ? "NONE" : "Node")}, Right: {(null == Right ? "NONE" : "Node")}]";
            }

            public void Print(int depth = 0)
            {
                const string tab = "  ";
                var sb = new StringBuilder();
                for (var i = 0; i < depth; i++) sb.Append(tab);
                sb.Append($"LineIdx: {LineIdx}, Offset: {Offset}");
                if (null == Left) sb.Append(", left is none");
                if (null == Right) sb.Append(", right is none");
                Debug.Log(sb.ToString());
                Left?.Print(depth + 1);
                Right?.Print(depth + 1);
            }
        }

        private LineIdxNode _lineIdxRoot;
        private Stream _stream;
        private Encoding encoding;
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
            BuildLineIdx();
        }

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
            var lineCount = 0L;
            var byteBuffer = new byte[_byteBufferSize];
            var detectedEOL = NULL;
            var currentChar = NULL;
            int bytesRead;

            _lineIdxRoot = new LineIdxNode
            {
                LineIdx = 0L,
                Offset = 0L
            };
            var node = _lineIdxRoot;
            var chunkCnt = 0;
            var prevChunkLastLineIdx = 0L;
            var prevChunkLastLineOffset = 0L;
            if (encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while ((bytesRead = _stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    if (0 != prevChunkLastLineOffset)
                    {
                        node.Right = new LineIdxNode
                        {
                            LineIdx = prevChunkLastLineIdx,
                            Offset = prevChunkLastLineOffset
                        };
                        node = node.Right;
                    }

                    var lineEndPos = 0;
                    for (var i = 0; i < bytesRead; i++)
                    {
                        currentChar = (char)byteBuffer[i];

                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                                lineEndPos = i;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                            lineEndPos = i;
                        }
                    }
                    prevChunkLastLineIdx = lineCount;
                    prevChunkLastLineOffset = chunkCnt * byteBuffer.Length + lineEndPos;
                    chunkCnt++;
                    Debug.Log($"{Tag} chunk: {chunkCnt}, lineCount: {lineCount}");
                }
            } 
            else
            {
                var charBuffer = new char[byteBuffer.Length];

                while ((bytesRead = _stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    if (0 != prevChunkLastLineOffset)
                    {
                        node.Right = new LineIdxNode
                        {
                            LineIdx = prevChunkLastLineIdx,
                            Offset = prevChunkLastLineOffset
                        };
                        node = node.Right;
                    }
                    var charCount = encoding.GetChars(byteBuffer, 0, bytesRead, charBuffer, 0);
                    var lineEndPos = 0;
                    for (var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];

                        if (detectedEOL != NULL)
                        {
                            if (currentChar == detectedEOL)
                            {
                                lineCount++;
                                lineEndPos = i;
                            }
                        }
                        else if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                            lineEndPos = i;
                        }
                    }
                    prevChunkLastLineIdx = lineCount;
                    prevChunkLastLineOffset = chunkCnt * byteBuffer.Length + lineEndPos;
                    chunkCnt++;
                    Debug.Log($"{Tag} chunk: {chunkCnt}, lineCount: {lineCount}");
                }
            }

            if (currentChar != LF && currentChar != CR && currentChar != NULL)
            {
                lineCount++;
            }
            
            Debug.Log($"{Tag} BuildLineIdx, chunkCnt: {chunkCnt}, lineCount: {lineCount}");
            _lineIdxRoot.Print();
            // 将_lineIdxRootNode转为BST
            _lineIdxRoot = LinkNodes2BST(_lineIdxRoot);
            _lineIdxRoot.Print();
        }

        /// <summary>
        /// 将node链表转为平衡搜索二叉树
        /// </summary>
        /// <param name="head"></param>
        private LineIdxNode LinkNodes2BST(LineIdxNode head, int depth = 0)
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
        private int FindMiddleNode(LineIdxNode head, out LineIdxNode middlePrev, out LineIdxNode middle, out LineIdxNode end)
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

        public void Dispose()
        {
            _stream?.Dispose();
            _lineIdxRoot = null;
            encoding = default;
        }

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
        public IEnumerator<string> GetEnumerator(long lineIdxOffset)
        {
            // todo get byte offset
            _stream.Position = 0;
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
    }
}