using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerHtmlFileWriter
    {
        private StringBuilder _htmlContent;
        private string _logFilePath;
        private bool _isOnlyCoLoggerLogs;
        
        public void Init(bool isOnlyCoLoggerLogs, string filePath)
        {
            _isOnlyCoLoggerLogs = isOnlyCoLoggerLogs;
            _logFilePath = filePath;
            
            Application.logMessageReceived += HandleLog;
            InitializeHtmlFile();
        }
        
        public void Discard()
        {
            Application.logMessageReceived -= HandleLog;

            UpdateLogFile();
        }

        private void InitializeHtmlFile()
        {
            //_logFilePath = Path.Combine(Application.dataPath, "debug_log.html");
            _htmlContent = new StringBuilder();

            _htmlContent.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Unity Debug Log</title>
    <style>
        body {
            font-family: 'Courier New', monospace;
            margin: 0;
            padding: 20px;
            background-color: #1e1e1e;
            color: #d4d4d4;
        }
        .header {
            background-color: #2d2d2d;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
        }
        .search-box {
            width: 80%;
            padding: 10px;
            margin-bottom: 15px;
            background-color: #3c3c3c;
            color: #d4d4d4;
            border: 1px solid #4a4a4a;
            border-radius: 3px;
        }
        .log-entry {
            background-color: #2d2d2d;
            margin-bottom: 10px;
            border-radius: 5px;
            overflow: hidden;
            border-left: 4px solid #6a6a6a;
        }
        .log-header {
            padding: 10px;
            cursor: pointer;
            display: flex;
            justify-content: space-between;
            align-items: center;
            background-color: #3c3c3c;
        }
        .arrow {
        }
        .log-time {
            font-size: 12px;
            opacity: 0.8;
        }
        .log-type {
            padding: 3px 8px;
            border-radius: 3px;
            font-weight: bold;
            font-size: 12px;
        }
        .type-error { background-color: #ff4d4d; color: white; }
        .type-warning { background-color: #ffd700; color: black; }
        .type-log { background-color: #4d79ff; color: white; }
        .type-exception { background-color: #ff6b6b; color: white; }
        .log-content {
            padding: 10px;
            display: none;
        }
        .stack-trace {
            background-color: #3c3c3c;
            padding: 10px;
            border-radius: 3px;
            margin-top: 10px;
            font-size: 12px;
            white-space: pre-wrap;
            overflow-x: auto;
        }
        .log-message {
            margin-bottom: 5px;
        }
        .controls {
            margin-bottom: 15px;
        }
        .toggle-btn {
            background-color: #4a4a4a;
            color: white;
            border: none;
            padding: 5px 10px;
            margin-right: 5px;
            cursor: pointer;
            border-radius: 3px;
        }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Co Logger HTML Viewer</h1>
        <div class='controls'>
            <input type='text' id='searchBox' class='search-box' placeholder='Search by logs...' onkeyup='filterLogs()'>
            <br>
            <button class='toggle-btn' onclick='toggleAll(true)'>Expand</button>
            <button class='toggle-btn' onclick='toggleAll(false)'>Collapse</button>
            <button class='toggle-btn' onclick='groupLogs()'>Group</button>
            <button class='toggle-btn' onclick='ungroupLogs()'>Ungroup</button>
        </div>
    </div>
    <div id='logContainer'>");

            File.WriteAllText(_logFilePath, _htmlContent.ToString());
        }

        private int _logCount;
        
        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (_isOnlyCoLoggerLogs && !logString.Contains("[CL]")) return;

            _logCount++;
            
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = FormatColoredMessage(logString);
            var formattedStackTrace = HttpUtility.HtmlEncode(stackTrace);
            
            var logEntry = $@"
        <div class='log-entry' id='entry_{timestamp.Replace(":", "-")}' data-message='{EscapeHtml(logString) + type}'>
            <div class='log-header' onclick='toggleLog(this)'>
                <div>
                    <span class='log-type type-{type.ToString().ToLower()}'>{type.ToString()}</span>
                    <span class='log-message'>{formattedMessage}</span>
                    <span class='log-count' style='color: #999; margin-left: 6px;'>(x1)</span>
                </div>
                <span class='arrow'>▼</span>
            </div>
            <div class='log-content'>
                <div class='stack-trace'>{formattedStackTrace}</div>
            </div>
        </div>";

            _htmlContent.Append(logEntry);

            if (_logCount >= 50)
            {
                _logCount = 0;
                UpdateLogFile();
            }
        }

        private void UpdateLogFile()
        {
            var content = _htmlContent.ToString();
            var endIndex = content.LastIndexOf("</div>", StringComparison.Ordinal) - 10; // Позиция перед закрывающим тегом
            var finalContent = content.Substring(0, endIndex) + @"
    </div>
    <script>
        function toggleLog(header) {
            const content = header.nextElementSibling;
            const arrow = header.lastElementChild;
            if (content.style.display === 'none') {
                content.style.display = 'block';
                arrow.textContent = '▲';
            } else {
                content.style.display = 'none';
                arrow.textContent = '▼';
            }
        }

        function toggleAll(expand) {
            const entries = document.querySelectorAll('.log-entry');
            entries.forEach(entry => {
                const content = entry.querySelector('.log-content');
                const header = entry.querySelector('.log-header');
                //const arrow = header.querySelector('span:last-child');
                const arrow = entry.querySelector('.arrow');

                if (expand) {
                    content.style.display = 'block';
                    arrow.textContent = '▲';
                } else {
                    content.style.display = 'none';
                    arrow.textContent = '▼';
                }
            });
        }

        function groupLogs() {
            const container = document.getElementById('logContainer');
            const entries = Array.from(container.querySelectorAll('.log-entry'));

            entries.forEach(e => {
                e.style.display = 'block';
                e.removeAttribute('data-duplicate');
                const c = e.querySelector('.log-count');
                if (c) c.textContent = '(x1)';
            });

            const seen = new Map(); // msg -> { entry, count }
            for (const entry of entries) {
                const msg = entry.getAttribute('data-message') || '';
                if (!seen.has(msg)) {
                    seen.set(msg, { entry, count: 1 });
                } else {
                    const item = seen.get(msg);
                    item.count++;
                    entry.setAttribute('data-duplicate', '1');
                }
            }

            for (const { entry, count } of seen.values()) {
                const counter = entry.querySelector('.log-count');
                if (counter) counter.textContent = `(x${count})`;
            }
            entries.forEach(e => {
                if (e.getAttribute('data-duplicate') === '1') {
                    e.style.display = 'none';
                }
            });

            container.dataset.grouped = 'true';
        }

        function ungroupLogs() {
            const container = document.getElementById('logContainer');
            const entries = container.querySelectorAll('.log-entry');

            entries.forEach(e => {
                e.style.display = 'block';
                e.removeAttribute('data-duplicate');
                const c = e.querySelector('.log-count');
                if (c) c.textContent = '(x1)';
            });

            container.dataset.grouped = 'false';
        }

        function filterLogs() {
            const input = document.getElementById('searchBox');
            const filter = input.value.toLowerCase();
            const entries = document.querySelectorAll('.log-entry');
            
            entries.forEach(entry => {
                const message = entry.querySelector('.log-message').textContent.toLowerCase();
                const stackTrace = entry.querySelector('.stack-trace').textContent.toLowerCase();
                
                if (message.includes(filter) || stackTrace.includes(filter)) {
                    entry.style.display = 'block';
                } else {
                    entry.style.display = 'none';
                }
            });
        }

        // Автоскролл к новому логу
        window.addEventListener('load', function() {
            window.scrollTo(0, document.body.scrollHeight);
        });
    </script>
</body>
</html>";

            File.WriteAllText(_logFilePath, finalContent);
        }
        
        private string FormatColoredMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "";
            
            var formatted = message;
            
            formatted = Regex.Replace(formatted, @"$<color=([^>]+)>([^<]+)</color>$", 
                match => $"<span style='color: {match.Groups[1].Value};'>{match.Groups[2].Value}</span>");
            
            formatted = Regex.Replace(formatted, @"<color=([^>]+)>([^<]+)</color>", 
                match => $"<span style='color: {match.Groups[1].Value};'>{match.Groups[2].Value}</span>");
            
            formatted = Regex.Replace(formatted, @"<color=([^>]+)>([^<]+)</color>", 
                match => $"<span style='color: {match.Groups[1].Value};'>{match.Groups[2].Value}</span>");

            return formatted;
        }
        
        private string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            
            return input.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;");
        }
    }
}