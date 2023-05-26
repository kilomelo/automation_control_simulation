using System.Text.RegularExpressions;
using ACS_Common.Base;
using ACS_Common.Driver;
using ACS_Common.GCodeParser;
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

        private string testRegex = @"(?<=^|\n|\r)[GgMm]([1-9]+[0-9]*|0)\s[\s\S]*?(?=$|\n|\r)";
        // private Regex _gCodeRegex = new Regex();
        /// <summary>
        /// 发送GCode
        /// </summary>
        /// <param name="code"></param>
        public void SendGCode(GCommand command)
        {
            Debug.Log($"{Tag} SendGCode, code: {command}");
            
        }

        public TextAsset testCodeFile;

        private void OnEnable()
        {
            if (null == testCodeFile)
            {
                Debug.LogError($"{Tag} test code file is null");
                return;
            }
            Debug.Log("textFile content:");
            Debug.Log(testCodeFile.text);
            var testCommands = GTools.GCommandsFromText(testCodeFile.text);
        }
    }
}