using System.Threading;
using System.Threading.Tasks;
using ACS_Common.GCode;

namespace ACS_Common.MainBoard
{
    /// <summary>
    /// 3d打印主板 处理命令逻辑
    /// </summary>
    public partial class PrinterMainBoard
    {
        private GCommand _currentCommand;
        private GCommand _nextCommand;

        // 上一条命令执行完毕的时间戳
        private long _lastCommandFinishTimeStamp;

        private async Task RunGCommand(GCommand command, CancellationToken ct)
        {
            const string m = nameof(RunGCommand);
            // LogMethod(m, $"command: {command}");
            if (null == command)
            {
                LogErr(m, "null == command");
                SetCommandExecuteProgress(1f);
                return;
            }
            _sw.Start();
            SetExecutingCommandLineIdx(command.Index);
            switch (command.CommandType)
            {
                case Def.EGCommandType.Invalid:
                case Def.EGCommandType.None:
                    SetCommandExecuteProgress(1f);
                    break;
                case Def.EGCommandType.M:
                    while (true)
                    {
                        var delta = 0.01f * Random.Range(1f, 5f);
                        SetCommandExecuteProgress(_status.ExecutingProgress + delta);
                        if (_status.ExecutingProgress >= 1f) break;
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(10);
                    }
                    break;
                case Def.EGCommandType.G:
                    while (true)
                    {
                        var delta = 0.015f * Random.Range(1f, 5f);
                        SetCommandExecuteProgress(_status.ExecutingProgress + delta);
                        if (_status.ExecutingProgress >= 1f) break;
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(10);
                    }
                    break;
            }
            _sw.Stop();
            _status.ExecuteTimeMilliseconds = _sw.ElapsedMilliseconds;
            var timeElapsedMillisec = _status.ExecuteTimeMilliseconds - _lastCommandFinishTimeStamp;
            command.ExecuteTimeMilliSec = timeElapsedMillisec;
            // LogInfo(m, $"command [{command}] execute time: {timeElapsedMillisec}");
            _lastCommandFinishTimeStamp = _status.ExecuteTimeMilliseconds;
        }
    }
}