using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoradoLog.Editor
{
    public static class CoLoggerTools
    {
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach",
            "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
            "namespace", "new", "null", "object", "operator", "out", "override", "params", "private",
            "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
            "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try",
            "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while"
        };

        public static void CheckDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static void WriteFile(string path, string text)
        {
            File.WriteAllText(path, text, Encoding.UTF8);
        }

        public static string GetGeneratedDirectory(string generatePath)
        {
            var normalizedPath = (generatePath ?? string.Empty).Replace('\\', '/').TrimStart('/');
            return Path.Combine(Application.dataPath, normalizedPath, "CoradoLogGenerated");
        }

        public static string GetSafeIdentifier(string rawName, string fallbackName)
        {
            var source = string.IsNullOrWhiteSpace(rawName) ? fallbackName : rawName.Trim();
            var builder = new StringBuilder(source.Length + 1);

            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('_');
                }
            }

            var identifier = builder.ToString().Trim('_');
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = fallbackName;
            }

            if (char.IsDigit(identifier[0]))
            {
                identifier = "_" + identifier;
            }

            if (CSharpKeywords.Contains(identifier))
            {
                identifier = "_" + identifier;
            }

            return identifier;
        }

        public static string GetUniqueIdentifier(string rawName, HashSet<string> usedIdentifiers, string fallbackName)
        {
            var identifier = GetSafeIdentifier(rawName, fallbackName);
            var uniqueIdentifier = identifier;
            var index = 2;
            while (!usedIdentifiers.Add(uniqueIdentifier))
            {
                uniqueIdentifier = identifier + "_" + index;
                index++;
            }

            return uniqueIdentifier;
        }

        public static string EscapeStringLiteral(string value)
        {
            if (value == null) return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
