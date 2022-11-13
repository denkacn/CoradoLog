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

        public void Log(string message, Exception ex = null)
        {
            CoLogger.Log(message, _sender, _context, _importance, ex);
        }
    }
}