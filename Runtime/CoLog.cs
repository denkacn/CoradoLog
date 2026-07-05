using System;

namespace CoradoLog
{
    public class CoLog
    {
        private readonly string _sender;
        private readonly string _context;
        private readonly EDebugImportance _importance;
        
        public CoLog(string sender, string context, EDebugImportance importance = EDebugImportance.All)
        {
            _sender = sender;
            
            CoLogger.AddContext(context);
            
            _context = context;
            _importance = importance;
        }

        public void Log(string message, string tag = "", Exception ex = null, object customData = null)
        {
            CoLogger.Log(message, _sender, _context, tag, _importance, ex, customData);
        }

        public void LogWarning(string message, string tag = "", object customData = null)
        {
            CoLogger.LogWarning(message, _sender, _context, tag, _importance, customData);
        }

        public void LogError(string message, string tag = "", Exception ex = null, object customData = null)
        {
            CoLogger.LogError(message, _sender, _context, tag, _importance, ex, customData);
        }
    }
}


