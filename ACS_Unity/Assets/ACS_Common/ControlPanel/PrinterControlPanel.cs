using System;
using ACS_Common.Base;
using ACS_Common.GCode.View;
using ACS_Common.MainBoard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ACS_Common.ControlPanel
{
    /// <summary>
    /// 打印机控制面板
    /// </summary>
    public class PrinterControlPanel : ACS_Behaviour
    {
        [SerializeField] private PrinterMainBoard _mainBoard;
        [SerializeField] private PrinterGCodeView _gCodeView;
        [SerializeField] private TextMeshProUGUI _statusDisplay;

        [SerializeField] private Button _loadGCodeFileBtn;
        [SerializeField] private Button _executeGCodeFileBtn;
        [SerializeField] private Button _stopBtn;

        private TextMeshProUGUI _executeGCodeFileBtnLabel;
        
        public string TestGCodeFilePath;

        private void Start()
        {
            const string m = nameof(Start);

            if (null == _mainBoard)
            {
                LogErr(m, "_mainBoard is null");
                return;
            }
            _mainBoard.OnPrintProgressUpdate += OnPrintProgressUpdate;
            _mainBoard.OnStateChange += OnStateChange;
            if (null == _gCodeView) LogErr(m, "_gCodeView is null");
            else _gCodeView.SetPrinter(_mainBoard);
            
            if (null != _loadGCodeFileBtn) _loadGCodeFileBtn.onClick.AddListener(() =>
            {
                if (null != _mainBoard) _mainBoard.LoadGCodeFile(TestGCodeFilePath);
            });
            else LogErr(m, "_loadGCodeFileBtn is null");
            if (null != _executeGCodeFileBtn) {
                _executeGCodeFileBtn.onClick.AddListener(() =>
                {
                    if (null == _mainBoard) return;
                    switch (_mainBoard.Status.State)
                    {
                        case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Printing:
                            _mainBoard.Pause();
                            break;
                        case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Pause:
                            _mainBoard.Continue();
                            break;
                        case PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Idle:
                            _mainBoard.Execute();
                            break;
                    }
                });
                _executeGCodeFileBtnLabel =
                    _executeGCodeFileBtn.gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            else LogErr(m, "_executeGCodeFileBtn is null");
            if (null != _stopBtn) _stopBtn.onClick.AddListener(() =>
            {
                if (null != _mainBoard) _mainBoard.Stop();
            });
            else LogErr(m, "_stopBtn is null");

            if (null != _statusDisplay) _statusDisplay.text = string.Empty;
            else LogErr(m, "_statusDisplay is null");
        }

        protected override void Clear()
        {
            base.Clear();
            if (null != _mainBoard)
            {
                _mainBoard.OnPrintProgressUpdate -= OnPrintProgressUpdate;
                _mainBoard.OnStateChange -= OnStateChange;
            }
        }

        private void OnPrintProgressUpdate()
        {
            const string m = nameof(OnPrintProgressUpdate);
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            if (null != _statusDisplay) _statusDisplay.text = _mainBoard.Status.ToString();
        }

        private void OnStateChange()
        {
            const string m = nameof(OnPrintProgressUpdate);
            if (null == _mainBoard)
            {
                LogErr(m, "null == _mainBoard");
                return;
            }
            if (null != _statusDisplay) _statusDisplay.text = _mainBoard.Status.ToString();

            if (null != _executeGCodeFileBtnLabel)
            {
                _executeGCodeFileBtnLabel.text =
                    _mainBoard.Status.State == PrinterMainBoard.PrinterMainBoardStatus.EPrinterState.Printing
                        ? "Pause"
                        : "Execute";
            }
        }
    }
}