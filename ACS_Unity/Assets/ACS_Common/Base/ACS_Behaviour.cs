using UnityEngine;

namespace ACS_Common.Base
{
    /// <summary>
    /// behaviour基类
    /// </summary>
    public abstract class ACS_Behaviour : MonoBehaviour
    {
        private void Awake()
        {
            Init();
        }

        protected virtual void Init() {}
    }
}