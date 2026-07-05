using System;
using UnityEngine;

namespace CoradoLog.Web
{
    [CreateAssetMenu(fileName = "CoLoggerWebSettings", menuName = "CoLogger/Web Settings", order = 2)]
    [Serializable]
    public sealed class CoLoggerWebSettings : ScriptableObject
    {
        [Header("Connection")]
        public string BaseUrl = "https://logs.bypuziki.com";
        public string ApiToken = string.Empty;

        [Header("Sources")]
        public bool SendCoLoggerLogs = true;
        public bool SendUnityLogs = true;

        [Header("Client")]
        public string Source = "Unity";
        public string AppVersion = string.Empty;
        public string BuildNumber = string.Empty;
        public string ExternalUserId = string.Empty;
        public bool UseDeviceUniqueIdentifier = false;

        [Header("Duplicate Protection")]
        public bool IsDuplicateProtectionEnabled = true;
        public int MaxSameLogsPerWindow = 3;
        public float DuplicateWindowSeconds = 5f;
        public bool SendDuplicateSummary = true;

        [Header("Batching")]
        public int BatchSize = 50;
        public float FlushIntervalSeconds = 5f;
        public int MaxQueueSize = 1000;
        public bool FlushOnApplicationQuit = true;

        public bool IsReady => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiToken);
    }
}




