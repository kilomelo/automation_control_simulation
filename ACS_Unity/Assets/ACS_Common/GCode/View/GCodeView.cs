using UnityEngine;

namespace ACS_Common.GCode.View
{
    /// <summary>
    /// unity的gcode vide实现
    /// </summary>
    public class GCodeView : MonoBehaviour
    {
        [SerializeField]
        private float _width = 200f;
        [SerializeField]
        private float _height = 200f;
        [SerializeField]
        private float _scrollBarWidth = 100f;
        /// <summary>
        /// 展示行数
        /// </summary>
        [SerializeField]
        private int _displayLineCnt = 10;

    }
}