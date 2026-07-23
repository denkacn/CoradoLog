using System.IO;
using CoradoLog.Web;
using UnityEditor;
using UnityEngine;

namespace CoradoLog.Editor
{
    public static class CoLoggerSessionPrefixFileWriter
    {
        private const string DefaultSessionPrefixFilePath = "corado-session-prefix.txt";

        public static void WriteDefaultSessionPrefixFile(string prefix)
        {
            WriteSessionPrefixFile(DefaultSessionPrefixFilePath, prefix);
        }

        public static void WriteSessionPrefixFile(CoLoggerWebSettings settings, string prefix)
        {
            var relativePath = settings != null ? settings.SessionPrefixFilePath : DefaultSessionPrefixFilePath;
            WriteSessionPrefixFile(relativePath, prefix);
        }

        public static void WriteSessionPrefixFile(string relativePath, string prefix)
        {
            var assetPath = GetSessionPrefixAssetPath(relativePath);
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length)));
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(fullPath, prefix ?? string.Empty);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }

        public static string GetSessionPrefixAssetPath(CoLoggerWebSettings settings)
        {
            return GetSessionPrefixAssetPath(settings != null ? settings.SessionPrefixFilePath : DefaultSessionPrefixFilePath);
        }

        public static string GetSessionPrefixAssetPath(string relativePath)
        {
            var path = string.IsNullOrWhiteSpace(relativePath)
                ? DefaultSessionPrefixFilePath
                : relativePath.Trim();

            path = path.Replace('\\', '/').TrimStart('/');
            return $"Assets/StreamingAssets/{path}";
        }
    }
}
