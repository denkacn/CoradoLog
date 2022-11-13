using System;
using System.Linq;
using UnityEngine;

namespace CoradoLog
{
    public static class CoLogger
    {
        public const string CONTEXT_SYSTEM = "System";
        public const string CONTEXT_DEBUG = "Debug";

        private static CoLoggerSettings _settings;
        private static string _senders;
        private static ICoLoggerTransmitter _transmitter;

        internal static void Init(CoLoggerSettings settings)
        {
            _settings = settings;
            Log("DebugIt Initialize", "System", CONTEXT_SYSTEM, EDebugImportance.All);
        }

        public static void SetTransmitter(ICoLoggerTransmitter transmitter)
        {
            _transmitter = transmitter;
        }

        public static void SetDefaultParams(string senders)
        {
            _senders = senders;
        }

        public static void Log(string message, string context, EDebugImportance importance = EDebugImportance.All)
        {
            var sender = "System";
            if (!string.IsNullOrEmpty(_senders))
                sender = _senders;

            Log(message, sender, context, importance);
        }

        public static void Log(string message, string sender, string context, EDebugImportance importance = EDebugImportance.All, Exception ex = null)
        {
            if (!_settings.IsSenderExist(sender)) return;
            if ((int) importance < (int) _settings.Importance) return;
            
            if (!_settings.IsContextExist(context))
            {
                if (_settings.IsAddContextInRuntime)
                {
                    AddContext(context);
                }
                else
                {
                    return;
                }
            }

            var formatMessage = GetMessageFormat(message, importance);
            var formatContext = GetContextFormat(context);

            if (ex == null)
            {
                Debug.Log(string.Format("{3} [{1}] [{2}]: {0}", formatMessage, sender, formatContext, DateTime.Now));
            }
            else
            {
                Debug.LogError(string.Format("{3} [{1}] [{2}]: {0}\n{4}", formatMessage, sender, formatContext,
                    DateTime.Now, ex.ToString()));
            }

            if (_transmitter != null)
                _transmitter.ResendMe(message, sender, context, importance);
        }

        internal static void AddContext(string context)
        { 
            _settings.AddContext(context);
        }

        private static string GetContextFormat(string context)
        {
            var contextSetting = _settings.ContextSettings.FirstOrDefault(c => c.ContextName == context);
            var color = Color.white;
            
            if (contextSetting != null)
            {
                color = contextSetting.DrawColor;
            }

            return string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(color.r * 255f), (byte)(color.g * 255f),
                (byte)(color.b * 255f), context);
        }

        private static string GetMessageFormat(string message, EDebugImportance importance)
        {
            var importanceSetting = _settings.ImportanceSettings.FirstOrDefault(i => i.Importance == importance);
            var color = Color.white;
            
            if (importanceSetting != null)
            {
                color = importanceSetting.DrawColor;
            }

            return string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte) (color.r*255f), (byte) (color.g*255f),
                (byte) (color.b*255f), message);
        }
    }

    public interface ICoLoggerTransmitter
    {
        void ResendMe(string message, string sender, string context, EDebugImportance importance);
    }
}
