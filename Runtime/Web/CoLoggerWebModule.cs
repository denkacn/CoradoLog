using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoradoLog.Web
{
    public sealed class CoLoggerWebModule : IDisposable
    {
        private readonly Queue<CoLoggerEntry> _queue = new Queue<CoLoggerEntry>();
        private CoLoggerWebSettings _settings;
        private bool _isSubscribed;

        public bool IsInitialized => _settings != null;
        public int QueuedCount => _queue.Count;
        public CoLoggerWebSettings Settings => _settings;
        public bool IsBatchReady => _settings != null && _queue.Count >= Math.Max(1, _settings.BatchSize);

        public void Init(CoLoggerWebSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (IsInitialized)
            {
                Debug.LogWarning("CoLoggerWebModule is already initialized. Call Discard() before initializing it again.");
                return;
            }

            _settings = settings;
            Subscribe();
        }

        public void Discard()
        {
            Unsubscribe();
            _queue.Clear();
            _settings = null;
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
        }
    }
}
