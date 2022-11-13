using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoradoLog
{
    [Serializable]
    public class CoLoggerSettings
    {
        public EDebugImportance Importance;
        
        public List<ImportanceSetting> ImportanceSettings = new List<ImportanceSetting>()
        {
            new ImportanceSetting(EDebugImportance.All, Color.white),
            new ImportanceSetting(EDebugImportance.Medium, Color.yellow),
            new ImportanceSetting(EDebugImportance.Important, Color.magenta),
            new ImportanceSetting(EDebugImportance.Critical, Color.red)
        };
        
        public List<SenderSetting> SendeSettings;
        public List<ContextSetting> ContextSettings;

        public bool IsAddContextInRuntime = true;
        
        public bool IsSenderExist(string senderName)
        {
            var sender = SendeSettings.Find(s => s.SenderName == senderName);
            return sender != null && sender.IsEnable;
        }

        public bool IsContextExist(string contextName)
        {
            var context = ContextSettings.Find(s => s.ContextName == contextName);
            return context != null && context.IsEnable;
        }

        public void AddContext(string contextName)
        {
            var context = ContextSettings.Find(s => s.ContextName == contextName);
            if (context != null) return;

            ContextSettings.Add(new ContextSetting()
                { ContextName = contextName, DrawColor = Color.magenta, IsEnable = true });
        }
    }

    public enum EDebugImportance
    {
        All = 0,
        Medium = 1,
        Important = 2,
        Critical = 3,
    }

    [Serializable]
    public class SenderSetting
    {
        public string SenderName;
        public bool IsEnable;
    }

    [Serializable]
    public class ContextSetting
    {
        public string ContextName;
        public bool IsEnable;
        public Color DrawColor;
    }

    [Serializable]
    public class ImportanceSetting
    {
        public EDebugImportance Importance;
        public Color DrawColor;

        public ImportanceSetting()
        {
        }

        public ImportanceSetting(EDebugImportance importance, Color drawColor)
        {
            Importance = importance;
            DrawColor = drawColor;
        }
    }
}
