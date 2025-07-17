using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerLifeTimeCycle : MonoBehaviour
    {
        private void OnDestroy()
        {
            CoLogger.Discard();
        }
    }
}