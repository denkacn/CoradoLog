using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CoradoLog
{
    public class CoLoggerHtmlFileWriter
    {
        private static readonly Regex ColorTagRegex = new Regex(@"<color=([^>]+)>(.*?)</color>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex CssColorRegex = new Regex(@"^#?[a-zA-Z0-9]+$", RegexOptions.Compiled);

        private StringBuilder _logEntries;
        private string _logFilePath;
        private bool _isOnlyCoLoggerLogs;
        private bool _isSubscribed;
        private bool _isDiscarded;
        private int _logCountSinceFlush;
        private int _entryIndex;
        
        public void Init(bool isOnlyCoLoggerLogs, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("HTML log file path is empty.", nameof(filePath));

            _isOnlyCoLoggerLogs = isOnlyCoLoggerLogs;
            _logFilePath = filePath;
            _logEntries = new StringBuilder();
            _isDiscarded = false;
            _logCountSinceFlush = 0;
            _entryIndex = 0;

            var directoryPath = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
            _isSubscribed = true;

            UpdateLogFile();
        }
        
        public void Discard()
        {
            if (_isDiscarded) return;

            if (_isSubscribed)
            {
                Application.logMessageReceived -= HandleLog;
                _isSubscribed = false;
            }

            UpdateLogFile();
            _isDiscarded = true;
        }
        
        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (_isDiscarded) return;

            var safeLogString = logString ?? string.Empty;
            if (_isOnlyCoLoggerLogs && !safeLogString.Contains("[CL]")) return;

            _entryIndex++;
            _logCountSinceFlush++;
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = FormatColoredMessage(safeLogString);
            var formattedStackTrace = EscapeHtml(stackTrace);
            var dataMessage = EscapeHtmlAttribute(safeLogString + type);
            var entryId = $"entry_{timestamp.Replace(":", "-").Replace(".", "-")}_{_entryIndex}";
            var logType = type.ToString();
            var logTypeClass = logType.ToLowerInvariant();
            
            var logEntry = $@"
        <div class='log-entry' id='{entryId}' data-message='{dataMessage}'>
            <div class='log-header' onclick='toggleLog(this)'>
                <div>
                    <span class='log-type type-{logTypeClass}'>{EscapeHtml(logType)}</span>
                    <span class='log-message'>{formattedMessage}</span>
                    <span class='log-count' style='color: #999; margin-left: 6px;'>(x1)</span>
                </div>
                <span class='arrow'>v</span>
            </div>
            <div class='log-content'>
                <div class='stack-trace'>{formattedStackTrace}</div>
            </div>
        </div>";

            _logEntries.Append(logEntry);

            if (_logCountSinceFlush >= 50)
            {
                _logCountSinceFlush = 0;
                UpdateLogFile();
            }
        }

        private void UpdateLogFile()
        {
            if (string.IsNullOrEmpty(_logFilePath) || _logEntries == null) return;

            try
            {
                var finalContent = GetHtmlHeader() + _logEntries + GetHtmlFooter();
                File.WriteAllText(_logFilePath, finalContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update CoLogger HTML log: {ex.Message}");
            }
        }
        
        private string FormatColoredMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            var result = new StringBuilder();
            var currentIndex = 0;
            var matches = ColorTagRegex.Matches(message);

            foreach (Match match in matches)
            {
                if (match.Index > currentIndex)
                {
                    result.Append(EscapeHtml(message.Substring(currentIndex, match.Index - currentIndex)));
                }

                var color = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                if (IsSafeCssColor(color))
                {
                    result.Append($"<span style='color: {EscapeHtmlAttribute(color)};'>");
                    result.Append(EscapeHtml(content));
                    result.Append("</span>");
                }
                else
                {
                    result.Append(EscapeHtml(match.Value));
                }

                currentIndex = match.Index + match.Length;
            }

            if (currentIndex < message.Length)
            {
                result.Append(EscapeHtml(message.Substring(currentIndex)));
            }

            return result.ToString();
        }

        private bool IsSafeCssColor(string color)
        {
            return !string.IsNullOrEmpty(color) && CssColorRegex.IsMatch(color);
        }
        
        private string EscapeHtml(string input)
        {
            return WebUtility.HtmlEncode(input ?? string.Empty);
        }

        private string EscapeHtmlAttribute(string input)
        {
            return EscapeHtml(input).Replace("'", "&#x27;");
        }

        private string GetHtmlHeader()
        {
            return @"<!DOCTYPE html>
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
    <div id='logContainer'>";
        }

        private string GetHtmlFooter()
        {
            return @"
    </div>
    <script>
        function toggleLog(header) {
            const content = header.nextElementSibling;
            const arrow = header.lastElementChild;
            if (content.style.display === 'none') {
                content.style.display = 'block';
                arrow.textContent = '^';
            } else {
                content.style.display = 'none';
                arrow.textContent = 'v';
            }
        }

        function toggleAll(expand) {
            const entries = document.querySelectorAll('.log-entry');
            entries.forEach(entry => {
                const content = entry.querySelector('.log-content');
                const arrow = entry.querySelector('.arrow');

                if (expand) {
                    content.style.display = 'block';
                    arrow.textContent = '^';
                } else {
                    content.style.display = 'none';
                    arrow.textContent = 'v';
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

            const seen = new Map();
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

            for (const item of seen.values()) {
                const counter = item.entry.querySelector('.log-count');
                if (counter) counter.textContent = '(x' + item.count + ')';
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

        window.addEventListener('load', function() {
            window.scrollTo(0, document.body.scrollHeight);
        });
    </script>
</body>
</html>";
        }
    }
}

