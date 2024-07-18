using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerInitiator : MonoBehaviour
    {
        public string GeneratePath => _generatePath;
        
        [SerializeField] private CoLoggerSettings _settings;
        [SerializeField] private string _deffSender;
        [SerializeField] private string _generatePath;
        
        void Awake()
        {
            CoLogger.Init(_settings);

            if (!string.IsNullOrEmpty(_deffSender))
            {
                CoLogger.SetDefaultParams(_deffSender);
            }

            Destroy(gameObject);
        }

        public List<string> GetSenderNames()
        {
            return _settings != null
                ? _settings.SendeSettings.Select(sender => sender.SenderName).ToList()
                : new List<string>();
        }

        public List<string> GetSeContextNames()
        {
            return _settings != null
                ? _settings.ContextSettings.Select(context => context.ContextName).ToList()
                : new List<string>();
        }
    }
}
