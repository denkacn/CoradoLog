using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerLifeTimeCycle : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnApplicationQuit()
        {
            CoLogger.FlushWebFromLifeTimeCycle(this);
        }
        
        private void OnDestroy()
        {
            CoLogger.DiscardFromLifeTimeCycle(this);
        }
    }
}

