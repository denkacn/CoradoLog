using System;

namespace CoradoLog
{
    public enum CoLoggerEntryLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public sealed class CoLoggerEntry
    {
        public CoLoggerEntry(
            DateTime timestamp,
            long sequence,
            CoLoggerEntryLevel level,
            string message,
            string sender,
            string context,
            string tag,
            EDebugImportance importance,
            Exception exception,
            object customData)
        {
            Timestamp = timestamp;
            Sequence = sequence;
            Level = level;
            Message = message ?? string.Empty;
            Sender = sender ?? string.Empty;
            Context = context ?? string.Empty;
            Tag = tag ?? string.Empty;
            Importance = importance;
            Exception = exception;
            CustomData = customData;
        }

        public DateTime Timestamp { get; }
        public long Sequence { get; }
        public CoLoggerEntryLevel Level { get; }
        public string Message { get; }
        public string Sender { get; }
        public string Context { get; }
        public string Tag { get; }
        public EDebugImportance Importance { get; }
        public Exception Exception { get; }
        public object CustomData { get; }
    }
}


