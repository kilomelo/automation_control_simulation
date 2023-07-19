using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ACS_Common.Base;
using ACS_Common.GCode;

namespace ACS_Common.MainBoard
{
    public class GCodeBuffer : ACS_Object, IDisposable
    {
        // protected override bool LogInfoEnable => false;
        public GCommandStream Source;
        private Queue<GCommand> _queue;
        // 读取数据间隔
        private const int READ_INTERVAL = 10;
        // 检查源数据流是否可用间隔
        private const int CHECK_SOURCE_INTERVAL = 100;
        private int _bufferSize = 20;
        private long _readPos;
        private CancellationTokenSource _ts;

        public GCodeBuffer(GCommandStream source)
        {
            const string m = nameof(GCodeBuffer);
            if (null == source)
            {
                LogErrStatic(m, m, "null == source");
            }
            Source = source;
            _queue = new Queue<GCommand>();
        }

        public void Start()
        {
            const string m = nameof(Start);
            if (null == Source)
            {
                LogErr(m, "null == Source");
                return;
            }
            if (null != _ts)
            {
                LogErr(m, "task is running");
                return;
            }
            _ts = new CancellationTokenSource();
            _readPos = 0;
            Task.Run(() => {
                LogInfo("readTask", "Start read");
                while (true)
                {
                    if (_ts.Token.IsCancellationRequested)
                    {
                        LogInfo("readTask", $"canceled");
                        break;
                    }
                    if (_queue.Count >= _bufferSize || !Source.IndexBuilt)
                    {
                        // LogInfo("readTask", $"buffer queue is full, _readPos: {_readPos}");
                        Thread.Sleep(CHECK_SOURCE_INTERVAL);
                        continue;
                    }
                    LogInfo(m, $"read {_readPos} command from source");
                    var command = Source.GetCommand(_readPos++);
                    // 读取到了流末尾
                    if (null == command) break;
                    if (!(command.CommandType is Def.EGCommandType.Invalid or Def.EGCommandType.None))
                    {
                        // LogInfo("readTask", $"_queue.Count: {_queue.Count}, {_readPos}'s command {command}");
                        lock(_queue) _queue.Enqueue(command);
                        Thread.Sleep(READ_INTERVAL);
                    }
                }
                LogInfo("readTask", $"Read done, cnt: {_readPos}");
                _readPos = -1;
                _queue.Clear();
            }, _ts.Token);
        }

        public void Stop()
        {
            const string m = nameof(Stop);
            LogMethod(m);
            _ts?.Cancel();
            _ts = null;
        }

        /// <summary>
        /// 获得一条命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns>如果当前没有执行读取线程，则返回-1；否则返回当前已读取存入buffer的行数</returns>
        public long GetGCommand(out GCommand command)
        {
            const string m = nameof(GetGCommand);
            // LogMethod(m);
            command = null;
            if (null == _ts) return -1;
            // LogInfo(m, $"_readTask.Status: {_readTask.Status}");
            if (null == _queue)
            {
                LogErr(m, "null == _queue");
                return -1;
            }

            lock(_queue)
            {
                if (_queue.Count == 0)
                {
                    LogInfo(m, "buffer queue is empty");
                }
                else
                {
                    command = _queue.Dequeue();
                }
                // LogInfo(m, $"command: {(command?.ToString()??"is null")}, return value: {_readPos}");
                return _readPos;
            }
        }

        public void Dispose()
        {
            Stop();
            Source = null;
        }
    }
}