using System.Collections.Generic;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// GCodeVide的Cache逻辑
    /// </summary>
    public partial class GCodeView
    {
        /// <summary>
        /// 缓存大小
        /// </summary>
        private int _cacheCapacity = 100;
        /// <summary>
        /// 文本缓存，key为行号
        /// </summary>
        private Dictionary<long, string> _textLineCache = new Dictionary<long, string>();
        /// <summary>
        /// 文本缓存队列，行号
        /// </summary>
        private Queue<long> _textLineCacheQueue = new Queue<long>();

        private void CacheText(long lineIdx, string text, bool checkCapacity = false)
        {
            _textLineCache[lineIdx] = text;
            _textLineCacheQueue.Enqueue(lineIdx);
            if (checkCapacity)
            {
                PreserveCacheCapacity();
            }
        }
        /// <summary>
        /// 保持cache容量
        /// </summary>
        private void PreserveCacheCapacity()
        {
            while (_textLineCacheQueue.Count > _cacheCapacity)
            {
                var removeCacheLineIdx = _textLineCacheQueue.Dequeue();
                _textLineCache.Remove(removeCacheLineIdx);
            }
        }
    }
}