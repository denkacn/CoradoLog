using System;

namespace CoradoLog.Web
{
    [Serializable]
    internal sealed class CoLoggerWebLogItem
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
