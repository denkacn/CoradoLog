using System.IO;

namespace CoradoLog.Editor
{
    public static class CoLoggerTools
    {
        public static void CheckDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static void WriteFile(string path, string text)
        {
            File.WriteAllText(path, text);
        }
    }
}