using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerFileWriter
    {
        private StreamWriter _writer;
        private string _logFilePath;
        private bool _isDisposed;
        
        public void Init(string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
                throw new ArgumentException("Log file path is empty.", nameof(logFilePath));

            _logFilePath = logFilePath;
            var directoryPath = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            _writer = new StreamWriter(_logFilePath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
            _isDisposed = false;
        }

        public void Write(string message)
        {
            if (_isDisposed || _writer == null) return;

            try
            {
                _writer.WriteLine(message ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write CoLogger file log: {ex.Message}");
            }
        }

        public void Discard()
        {
            if (_isDisposed) return;

            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to close CoLogger file writer: {ex.Message}");
            }
            finally
            {
                _writer = null;
                _isDisposed = true;
            }
        }
    }
}

