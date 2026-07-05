using System;
using System.Collections.Generic;

namespace CoradoLog.Web
{
    [Serializable]
    internal sealed class CoLoggerWebBatchRequest
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
}
