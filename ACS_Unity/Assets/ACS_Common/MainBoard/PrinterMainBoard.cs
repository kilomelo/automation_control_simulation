using System.Collections;
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

        public GCommandStream CommandStream;

        /// <summary>
        /// 发送GCode
        /// </summary>
        /// <param name="code"></param>
        public void SendGCode(GCommand command)
        {
            Debug.Log($"{Tag} SendGCode, code: {command}");
            
        }

        // public TextAsset testCodeFile;

        // private void OnEnable()
        // {
        //     if (null == testCodeFile)
        //     {
        //         Debug.LogError($"{Tag} test code file is null");
        //         return;
        //     }
        //     Debug.Log("textFile content:");
        //     Debug.Log(testCodeFile.text);
        //     var testCommands = GTools.GCommandsFromText(testCodeFile.text);
        // }

        public string GCodeFilePath;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(GCodeFilePath))
            {
                Debug.LogError($"{Tag}, test gcode file path is empty");
                return;
            }

            CommandStream = GTools.GCommandFromFile(GCodeFilePath);
            // StartCoroutine(CommandStream.TestReadEachChar());
            StartCoroutine(CommandStream.TestReadEachLine());
            // CommandStream.TestReadEachLine();
            // StartCoroutine(ReadLines());
        }

        private IEnumerator ReadLines()
        {
            Debug.Log($"{Tag}, ReadLines start");
            
            using var itor = CommandStream.GetEnumerator(0);
            var i = 0;
            while (itor.MoveNext())
            {
                Debug.Log($"{Tag} {i++}'s line: {itor.Current}");
                yield return 0;
            }
            // var i = 0;
            // foreach (var line in CommandStream)
            // {
            //     Debug.Log($"{Tag} {i}'s line: {line}");
            //     i++;
            //     yield return 0;
            // }
            Debug.Log($"{Tag}, ReadLines end");
        }
    }
}