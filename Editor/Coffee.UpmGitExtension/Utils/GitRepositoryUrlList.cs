using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEditorInternal;

namespace Coffee.UpmGitExtension
{
    internal static class GitRepositoryUrlList
    {
        private static string _workingDirectory => InternalEditorUtility.unityPreferencesFolder + "/GitPackageDatabase";
        private static string _cacheFile => _workingDirectory + "/GitRepositoryUrlList.txt";

        [Serializable]
        internal class FetchResultUrl
        {
            public string url;
        }

        public static void AddUrl(string url)
        {
            url = Regex.Replace(url, "(#.*)$", "");
            if (File.Exists(_cacheFile) && !File.ReadAllLines(_cacheFile).Contains(url))
            {
                File.AppendAllLines(_cacheFile, new[] { url });
            }
        }

        public static string[] GetUrls()
        {
            if (!File.Exists(_cacheFile))
            {
                var urls = Directory.GetDirectories(_workingDirectory, "Results*")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json"))
                    .Select(file => File.ReadAllText(file, System.Text.Encoding.UTF8))
                    .Select(text => JsonUtility.FromJson<FetchResultUrl>(text))
                    .Select(result => Regex.Replace(result.url, "(#.*)$", ""))
                    .Distinct();
                File.WriteAllLines(_cacheFile, urls);
            }

            return File.Exists(_cacheFile)
                ? File.ReadAllLines(_cacheFile)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .ToArray()
                : Array.Empty<string>();
        }
    }
}