using UnityEngine;

namespace CoradoLog.Web
{
    public sealed class CoLoggerWebInitiator : MonoBehaviour
    {
        [SerializeField] private CoLoggerWebSettings _settings;

        private CoLoggerWebModule _module;

        public CoLoggerWebModule Module => _module;

        private void Awake()
        {
            if (_settings == null) return;

            _module = new CoLoggerWebModule();
            _module.Init(_settings);
        }

        private void OnDestroy()
        {
            _module?.Discard();
            _module = null;
        }
    }
}
