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
        private readonly Queue<CoLoggerEntry> _queue = new Queue<CoLoggerEntry>();
        private readonly Dictionary<string, DuplicateLogState> _duplicateStates = new Dictionary<string, DuplicateLogState>();
        private CoLoggerWebSettings _settings;
        private MonoBehaviour _coroutineRunner;
        private Coroutine _flushCoroutine;
        private bool _isSubscribed;
        private bool _isSending;
        private long _externalLogSequence;
        private long _unityLogSequence;
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
            _sessionId = CoLoggerWebHelper.CreateSessionId();
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
            _duplicateStates.Clear();
            _settings = null;
            _coroutineRunner = null;
            _isSending = false;
            _externalLogSequence = 0;
            _unityLogSequence = 0;
            _sessionId = null;
        }

        public void Dispose()
        {
            Discard();
        }

        public void QueueLog(
            string message,
            CoLoggerEntryLevel level = CoLoggerEntryLevel.Information,
            string sender = null,
            string context = null,
            string tag = null,
            EDebugImportance importance = EDebugImportance.All,
            Exception exception = null,
            object customData = null,
            string stackTrace = null)
        {
            if (_settings == null || !_settings.IsReady) return;

            var entry = new CoLoggerEntry(
                DateTime.Now,
                ++_externalLogSequence,
                level,
                message,
                CoLoggerWebHelper.GetValueOrDefault(sender, "External"),
                CoLoggerWebHelper.GetValueOrDefault(context, "External"),
                tag,
                importance,
                exception,
                customData,
                stackTrace);

            EnqueueEntry(entry);
        }

        public void QueueWarning(string message, string sender = null, string context = null, string tag = null, object customData = null, string stackTrace = null)
        {
            QueueLog(message, CoLoggerEntryLevel.Warning, sender, context, tag, EDebugImportance.Medium, null, customData, stackTrace);
        }

        public void QueueError(string message, string sender = null, string context = null, string tag = null, Exception exception = null, object customData = null, string stackTrace = null)
        {
            QueueLog(message, CoLoggerEntryLevel.Error, sender, context, tag, EDebugImportance.Critical, exception, customData, stackTrace);
        }

        public void QueueCritical(string message, string sender = null, string context = null, string tag = null, Exception exception = null, object customData = null, string stackTrace = null)
        {
            QueueLog(message, CoLoggerEntryLevel.Critical, sender, context, tag, EDebugImportance.Critical, exception, customData, stackTrace);
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
            Application.logMessageReceived -= HandleUnityLogReceived;

            if (_settings.SendCoLoggerLogs)
            {
                CoLogger.LogReceived += HandleLogReceived;
            }

            if (_settings.SendUnityLogs)
            {
                Application.logMessageReceived += HandleUnityLogReceived;
            }

            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;

            CoLogger.LogReceived -= HandleLogReceived;
            Application.logMessageReceived -= HandleUnityLogReceived;
            _isSubscribed = false;
        }

        private void HandleLogReceived(CoLoggerEntry entry)
        {
            if (_settings == null || !_settings.IsReady || !_settings.SendCoLoggerLogs || entry == null) return;

            EnqueueEntry(entry);
        }

        private void HandleUnityLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_settings == null || !_settings.IsReady || !_settings.SendUnityLogs) return;
            if (CoLoggerWebHelper.IsIgnoredUnityLog(condition)) return;

            var level = CoLoggerWebHelper.MapUnityLogType(type);
            if (level != CoLoggerEntryLevel.Warning && level != CoLoggerEntryLevel.Error && level != CoLoggerEntryLevel.Critical) return;

            var entry = new CoLoggerEntry(
                DateTime.Now,
                ++_unityLogSequence,
                level,
                condition,
                "Unity",
                "Unity",
                type.ToString(),
                level == CoLoggerEntryLevel.Warning ? EDebugImportance.Medium : EDebugImportance.Critical,
                null,
                null,
                stackTrace);

            EnqueueEntry(entry);
        }

        private void EnqueueEntry(CoLoggerEntry entry)
        {
            if (entry == null) return;
            if (IsDuplicateSuppressed(entry)) return;

            EnqueueEntryDirect(entry);
        }

        private void EnqueueEntryDirect(CoLoggerEntry entry)
        {
            if (entry == null) return;

            while (_queue.Count >= Math.Max(1, _settings.MaxQueueSize))
            {
                _queue.Dequeue();
            }

            _queue.Enqueue(entry);

            if (IsBatchReady || CoLoggerWebHelper.IsImmediateFlushLevel(entry.Level))
            {
                Flush();
            }
        }

        private bool IsDuplicateSuppressed(CoLoggerEntry entry)
        {
            if (_settings == null || !_settings.IsDuplicateProtectionEnabled) return false;

            var key = CoLoggerWebHelper.BuildDuplicateKey(entry);
            var now = Time.realtimeSinceStartup;
            var windowSeconds = Math.Max(0.1f, _settings.DuplicateWindowSeconds);
            DuplicateLogState state;

            if (!_duplicateStates.TryGetValue(key, out state))
            {
                _duplicateStates[key] = new DuplicateLogState(now, entry);
                return false;
            }

            if (now - state.WindowStartedAt > windowSeconds)
            {
                EnqueueDuplicateSummaryIfNeeded(state, windowSeconds);
                _duplicateStates[key] = new DuplicateLogState(now, entry);
                return false;
            }

            state.LastEntry = entry;

            if (state.SentCount < Math.Max(1, _settings.MaxSameLogsPerWindow))
            {
                state.SentCount++;
                return false;
            }

            state.SuppressedCount++;
            return true;
        }

        private void EnqueueExpiredDuplicateSummaries()
        {
            if (_settings == null || !_settings.IsDuplicateProtectionEnabled || !_settings.SendDuplicateSummary) return;

            var now = Time.realtimeSinceStartup;
            var windowSeconds = Math.Max(0.1f, _settings.DuplicateWindowSeconds);
            var expiredKeys = new List<string>();

            foreach (var pair in _duplicateStates)
            {
                if (now - pair.Value.WindowStartedAt > windowSeconds)
                {
                    EnqueueDuplicateSummaryIfNeeded(pair.Value, windowSeconds);
                    expiredKeys.Add(pair.Key);
                }
            }

            for (var i = 0; i < expiredKeys.Count; i++)
            {
                _duplicateStates.Remove(expiredKeys[i]);
            }
        }

        private void EnqueueDuplicateSummaryIfNeeded(DuplicateLogState state, float windowSeconds)
        {
            if (_settings == null || !_settings.SendDuplicateSummary || state == null || state.SuppressedCount <= 0 || state.LastEntry == null) return;

            var summary = CoLoggerWebHelper.CreateDuplicateSummaryEntry(
                state,
                windowSeconds,
                ++_externalLogSequence);

            EnqueueEntryDirect(summary);
        }

        private IEnumerator FlushLoop()
        {
            while (_settings != null)
            {
                yield return new WaitForSeconds(Math.Max(0.1f, _settings.FlushIntervalSeconds));
                EnqueueExpiredDuplicateSummaries();
                Flush();
            }
        }

        private IEnumerator SendNextBatch()
        {
            if (_settings == null || _isSending || _queue.Count == 0) yield break;

            _isSending = true;
            var batch = DequeueBatch(GetBatchSize());
            var requestJson = CoLoggerWebHelper.BuildRequestJson(_settings, _sessionId, batch);
            var requestUrl = CoLoggerWebHelper.BuildRequestUrl(_settings);
            var body = Encoding.UTF8.GetBytes(requestJson);
            var isSendFailed = false;

            using (var request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Corado-Token", _settings.ApiToken.Trim());

                yield return request.SendWebRequest();

                if (CoLoggerWebHelper.IsRequestFailed(request))
                {
                    isSendFailed = true;
                    CoLoggerWebHelper.RequeueFront(_queue, batch, _settings.MaxQueueSize);
                    Debug.LogWarning($"CoLogger web log send failed: {request.responseCode} {request.error}");
                }
            }

            _isSending = false;

            if (!isSendFailed && _settings != null && _queue.Count >= GetBatchSize())
            {
                Flush();
            }
        }

        private int GetBatchSize()
        {
            return CoLoggerWebHelper.GetBatchSize(_settings);
        }
    }
}
