using System.IO;

namespace CoradoLog
{
    public class CoLoggerFileWriter
    {
        private StreamWriter _writer;
        private string _logFilePath;
        
        public void Init(string logFilePath)
        {
            _logFilePath = logFilePath;
            _writer = new StreamWriter(_logFilePath, append: true);
            _writer.AutoFlush = true;
        }

        public void Write(string message)
        {
            _writer.WriteLine(message);
        }

        public void Discard()
        {
            _writer?.Close();
        }
    }
}