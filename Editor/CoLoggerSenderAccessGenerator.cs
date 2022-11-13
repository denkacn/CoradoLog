using System.IO;
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

namespace CoradoLogSystem
{
    public static class Debug{senderName}
    {
        private const string Sender = ""{senderName}"";
        private const string Context = ""{contextName}"";
        
        public static void Log(string message, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, EDebugImportance.All, ex);
        }

        public static void Log(string message, EDebugImportance importance = EDebugImportance.All, Exception ex = null)
        {
            CoLogger.Log(message, Sender, Context, importance, ex);
        }
        
        public static void Log(string message, string context, EDebugImportance importance = EDebugImportance.All, Exception ex = null)
        {
            CoLogger.Log(message, Sender, context, importance, ex);
        }
    }
}";
            var correctCode = sourceBuilder.Replace("{senderName}", senderName);
            correctCode = correctCode.Replace("{contextName}", "Debug");

            
            var path = Application.dataPath + "/CoradoLogGenerated/Debug" + senderName + ".cs";
            File.WriteAllText(path, correctCode);
            
            AssetDatabase.Refresh();
        }
    }
}