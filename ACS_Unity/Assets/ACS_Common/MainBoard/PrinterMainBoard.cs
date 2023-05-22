using System.Text.RegularExpressions;
using ACS_Common.Base;
using ACS_Common.Driver;
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

        public string testRegex = @"\bS\S*";
        // private Regex _gCodeRegex = new Regex();
        /// <summary>
        /// 发送GCode
        /// </summary>
        /// <param name="code"></param>
        public void SendGCode(string code)
        {
            Debug.Log($"{Tag} SendGCode, code: {code}");

            // var matches = _gCodeRegex.Matches(code);
            var matches = Regex.Matches(code, testRegex);
            if (matches.Count > 0)
            {
                foreach (var match in matches)
                {
                    Debug.Log($"{Tag} match: {match}");
                }
            }
            else
            {
                Debug.LogError($"{Tag} SendGCode with invalid code");
            }
        }

        public string testCode;

        private void OnEnable()
        {
            SendGCode(testCode);
        }
    }
}