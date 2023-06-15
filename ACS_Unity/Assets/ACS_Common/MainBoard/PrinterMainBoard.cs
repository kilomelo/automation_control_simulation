using System.Collections;
using ACS_Common.Base;
using ACS_Common.Driver;
using ACS_Common.GCode;
using UnityEngine;

namespace ACS_Common.MainBoard
{
    /// <summary>
    /// 3d打印主板
    /// </summary>
    public class PrinterMainBoard : ACS_Behaviour
    {
        private const string Tag = nameof(PrinterMainBoard);

        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverX;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverY;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverZ;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverE;

        public GCommandStream CommandStream;

        /// <summary>
        /// 发送GCode
        /// </summary>
        /// <param name="code"></param>
        public void SendGCode(GCommand command)
        {
            LogMethod($"SendGCode, code: {command}");
            
        }

        public string GCodeFilePath;

        private void OnEnable()
        {
            const string m = nameof(OnEnable);
            if (string.IsNullOrEmpty(GCodeFilePath))
            {
                LogErr(m, $"test gcode file path is empty");
                return;
            }

            CommandStream = new GCommandStream(GCodeFilePath);
            // StartCoroutine(CommandStream.TestReadEachChar());
            // StartCoroutine(CommandStream.TestReadEachLine());
            // StartCoroutine(ReadLines());
        }

        // private IEnumerator ReadLines()
        // {
        //     const string m = nameof(ReadLines);
        //     LogMethod(m);
        //     if (null == CommandStream)
        //     {
        //         LogErr(m, "CommandStream is null");
        //         yield break;
        //     }
        //     while (!CommandStream.IndexBuilt)
        //     {
        //         LogInfo(m, "wait for index build");
        //         yield return 0;
        //     }
        //     LogInfo(m, "index build finished");
        //     using var itr = CommandStream.GetEnumerator(654235);
        //     var i = 0;
        //     while (itr.MoveNext())
        //     {
        //         i++;
        //         LogInfo(m, $"{i}'s line: {itr.Current}");
        //         if (i > 24) break;
        //         // LogInfo(m, $"foreach line");
        //         yield return 0;
        //     }
        //     LogInfo(m, $"end");
        // }
        
        protected void LogMethod(string methodName, string info = null)
        {
            Debug.Log($"# {Tag} # <{methodName}> {info} //--------------------------------------------------------------------------");
        }
        
        protected void LogInfo(string methodName, string info)
        {
            Debug.Log($"# {Tag} # <{methodName}> {info}");
        }

        protected void LogErr(string methodName, string info)
        {
            Debug.LogError($"# {Tag} # <{methodName}> {info}");
        }
        
        protected void LogWarn(string methodName, string info)
        {
            Debug.LogWarning($"# {Tag} # <{methodName}> {info}");
        }
    }
}