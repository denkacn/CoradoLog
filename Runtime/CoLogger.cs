using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CoradoLog.Interfaces;
using UnityEngine;

namespace CoradoLog
{
    public static class CoLogger
    {
        private const string CONTEXT_SYSTEM = "System";
        private const string SENDER_SYSTEM = "System";
        private const string CustomContextSymbol = "!";
        
        private static string _senders;
        private static CoLoggerSettings _settings;
        private static ICoLoggerTransmitter _transmitter;
        private static CoLoggerFileWriter _writer;
        private static CoLoggerHtmlFileWriter _htmlWriter;
        private static ICoLoggerCustomDataProvider _customDataProvider;
        
        public static void Init(CoLoggerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            
            _settings = settings;

            new GameObject("CoLoggerLifeTimeCycleController")
                .AddComponent<CoLoggerLifeTimeCycle>();

            if (_settings.IsLogToFile)
            {
                EnableFileWriter(Application.dataPath + _settings.FileWriterPath);
            }

            if (_settings.IsLogToHtml)
            {
                EnableHtmlWriter(Application.dataPath + _settings.HtmlFileWriterPath, _settings.IsOnlyCoLoggerLogs);
            }
            
            Log("CoLogger Initialize", CONTEXT_SYSTEM);
        }

        public static void EnableFileWriter(string path, string prefix = "")
        {
            if (_writer != null) return;

            try
            {
                var exeDir = Path.GetDirectoryName(path);
                if (exeDir != null)
                {
                    var logFilePath = Path.Combine(exeDir,
                        $"cologger_{prefix}_{Guid.NewGuid().ToString().Replace("-", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                    _writer = new CoLoggerFileWriter();
                    _writer.Init(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to enable file writer: {ex.Message}");
            }
        }

        public static void EnableHtmlWriter(string path, bool isOnlyCoLoggerLogs, string suffix = "")   
        {
            if (_htmlWriter != null) return;

            try
            {
                var exeDir = Path.GetDirectoryName(path);
                var logFilePath = Path.Combine(exeDir,
                    $"cologger_html_{suffix}_{Guid.NewGuid().ToString().Replace("-", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.html");

                _htmlWriter = new CoLoggerHtmlFileWriter();
                _htmlWriter.Init(isOnlyCoLoggerLogs, logFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to enable HTML writer: {ex.Message}");
            }
        }

        public static void SetTransmitter(ICoLoggerTransmitter transmitter)
        {
            _transmitter = transmitter;
        }

        public static void SetDefaultParams(string senders)
        {
            _senders = senders;
        }

        public static void SetCustomDataProvider(ICoLoggerCustomDataProvider customDataProvider)
        {
            _customDataProvider = customDataProvider;
        }
        
        public static void Log(string message, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            Log(message, SENDER_SYSTEM, CONTEXT_SYSTEM, string.Empty, importance);
        }
        
        public static void LogError(string message, Exception ex, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            Log(message, SENDER_SYSTEM, CONTEXT_SYSTEM, string.Empty, importance, ex);
        }

        public static void Log(string message, string context, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            var sender = string.IsNullOrEmpty(_senders) ? SENDER_SYSTEM : _senders;
            Log(message, sender, context, string.Empty, importance);
        }
        
        public static void LogError(string message, string context, Exception ex, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            var sender = string.IsNullOrEmpty(_senders) ? SENDER_SYSTEM : _senders;
            Log(message, sender, context, string.Empty, importance, ex);
        }
        
        public static void Log(string message, string context, string tag, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            var sender = string.IsNullOrEmpty(_senders) ? SENDER_SYSTEM : _senders;
            Log(message, sender, context, tag, importance);
        }

        public static void Log(string message, string sender, string context, string tag, EDebugImportance importance = EDebugImportance.All, Exception ex = null, object customData = null)
        {
            if (!string.IsNullOrEmpty(tag) && !_settings.IsTagExist(tag)) return;

            try
            {
                var formatMessage = GetMessageFormat(message, importance);
                var formatContext = GetContextFormat(context);
                var formatTag = GetTagFormat(tag);
                var formatCustomData = customData != null && _customDataProvider != null
                    ? _customDataProvider.GetCustomDataFormat(customData)
                    : string.Empty;

                string correctLogString;

                if (ex == null)
                {
                    correctLogString = $"{DateTime.Now} [CL][{sender}] [{formatContext}] {formatTag}: {formatMessage}";
                }
                else
                {
                    correctLogString = $"{DateTime.Now} [CL][{sender}] [{formatContext}] {formatTag}: {formatMessage}\n{ex}";
                }

                if (formatCustomData != string.Empty)
                    correctLogString += $"\nCustomData: ({formatCustomData})";

                var isConditionValid = _settings.IsSenderExist(sender) && (int)importance >= (int)_settings.Importance && IsContextValid(context);

                if (isConditionValid)
                {
                    if (ex == null)
                        Debug.Log(correctLogString);
                    else
                        Debug.LogError(correctLogString);
                }

                _transmitter?.ResendMe(message, sender, context, importance);

                if (_settings.SkipConditionsForFileWriter || isConditionValid) _writer?.Write(correctLogString);

            }
            catch (Exception logEx)
            {
                Debug.LogError($"Error during logging: {logEx.Message}");
            }
        }

        public static void AddContext(string context)
        { 
            _settings.AddContext(context);
        }
        
        private static bool IsContextValid(string context)
        {
            if (_settings.IsContextExist(context)) return true;

            if (!_settings.IsAddContextInRuntime) return false;
            
            AddContext(context);
            return true;

        }
        
        private static string GetTagFormat(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return string.Empty;

            var tagColor = _settings.TagColor;
            var hexColor = ColorUtility.ToHtmlStringRGB(tagColor);
            
            return $"(<color=#{hexColor}>{tag}</color>)";

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
            
            var hexColor = ColorUtility.ToHtmlStringRGB(color);
            
            return $"<color=#{hexColor}>{customSymbol}{context}</color>";
        }

        private static string GetMessageFormat(string message, EDebugImportance importance)
        {
            var messageSanitized = SanitizeMessage(message);
            
            var importanceSetting = _settings.ImportanceSettings.FirstOrDefault(i => i.Importance == importance);
            var color = Color.white;
            
            if (importanceSetting != null)
            {
                color = importanceSetting.DrawColor;
            }
            
            var hexColor = ColorUtility.ToHtmlStringRGB(color);

            return $"<color=#{hexColor}>{messageSanitized}</color>";
        }
        
        public static string SanitizeMessage(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Zа-яА-Я0-9\s\.,!?;:()\-]", "");
        }

        public static void Discard()
        {
            Log("CoLogger Discard", CONTEXT_SYSTEM);

            try
            {
                _writer?.Discard();
                _htmlWriter?.Discard();
                
                _writer = null;
                _htmlWriter = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during CoLogger discard: {ex.Message}");
            }
        }
    }

    public interface ICoLoggerTransmitter
    {
        void ResendMe(string message, string sender, string context, EDebugImportance importance);
    }
}
