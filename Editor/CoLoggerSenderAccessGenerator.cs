using UnityEditor;
using UnityEngine;

namespace CoradoLog.Editor
{
    public class CoLoggerSenderAccessGenerator
    {
        public void GenerateSenderClass(string senderName, string generatePath)
        {
            var sourceBuilder = @"
using System;

namespace CoradoLog
{
    public static class Debug{senderName}
    {
        private const string Sender = ""{senderName}"";
        private const string Context = ""{contextName}"";
        
        public static void Log(string message, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, string.Empty, EDebugImportance.All, ex);
        }

        public static void Log(string message, EDebugImportance importance, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, string.Empty, importance, ex);
        }
        
        public static void Log(string message, string context, EDebugImportance importance = EDebugImportance.All, Exception ex = null)
        {
            CoLogger.Log(message, Sender, context, string.Empty, importance, ex);
        }

        public static void LogTag(string message, string tag = """", EDebugImportance importance = EDebugImportance.All, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, tag, importance, ex);
        }
        
        public static void LogError(string message, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, string.Empty, EDebugImportance.All, ex);
        }
        
        public static void LogTagError(string message, string tag = """", Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, tag, EDebugImportance.All, ex);
        }
    }
}";
            var correctCode = sourceBuilder.Replace("{senderName}", senderName);
            correctCode = correctCode.Replace("{contextName}", "Debug");

            var directoryPath = Application.dataPath + "/" + generatePath + "CoradoLogGenerated/";
            CoLoggerTools.CheckDirectory(directoryPath);
            
            var path = Application.dataPath + "/" + generatePath + "CoradoLogGenerated/Debug" + senderName + ".cs";
            CoLoggerTools.WriteFile(path, correctCode);

            AssetDatabase.Refresh();
        }
    }
}