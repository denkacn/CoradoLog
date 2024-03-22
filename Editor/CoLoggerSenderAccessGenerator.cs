using UnityEditor;
using UnityEngine;

namespace CoradoLog.Editor
{
    public class CoLoggerSenderAccessGenerator
    {
        public void GenerateSenderClass(string senderName)
        {
            var sourceBuilder = @"
using System;

namespace CoradoLog
{
    public static class Debug{senderName}
    {
        private const string Sender = ""{senderName}"";
        private const string Context = ""{contextName}"";
        
        public static void Log(string message, string tag = "", Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, tag, EDebugImportance.All, ex);
        }

        public static void Log(string message, string tag = "", Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, tag, EDebugImportance.All, ex);
        }

        public static void Log(string message, string tag = "", EDebugImportance importance, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, tag, importance, ex);
        }
        
        public static void Log(string message, string context, string tag = "", EDebugImportance importance = EDebugImportance.All, Exception ex = null)
        {
            CoLogger.Log(message, Sender, context, tag, importance, ex);
        }
    }
}";
            var correctCode = sourceBuilder.Replace("{senderName}", senderName);
            correctCode = correctCode.Replace("{contextName}", "Debug");

            var directoryPath = Application.dataPath + "/CoradoLogGenerated/";
            CoLoggerTools.CheckDirectory(directoryPath);
            
            var path = Application.dataPath + "/CoradoLogGenerated/Debug" + senderName + ".cs";
            CoLoggerTools.WriteFile(path, correctCode);

            AssetDatabase.Refresh();
        }
    }
}