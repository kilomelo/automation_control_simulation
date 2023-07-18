using UnityEngine;

namespace ACS_Common.Base
{
    public class ACS_Object
    {
        protected string Tag => GetType().Name;
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
        
        protected static void LogInfoStatic(string tag, string methodName, string info = null)
        {
            Debug.Log($"# {tag} # <{methodName}> {info} //========================================================================");
        }
        protected static void LogErrStatic(string tag, string methodName, string info)
        {
            Debug.LogError($"# {tag} # <{methodName}> {info} //========================================================================");
        }
        
        protected void LogWarnStatic(string tag, string methodName, string info)
        {
            Debug.LogWarning($"# {tag} # <{methodName}> {info} //========================================================================");
        }

        public virtual void Init() {}
        public virtual void Clear() {}
    }
}