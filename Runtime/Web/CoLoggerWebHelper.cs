using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace CoradoLog.Web
{
    internal static class CoLoggerWebHelper
    {
        private const int BackendMaxBatchSize = 1000;

        public static string BuildRequestUrl(CoLoggerWebSettings settings)
        {
            return $"{settings.BaseUrl.Trim().TrimEnd('/')}/api/v1/logs/batch";
        }

        public static string BuildRequestJson(CoLoggerWebSettings settings, string sessionId, List<CoLoggerEntry> entries)
        {
            var request = new CoLoggerWebBatchRequest
            {
                sessionId = sessionId,
                source = TrimToLength(GetSource(settings), 80),
                appVersion = TrimToLength(GetValueOrDefault(settings.AppVersion, Application.version), 80),
                buildNumber = TrimToLength(settings.BuildNumber, 80),
                platform = TrimToLength(Application.platform.ToString(), 80),
                deviceId = TrimToLength(GetDeviceId(settings), 200),
                externalUserId = TrimToLength(settings.ExternalUserId, 200),
                logs = new List<CoLoggerWebLogItem>(entries.Count)
            };

            for (var i = 0; i < entries.Count; i++)
            {
                request.logs.Add(MapEntry(entries[i]));
            }

            return JsonUtility.ToJson(request);
        }

        public static int GetBatchSize(CoLoggerWebSettings settings)
        {
            if (settings == null) return 1;

            return Mathf.Clamp(settings.BatchSize, 1, BackendMaxBatchSize);
        }

        public static string CreateSessionId()
        {
            return $"unity-{Guid.NewGuid():N}";
        }

        public static void RequeueFront(Queue<CoLoggerEntry> queue, List<CoLoggerEntry> batch, int maxQueueSize)
        {
            if (queue == null || batch == null || batch.Count == 0) return;

            var remaining = new List<CoLoggerEntry>(queue);
            queue.Clear();

            for (var i = 0; i < batch.Count; i++)
            {
                queue.Enqueue(batch[i]);
            }

            for (var i = 0; i < remaining.Count; i++)
            {
                queue.Enqueue(remaining[i]);
            }

            while (queue.Count > Math.Max(1, maxQueueSize))
            {
                queue.Dequeue();
            }
        }

        public static bool IsIgnoredUnityLog(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return false;
            if (condition.Contains("[CL]")) return true;
            if (condition.StartsWith("CoLogger", StringComparison.Ordinal)) return true;
            if (condition.StartsWith("Error during CoLogger", StringComparison.Ordinal)) return true;

            return false;
        }

        public static CoLoggerEntryLevel MapUnityLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return CoLoggerEntryLevel.Warning;
                case LogType.Assert:
                case LogType.Error:
                    return CoLoggerEntryLevel.Error;
                case LogType.Exception:
                    return CoLoggerEntryLevel.Critical;
                default:
                    return CoLoggerEntryLevel.Information;
            }
        }

        public static bool IsUnityLogTypeEnabled(LogType type, CoLoggerWebSettings settings)
        {
            if (settings == null) return false;
            if (type == LogType.Log) return settings.SendUnityInfoLogs;
            if (type == LogType.Warning) return settings.SendUnityWarningLogs;

            return type == LogType.Assert ||
                   type == LogType.Error ||
                   type == LogType.Exception;
        }

        public static EDebugImportance MapUnityImportance(CoLoggerEntryLevel level)
        {
            switch (level)
            {
                case CoLoggerEntryLevel.Warning:
                    return EDebugImportance.Medium;
                case CoLoggerEntryLevel.Error:
                case CoLoggerEntryLevel.Critical:
                    return EDebugImportance.Critical;
                default:
                    return EDebugImportance.All;
            }
        }

        public static bool IsImmediateFlushLevel(CoLoggerEntryLevel level)
        {
            return level == CoLoggerEntryLevel.Warning ||
                   level == CoLoggerEntryLevel.Error ||
                   level == CoLoggerEntryLevel.Critical;
        }

        public static bool IsRequestFailed(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        public static string BuildDuplicateKey(CoLoggerEntry entry)
        {
            return string.Join("|",
                MapLevel(entry.Level),
                entry.Sender ?? string.Empty,
                entry.Context ?? string.Empty,
                entry.Tag ?? string.Empty,
                entry.Message ?? string.Empty,
                entry.Exception != null ? entry.Exception.GetType().FullName : string.Empty,
                entry.Exception != null ? entry.Exception.Message : string.Empty,
                entry.CallStack ?? string.Empty);
        }

        public static CoLoggerEntry CreateDuplicateSummaryEntry(DuplicateLogState state, float windowSeconds, long sequence)
        {
            var original = state.LastEntry;
            var message = $"Suppressed {state.SuppressedCount} duplicate logs during {windowSeconds:0.##} seconds: {original.Message}";

            return new CoLoggerEntry(
                DateTime.Now,
                sequence,
                original.Level,
                message,
                original.Sender,
                original.Context,
                original.Tag,
                original.Importance,
                original.Exception,
                original.CustomData,
                original.CallStack);
        }

        public static string GetValueOrDefault(string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static CoLoggerWebLogItem MapEntry(CoLoggerEntry entry)
        {
            return new CoLoggerWebLogItem
            {
                timestamp = entry.Timestamp.ToUniversalTime().ToString("O"),
                sequence = entry.Sequence,
                level = MapLevel(entry.Level),
                importance = entry.Importance.ToString(),
                sender = TrimToLength(entry.Sender, 160),
                context = TrimToLength(entry.Context, 160),
                tag = TrimToLength(entry.Tag, 160),
                message = entry.Message,
                exceptionType = entry.Exception?.GetType().FullName,
                exceptionMessage = entry.Exception?.Message,
                stackTrace = BuildStackTrace(entry),
                customData = entry.CustomData?.ToString()
            };
        }

        private static string GetSource(CoLoggerWebSettings settings)
        {
            return GetValueOrDefault(settings.Source, "Unity");
        }

        private static string GetDeviceId(CoLoggerWebSettings settings)
        {
            return settings.UseDeviceUniqueIdentifier ? SystemInfo.deviceUniqueIdentifier : null;
        }

        private static string BuildStackTrace(CoLoggerEntry entry)
        {
            if (entry == null) return null;

            var exceptionStackTrace = entry.Exception?.StackTrace;
            var callStack = entry.CallStack;

            if (string.IsNullOrWhiteSpace(exceptionStackTrace))
            {
                return string.IsNullOrWhiteSpace(callStack) ? null : callStack;
            }

            if (string.IsNullOrWhiteSpace(callStack))
            {
                return exceptionStackTrace;
            }

            return $"Exception stack trace:\n{exceptionStackTrace}\n\nCoLogger call stack:\n{callStack}";
        }

        private static string MapLevel(CoLoggerEntryLevel level)
        {
            switch (level)
            {
                case CoLoggerEntryLevel.Trace:
                    return "Trace";
                case CoLoggerEntryLevel.Debug:
                    return "Debug";
                case CoLoggerEntryLevel.Warning:
                    return "Warning";
                case CoLoggerEntryLevel.Error:
                    return "Error";
                case CoLoggerEntryLevel.Critical:
                    return "Critical";
                default:
                    return "Information";
            }
        }

        private static string TrimToLength(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }
    }
}




