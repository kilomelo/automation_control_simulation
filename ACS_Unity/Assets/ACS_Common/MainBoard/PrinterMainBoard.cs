using ACS_Common.Base;
using ACS_Common.Driver;
using ACS_Common.GCode;
using UnityEngine;

namespace ACS_Common.MainBoard
{
    /// <summary>
    /// 3d打印主板
    /// </summary>
    public class PrinterMainBoard : ACS_Behaviour, IGCommandStreamHolder
    {
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverX;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverY;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverZ;
        [SerializeField] private StepMotorDriverBehaviour _stepMotorDriverE;

        private GCommandStream _commandStream;
        public GCommandStream Stream => _commandStream;

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

            _commandStream = new GCommandStream(GCodeFilePath);
        }

        private void OnDestroy()
        {
            _commandStream?.Dispose();
        }
    }
}