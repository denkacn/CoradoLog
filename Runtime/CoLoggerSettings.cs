using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoradoLog
{
    [CreateAssetMenu(fileName = "CoLoggerSettingsInstance", menuName = "CoLogger/CoLoggerSettings", order = 1)]
    [Serializable]
    public class CoLoggerSettings : ScriptableObject
    {
        public EDebugImportance Importance;
        
        public List<ImportanceSetting> ImportanceSettings = new List<ImportanceSetting>()
        {
            new ImportanceSetting(EDebugImportance.All, Color.white),
            new ImportanceSetting(EDebugImportance.Medium, Color.yellow),
            new ImportanceSetting(EDebugImportance.Important, Color.magenta),
            new ImportanceSetting(EDebugImportance.Critical, Color.red)
        };
        
        public List<SenderSetting> SendeSettings = new List<SenderSetting>()
        {
            new SenderSetting("System"),
            new SenderSetting("Debug")
        };

        public List<ContextSetting> ContextSettings = new List<ContextSetting>()
        {
            new ContextSetting("System", new Color32(146, 181, 197, 255)),
            new ContextSetting("Debug", new Color32(210, 210, 210, 255))
        };

        public Color TagColor = Color.green;
        public List<string> Tags = new List<string>(); 

        public bool IsAddContextInRuntime = true;

        public bool IsLogToFile = false;
        public string FileWriterPath = "";
        
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

        public bool IsTagExist(string tag)
        {
            return Tags.Contains(tag);
        }
        
        public void AddContext(string contextName)
        {
            var context = ContextSettings.Find(s => s.ContextName == contextName);
            if (context != null) return;

            ContextSettings.Add(new ContextSetting()
                { ContextName = contextName, DrawColor = Color.magenta, IsEnable = true, IsRuntime = true });
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
        
        public SenderSetting(){}
        
        public SenderSetting(string senderName, bool isEnable = true)
        {
            SenderName = senderName;
            IsEnable = isEnable;
        }
    }

    [Serializable]
    public class ContextSetting
    {
        public string ContextName;
        public bool IsEnable;
        public Color DrawColor;

        //[HideInInspector]
        public bool IsRuntime = false;
        
        public ContextSetting(){}

        public ContextSetting(string contextName, Color drawColor, bool isEnable = true)
        {
            ContextName = contextName;
            IsEnable = isEnable;
            DrawColor = drawColor;
        }
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
