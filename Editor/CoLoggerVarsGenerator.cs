using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

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
            var senders = new StringBuilder();

            foreach (var sender in senderNames)
            {
                senders.AppendFormat("\t\t\tpublic static string {0} = \"{0}\";\n", sender);
            }
            
            var contexts = new StringBuilder();
            
            foreach (var context in contextNames)
            {
                contexts.AppendFormat("\t\t\tpublic static string {0} = \"{0}\";\n", context);
            }

            var correctCode = sourceBuilder.Replace("{senders}", senders.ToString());
            correctCode = correctCode.Replace("{contexts}", contexts.ToString());
            
            var directoryPath = Application.dataPath + "/" + generatePath + "CoradoLogGenerated/";
            CoLoggerTools.CheckDirectory(directoryPath);

            var path = Application.dataPath + "/" + generatePath + "CoradoLogGenerated/CoLoggerVars.cs";
            CoLoggerTools.WriteFile(path, correctCode);

            AssetDatabase.Refresh();
        }
    }
}