using System;
using System.IO;
using System.Linq;
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
        private static CoLoggerLifeTimeCycle _lifeTimeCycle;
        private static bool _isInitialized;
        private static bool _isDiscarding;
        private static bool _isMissingInitializationWarningLogged;

        public static bool IsInitialized => _isInitialized;
        
        public static void Init(CoLoggerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_isInitialized)
            {
                Debug.LogWarning("CoLogger is already initialized. Call CoLogger.Discard() before initializing it again.");
                return;
            }
            
            _settings = settings;
            _isInitialized = true;
            _isMissingInitializationWarningLogged = false;

            EnsureLifeTimeCycle();

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
            Log(message, SENDER_SYSTEM, CONTEXT_SYSTEM, string.Empty, importance, customData: customData);
        }
        
        public static void LogError(string message, Exception ex, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            Log(message, SENDER_SYSTEM, CONTEXT_SYSTEM, string.Empty, importance, ex, customData);
        }

        public static void Log(string message, string context, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            var sender = string.IsNullOrEmpty(_senders) ? SENDER_SYSTEM : _senders;
            Log(message, sender, context, string.Empty, importance, customData: customData);
        }
        
        public static void LogError(string message, string context, Exception ex, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            var sender = string.IsNullOrEmpty(_senders) ? SENDER_SYSTEM : _senders;
            Log(message, sender, context, string.Empty, importance, ex, customData);
        }
        
        public static void Log(string message, string context, string tag, EDebugImportance importance = EDebugImportance.All, object customData = null)
        {
            var sender = string.IsNullOrEmpty(_senders) ? SENDER_SYSTEM : _senders;
            Log(message, sender, context, tag, importance, customData: customData);
        }

        public static void Log(string message, string sender, string context, string tag, EDebugImportance importance = EDebugImportance.All, Exception ex = null, object customData = null)
        {
            if (!IsEnsureInitialized(message, sender, context, tag, ex)) return;

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
            if (!IsEnsureInitialized($"Cannot add context '{context}' before CoLogger initialization.", SENDER_SYSTEM, CONTEXT_SYSTEM, string.Empty, null)) return;
            
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
            var importanceSetting = _settings.ImportanceSettings.FirstOrDefault(i => i.Importance == importance);
            var color = Color.white;
            
            if (importanceSetting != null)
            {
                color = importanceSetting.DrawColor;
            }
            
            var hexColor = ColorUtility.ToHtmlStringRGB(color);

            return $"<color=#{hexColor}>{message ?? string.Empty}</color>";
        }
        
        public static string SanitizeMessage(string input)
        {
            return input ?? string.Empty;
        }

        public static void Discard()
        {
            DiscardInternal(true);
        }

        internal static void DiscardFromLifeTimeCycle(CoLoggerLifeTimeCycle lifeTimeCycle)
        {
            if (_lifeTimeCycle == lifeTimeCycle)
            {
                _lifeTimeCycle = null;
            }

            DiscardInternal(false);
        }

        private static void EnsureLifeTimeCycle()
        {
            if (_lifeTimeCycle != null) return;

            var lifeTimeCycleObject = new GameObject("CoLoggerLifeTimeCycleController");
            _lifeTimeCycle = lifeTimeCycleObject.AddComponent<CoLoggerLifeTimeCycle>();
        }

        private static bool IsEnsureInitialized(string message, string sender, string context, string tag, Exception ex)
        {
            if (_isInitialized && _settings != null) return true;

            if (!_isMissingInitializationWarningLogged)
            {
                Debug.LogWarning("CoLogger is not initialized. Use CoLogger.Init(settings) before writing CoLogger logs.");
                _isMissingInitializationWarningLogged = true;
            }

            WriteFallbackLog(message, sender, context, tag, ex);
            return false;
        }

        private static void WriteFallbackLog(string message, string sender, string context, string tag, Exception ex)
        {
            var formatTag = string.IsNullOrEmpty(tag) ? string.Empty : $"({tag})";
            var correctLogString = $"{DateTime.Now} [CL][{sender}] [{context}] {formatTag}: {message}";

            if (ex != null)
            {
                correctLogString += $"\n{ex}";
                Debug.LogError(correctLogString);
                return;
            }

            Debug.Log(correctLogString);
        }

        private static void DiscardInternal(bool destroyLifeTimeCycle)
        {
            if (_isDiscarding) return;
            
            _isDiscarding = true;
            
            if (_isInitialized && _settings != null)
            {
                Log("CoLogger Discard", CONTEXT_SYSTEM);
            }

            try
            {
                _writer?.Discard();
                _htmlWriter?.Discard();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during CoLogger discard: {ex.Message}");
            }
            finally
            {
                _writer = null;
                _htmlWriter = null;
                _settings = null;
                _senders = null;
                _transmitter = null;
                _customDataProvider = null;
                _isInitialized = false;
                _isMissingInitializationWarningLogged = false;

                if (destroyLifeTimeCycle && _lifeTimeCycle != null)
                {
                    UnityEngine.Object.Destroy(_lifeTimeCycle.gameObject);
                }

                _lifeTimeCycle = null;
                _isDiscarding = false;
            }
        }
    }

    public interface ICoLoggerTransmitter
    {
        void ResendMe(string message, string sender, string context, EDebugImportance importance);
    }
}

