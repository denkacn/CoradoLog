using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerLifeTimeCycle : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDestroy()
        {
            CoLogger.Discard();
        }
    }
}