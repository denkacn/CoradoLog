﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerInitiator : MonoBehaviour
    {
        [SerializeField] private CoLoggerSettings _settings;
        [SerializeField] private string _deffSender;

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
            return _settings.SendeSettings.Select(sender => sender.SenderName).ToList();
        }

        public List<string> GetSeContextNames()
        {
            return _settings.ContextSettings.Select(context => context.ContextName).ToList();
        }
    }
}
