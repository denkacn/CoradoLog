using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CoradoLog.Web
{
    public sealed class CoLoggerWebModule : IDisposable
    {
        private const int BackendMaxBatchSize = 1000;
        private readonly Queue<CoLoggerEntry> _queue = new Queue<CoLoggerEntry>();
        private CoLoggerWebSettings _settings;
        private MonoBehaviour _coroutineRunner;
        private Coroutine _flushCoroutine;
        private bool _isSubscribed;
        private bool _isSending;
        private string _sessionId;

        public bool IsInitialized => _settings != null;
        public int QueuedCount => _queue.Count;
        public CoLoggerWebSettings Settings => _settings;
        public bool IsBatchReady => _settings != null && _queue.Count >= GetBatchSize();

        public void Init(CoLoggerWebSettings settings, MonoBehaviour coroutineRunner)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (coroutineRunner == null)
                throw new ArgumentNullException(nameof(coroutineRunner));

            if (IsInitialized)
            {
                Debug.LogWarning("CoLoggerWebModule is already initialized. Call Discard() before initializing it again.");
                return;
            }

            _settings = settings;
            _coroutineRunner = coroutineRunner;
            _sessionId = CreateSessionId();
            Subscribe();
            _flushCoroutine = _coroutineRunner.StartCoroutine(FlushLoop());
        }

        public void Discard()
        {
            Unsubscribe();

            if (_flushCoroutine != null && _coroutineRunner != null)
            {
                _coroutineRunner.StopCoroutine(_flushCoroutine);
            }

            _flushCoroutine = null;
            _queue.Clear();
            _settings = null;
            _coroutineRunner = null;
            _isSending = false;
            _sessionId = null;
        }

        public void Dispose()
        {
            Discard();
        }

        public List<CoLoggerEntry> DequeueBatch(int maxCount)
        {
            var count = Math.Min(Math.Max(0, maxCount), _queue.Count);
            var batch = new List<CoLoggerEntry>(count);

            for (var i = 0; i < count; i++)
            {
                batch.Add(_queue.Dequeue());
            }

            return batch;
        }

        public void Flush()
        {
            if (_coroutineRunner == null || _settings == null || _isSending || _queue.Count == 0) return;

            _coroutineRunner.StartCoroutine(SendNextBatch());
        }

        private void Subscribe()
        {
            if (_isSubscribed) return;

            CoLogger.LogReceived -= HandleLogReceived;
            CoLogger.LogReceived += HandleLogReceived;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;

            CoLogger.LogReceived -= HandleLogReceived;
            _isSubscribed = false;
        }

        private void HandleLogReceived(CoLoggerEntry entry)
        {
            if (_settings == null || !_settings.IsReady || entry == null) return;

            while (_queue.Count >= Math.Max(1, _settings.MaxQueueSize))
            {
                _queue.Dequeue();
            }

            _queue.Enqueue(entry);

            if (IsBatchReady || entry.Level == CoLoggerEntryLevel.Error || entry.Level == CoLoggerEntryLevel.Critical)
            {
                Flush();
            }
        }

        private IEnumerator FlushLoop()
        {
            while (_settings != null)
            {
                yield return new WaitForSeconds(Math.Max(0.1f, _settings.FlushIntervalSeconds));
                Flush();
            }
        }

        private IEnumerator SendNextBatch()
        {
            if (_settings == null || _isSending || _queue.Count == 0) yield break;

            _isSending = true;
            var batch = DequeueBatch(GetBatchSize());
            var requestJson = BuildRequestJson(batch);
            var requestUrl = BuildRequestUrl();
            var body = Encoding.UTF8.GetBytes(requestJson);

            var isSendFailed = false;

            using (var request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Corado-Token", _settings.ApiToken.Trim());

                yield return request.SendWebRequest();

                if (IsRequestFailed(request))
                {
                    isSendFailed = true;
                    RequeueFront(batch);
                    Debug.LogWarning($"CoLogger web log send failed: {request.responseCode} {request.error}");
                }
            }

            _isSending = false;

            if (!isSendFailed && _settings != null && _queue.Count >= GetBatchSize())
            {
                Flush();
            }
        }

        private string BuildRequestUrl()
        {
            return $"{_settings.BaseUrl.Trim().TrimEnd('/')}/api/v1/logs/batch";
        }

        private string BuildRequestJson(List<CoLoggerEntry> entries)
        {
            var request = new CoLoggerWebBatchRequest
            {
                sessionId = _sessionId,
                source = TrimToLength(GetSource(), 80),
                appVersion = TrimToLength(GetValueOrDefault(_settings.AppVersion, Application.version), 80),
                buildNumber = TrimToLength(_settings.BuildNumber, 80),
                platform = TrimToLength(Application.platform.ToString(), 80),
                deviceId = TrimToLength(GetDeviceId(), 200),
                externalUserId = TrimToLength(_settings.ExternalUserId, 200),
                logs = new List<CoLoggerWebLogItem>(entries.Count)
            };

            for (var i = 0; i < entries.Count; i++)
            {
                request.logs.Add(MapEntry(entries[i]));
            }

            return JsonUtility.ToJson(request);
        }

        private CoLoggerWebLogItem MapEntry(CoLoggerEntry entry)
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
                stackTrace = entry.Exception?.StackTrace,
                customData = entry.CustomData?.ToString()
            };
        }

        private int GetBatchSize()
        {
            if (_settings == null) return 1;

            return Mathf.Clamp(_settings.BatchSize, 1, BackendMaxBatchSize);
        }

        private string GetSource()
        {
            return GetValueOrDefault(_settings.Source, "Unity");
        }

        private string GetDeviceId()
        {
            return _settings.UseDeviceUniqueIdentifier ? SystemInfo.deviceUniqueIdentifier : null;
        }

        private string CreateSessionId()
        {
            return $"unity-{Guid.NewGuid():N}";
        }

        private void RequeueFront(List<CoLoggerEntry> batch)
        {
            if (batch == null || batch.Count == 0) return;

            var remaining = new List<CoLoggerEntry>(_queue);
            _queue.Clear();

            for (var i = 0; i < batch.Count; i++)
            {
                _queue.Enqueue(batch[i]);
            }

            for (var i = 0; i < remaining.Count; i++)
            {
                _queue.Enqueue(remaining[i]);
            }

            while (_settings != null && _queue.Count > Math.Max(1, _settings.MaxQueueSize))
            {
                _queue.Dequeue();
            }
        }

        private static bool IsRequestFailed(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
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

        private static string GetValueOrDefault(string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static string TrimToLength(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        [Serializable]
        private sealed class CoLoggerWebBatchRequest
        {
            public string sessionId;
            public string source;
            public string appVersion;
            public string buildNumber;
            public string platform;
            public string deviceId;
            public string externalUserId;
            public List<CoLoggerWebLogItem> logs;
        }

        [Serializable]
        private sealed class CoLoggerWebLogItem
        {
            public string timestamp;
            public long sequence;
            public string level;
            public string importance;
            public string sender;
            public string context;
            public string tag;
            public string message;
            public string exceptionType;
            public string exceptionMessage;
            public string stackTrace;
            public string customData;
        }
    }
}


