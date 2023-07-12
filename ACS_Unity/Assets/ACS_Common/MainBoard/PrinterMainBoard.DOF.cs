using System.Collections.Generic;

namespace ACS_Common.MainBoard
{
    /// <summary>
    /// 3d打印主板 运动自由度控制逻辑
    /// </summary>
    public partial class PrinterMainBoard
    {
        /// <summary>
        /// Dimension of freedom
        /// </summary>
        private class DOF
        {
            private string _name;

            public DOF(string name)
            {
                _name = name;
            }
        }

        private readonly Dictionary<string, DOF> _dof = new Dictionary<string, DOF>();
    }
}