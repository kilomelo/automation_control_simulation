using System;
using System.Collections;
using System.Collections.Generic;
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

        private IEnumerator RunGCommand(GCommand command)
        {
            const string m = nameof(RunGCommand);
            // LogMethod(m, $"command: {command}");
            if (null == command)
            {
                LogErr(m, "null == command");
                SetCommandExecuteProgress(1f);
                yield break;
            }
            _sw.Start();
            switch (command.CommandType)
            {
                case Def.EGCommandType.Invalid:
                case Def.EGCommandType.None:
                    SetCommandExecuteProgress(1f);
                    break;
                case Def.EGCommandType.M:
                    while (true)
                    {
                        SetCommandExecuteProgress(_status.ExecutingProgress + 0.02f);
                        if (_status.ExecutingProgress >= 1f) break;
                        yield return 0;
                    }
                    break;
                case Def.EGCommandType.G:
                    while (true)
                    {
                        SetCommandExecuteProgress(_status.ExecutingProgress + 0.05f);
                        if (_status.ExecutingProgress >= 1f) break;
                        yield return 0;
                    }
                    break;
            }
            _sw.Stop();
            _status.ExecuteTimeMilliseconds = _sw.ElapsedMilliseconds;
            var timeElapsedMillisec = _status.ExecuteTimeMilliseconds - _lastCommandFinishTimeStamp;
            command.ExecuteTimeMilliSec = timeElapsedMillisec;
            LogInfo(m, $"command [{command}] execute time: {timeElapsedMillisec}");
            _lastCommandFinishTimeStamp = _status.ExecuteTimeMilliseconds;
        }
    }
}