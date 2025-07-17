using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CoradoLog
{
    public static class CoLogger
    {
        private const string CONTEXT_SYSTEM = "System";
        private const string SENDER_SYSTEM = "System";
        
        private const string CustomContextSymbol = "!";
        
        private static CoLoggerSettings _settings;
        private static string _senders;
        private static ICoLoggerTransmitter _transmitter;
        private static CoLoggerFileWriter _writer;
        
        public static void Init(CoLoggerSettings settings)
        {
            _settings = settings;
            
            new GameObject("CoLoggerLifeTimeCycleController").AddComponent<CoLoggerLifeTimeCycle>();

            if (_settings.IsLogToFile)
            {
                EnableFileWriter(Application.dataPath + _settings.FileWriterPath);
            }
            
            Log("CoLogger Initialize", CONTEXT_SYSTEM);
        }
        
        public static void EnableFileWriter(string path)
        {
            if (_writer != null) return;
            
            var exeDir = Path.GetDirectoryName(path);
            var logFilePath = Path.Combine(exeDir,
                "cologger_" + Guid.NewGuid().ToString().Replace("-", "_") + "_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                
            _writer = new CoLoggerFileWriter();
            _writer.Init(logFilePath);
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
            var sender = SENDER_SYSTEM;
            
            if (!string.IsNullOrEmpty(_senders))
                sender = _senders;

            Log(message, sender, context, string.Empty, importance);
        }
        
        public static void Log(string message, string context, string tag, EDebugImportance importance = EDebugImportance.All)
        {
            var sender = SENDER_SYSTEM;
            
            if (!string.IsNullOrEmpty(_senders))
                sender = _senders;

            Log(message, sender, context, tag, importance);
        }

        public static void Log(string message, string sender, string context, string tag, EDebugImportance importance = EDebugImportance.All, Exception ex = null)
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

            if (!string.IsNullOrEmpty(tag) && !_settings.IsTagExist(tag)) return;

            var formatMessage = GetMessageFormat(message, importance);
            var formatContext = GetContextFormat(context);
            var formatTag = GetTagFormat(tag);

            string correctLogString;

            if (ex == null)
            {
                correctLogString = string.Format("{3} [{1}] [{2}] {4}: {0}", formatMessage, sender, formatContext, DateTime.Now, formatTag);
            }
            else
            {
                correctLogString = string.Format("{3} [{1}] [{2}] {5}: {0}\n{4}", formatMessage, sender, formatContext, DateTime.Now, ex, formatTag);
            }

            Debug.Log(correctLogString);
            
            _transmitter?.ResendMe(message, sender, context, importance);

            _writer?.Write(correctLogString);
        }

        public static void AddContext(string context)
        { 
            _settings.AddContext(context);
        }
        
        private static object GetTagFormat(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return string.Empty;

            var tagColor = _settings.TagColor;
            
            return string.Format("(<color=#{0:X2}{1:X2}{2:X2}>{3}</color>)", (byte)(tagColor.r * 255f), (byte)(tagColor.g * 255f),
                (byte)(tagColor.b * 255f), tag);
        }

        private static string GetContextFormat(string context)
        {
            var contextSetting = _settings.ContextSettings.FirstOrDefault(c => c.ContextName == context);
            var color = Color.white;
            var customSymbol = string.Empty;
            
            if (contextSetting != null)
            {
                color = contextSetting.DrawColor;
                customSymbol = contextSetting.IsRuntime ? CustomContextSymbol : string.Empty;
            }
            
            return string.Format("<color=#{0:X2}{1:X2}{2:X2}>{4}{3}</color>", (byte)(color.r * 255f), (byte)(color.g * 255f),
                (byte)(color.b * 255f), context, customSymbol);
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

        public static void Discard()
        {
            Log("CoLogger Discard", CONTEXT_SYSTEM);
            
            _writer?.Discard();
        }
    }

    public interface ICoLoggerTransmitter
    {
        void ResendMe(string message, string sender, string context, EDebugImportance importance);
    }
}
