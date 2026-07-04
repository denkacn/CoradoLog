using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace CoradoLog.Editor
{
    public class CoLoggerVarsGenerator
    {
        public void GenerateVarsClass(string[] senderNames, string[] contextNames, string generatePath)
        {
            var sourceBuilder = @"
namespace CoradoLog
{
    public static class CoLoggerVars
    {
        public static class Senders
        {
{senders}
        }
        
        public static class Contexts
        {
{contexts}
        }
    }    
}
";
            var correctCode = sourceBuilder.Replace("{senders}", BuildVarsBlock(senderNames, "Sender"));
            correctCode = correctCode.Replace("{contexts}", BuildVarsBlock(contextNames, "Context"));
            
            var directoryPath = CoLoggerTools.GetGeneratedDirectory(generatePath);
            CoLoggerTools.CheckDirectory(directoryPath);

            var path = Path.Combine(directoryPath, "CoLoggerVars.cs");
            CoLoggerTools.WriteFile(path, correctCode);

            AssetDatabase.Refresh();
        }

        private string BuildVarsBlock(string[] names, string fallbackName)
        {
            var builder = new StringBuilder();
            var usedIdentifiers = new HashSet<string>();
            if (names == null) return string.Empty;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                var identifier = CoLoggerTools.GetUniqueIdentifier(name, usedIdentifiers, fallbackName);
                var value = CoLoggerTools.EscapeStringLiteral(name);
                builder.AppendFormat("            public static string {0} = \"{1}\";\n", identifier, value);
            }

            return builder.ToString();
        }
    }
}
