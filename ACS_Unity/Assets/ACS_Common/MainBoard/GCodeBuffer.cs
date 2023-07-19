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
        // 读取数据间隔
        private const int READ_INTERVAL = 10;
        // 检查源数据流是否可用间隔
        private const int CHECK_SOURCE_INTERVAL = 100;
        public GCommandStream Source;
        private Queue<GCommand> _queue;
        private int _bufferSize = 20;
        private Task _readTask;
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
            if (null != _readTask)
            {
                LogErr(m, "task is running");
                return;
            }
            _ts = new CancellationTokenSource();
            _readPos = 0;
            _readTask = Task.Run(() => {
                LogInfo("readTask", "Start read");
                while (true)
                {
                    if (_ts.Token.IsCancellationRequested)
                    {
                        LogInfo("readTask", $"canceled");
                        _readPos = -1;
                        _queue.Clear();
                        break;
                    }
                    if (_queue.Count >= _bufferSize || !Source.IndexBuilt)
                    {
                        // LogInfo("readTask", $"buffer queue is full, _readPos: {_readPos}");
                        Thread.Sleep(CHECK_SOURCE_INTERVAL);
                        continue;
                    }
                    var command = Source.GetCommand(_readPos++);
                    if (null == command) break;
                    if (!(command.CommandType is Def.EGCommandType.Invalid or Def.EGCommandType.None))
                    {

                        lock(_queue)
                        {
                            _queue.Enqueue(command);
                            Thread.Sleep(READ_INTERVAL);
                            // LogInfo("readTask", $"_queue.Count: {_queue.Count}, {_readPos}'s command {command}");
                        }
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
            if (null != _readTask)
            {
                LogInfo(m, "cancel read task");
                _ts.Cancel();
            }
            _readTask = null;
        }

        public long GetGCommand(out GCommand command)
        {
            const string m = nameof(GetGCommand);
            command = null;
            if (null == _queue)
            {
                LogErr(m, "null == _queue");
                return -1;
            }
            lock(_queue)
            {
                if (_queue.Count == 0)
                {
                    // LogInfo(m, "buffer queue is empty");
                }
                else
                {
                    command = _queue.Dequeue();
                }
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