using UnityEngine;

namespace ACS_Common.Base
{
    public class ACS_Object<T> where T : ACS_Object<T>
    {
        // protected virtual string Tag => GetType().Name;
        protected static void LogMethod(string methodName, string info = null)
        {
            Debug.Log($"# {typeof(T).Name} # <{methodName}> {info} //--------------------------------------------------------------------------");
        }
        
        protected static void LogInfo(string methodName, string info)
        {
            Debug.Log($"# {typeof(T).Name} # <{methodName}> {info}");
        }

        protected static  void LogErr(string methodName, string info)
        {
            Debug.LogError($"# {typeof(T).Name} # <{methodName}> {info}");
        }
        
        protected static  void LogWarn(string methodName, string info)
        {
            Debug.LogWarning($"# {typeof(T).Name} # <{methodName}> {info}");
        }
    }
}