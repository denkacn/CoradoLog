using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoradoLog.Editor
{
    public class CoLoggerVarsGenerator
    {
        public void GenerateVarsClass(string[] senderNames, string[] contextNames)
        {
            var sourceBuilder = @"
namespace CoradoLogSystem
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
            
            var path = Application.dataPath + "/CoradoLogGenerated/CoLoggerVars.cs";
            File.WriteAllText(path, correctCode);
            
            AssetDatabase.Refresh();
        }
    }
}