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
        [SerializeField] private MainBoardComp _mainBoardComp;
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

            if (null == _mainBoardComp)
            {
                LogErr(m, "_mainBoardComp is null");
                return;
            }
            _mainBoardComp.OnPrintProgressUpdate += OnPrintProgressUpdate;
            _mainBoardComp.OnStateChange += OnStateChange;
            if (null == _gCodeView) LogErr(m, "_gCodeView is null");
            else _gCodeView.SetPrinter(_mainBoardComp);
            
            if (null != _loadGCodeFileBtn) _loadGCodeFileBtn.onClick.AddListener(() =>
            {
                if (null != _mainBoardComp) _mainBoardComp.LoadGCodeFile(TestGCodeFilePath);
            });
            else LogErr(m, "_loadGCodeFileBtn is null");
            if (null != _executeGCodeFileBtn) {
                _executeGCodeFileBtn.onClick.AddListener(() =>
                {
                    if (null == _mainBoardComp) return;
                    switch (_mainBoardComp.Status.CommandState)
                    {
                        case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Printing:
                            _mainBoardComp.Pause();
                            break;
                        case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Pause:
                            _mainBoardComp.Continue();
                            break;
                        case PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Idle:
                            _mainBoardComp.Execute();
                            break;
                    }
                });
                _executeGCodeFileBtnLabel =
                    _executeGCodeFileBtn.gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            else LogErr(m, "_executeGCodeFileBtn is null");
            if (null != _stopBtn) _stopBtn.onClick.AddListener(() =>
            {
                if (null != _mainBoardComp) _mainBoardComp.Stop();
            });
            else LogErr(m, "_stopBtn is null");

            if (null != _statusDisplay) _statusDisplay.text = string.Empty;
            else LogErr(m, "_statusDisplay is null");

            OnStateChange();
            OnPrintProgressUpdate();
        }

        protected override void Clear()
        {
            base.Clear();
            if (null != _mainBoardComp)
            {
                _mainBoardComp.OnPrintProgressUpdate -= OnPrintProgressUpdate;
                _mainBoardComp.OnStateChange -= OnStateChange;
            }
        }

        private void OnPrintProgressUpdate()
        {
            const string m = nameof(OnPrintProgressUpdate);
            // LogMethod(m);
            if (null == _mainBoardComp)
            {
                LogErr(m, "null == _mainBoardComp");
                return;
            }
            if (null != _statusDisplay) _statusDisplay.text = _mainBoardComp.Status.ToString();
        }

        private void OnStateChange()
        {
            const string m = nameof(OnStateChange);
            // LogMethod(m);
            if (null == _mainBoardComp)
            {
                LogErr(m, "null == _mainBoardComp");
                return;
            }
            if (null != _statusDisplay) _statusDisplay.text = _mainBoardComp.Status.ToString();

            if (null != _executeGCodeFileBtnLabel)
            {
                _executeGCodeFileBtnLabel.text =
                    _mainBoardComp.Status.CommandState == PrinterMainBoard.PrinterMainBoardStatus.ECommandState.Printing
                        ? "Pause"
                        : "Execute";
            }
        }
    }
}