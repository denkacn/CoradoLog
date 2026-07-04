using System.IO;
using UnityEditor;

namespace CoradoLog.Editor
{
    public class CoLoggerSenderAccessGenerator
    {
        public void GenerateSenderClass(string senderName, string generatePath)
        {
            if (string.IsNullOrWhiteSpace(senderName)) return;

            var senderIdentifier = CoLoggerTools.GetSafeIdentifier(senderName, "Sender");
            var senderLiteral = CoLoggerTools.EscapeStringLiteral(senderName);
            var contextLiteral = CoLoggerTools.EscapeStringLiteral("Debug");
            var className = "Debug" + senderIdentifier;

            var sourceBuilder = @"
using System;

namespace CoradoLog
{
    public static class {className}
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
            var correctCode = sourceBuilder.Replace("{className}", className);
            correctCode = correctCode.Replace("{senderName}", senderLiteral);
            correctCode = correctCode.Replace("{contextName}", contextLiteral);

            var directoryPath = CoLoggerTools.GetGeneratedDirectory(generatePath);
            CoLoggerTools.CheckDirectory(directoryPath);
            
            var path = Path.Combine(directoryPath, className + ".cs");
            CoLoggerTools.WriteFile(path, correctCode);

            AssetDatabase.Refresh();
        }
    }
}
