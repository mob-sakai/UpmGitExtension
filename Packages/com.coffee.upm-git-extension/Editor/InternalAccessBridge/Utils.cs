#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Conditional = System.Diagnostics.ConditionalAttribute;
// using Semver;

namespace UnityEditor.PackageManager.UI
{
    public static class GitUtils
    {
        static readonly StringBuilder s_sbError = new StringBuilder();
        static readonly StringBuilder s_sbOutput = new StringBuilder();
        public delegate void CommandCallback(bool success, string output);

        static readonly Regex s_regRefs = new Regex("refs/(tags|remotes/origin)/([^/]+),(.+),(.+)$", RegexOptions.Compiled);

			
        // repourlから
        public static void GetRefs(string packageName, string repoUrl, Action<IEnumerable<string>> callback)
        {
            // StringBuilder command = new StringBuilder ();
            var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cacheRoot = new DirectoryInfo(Path.Combine(appDir, "UpmGitExtension"));
            var cacheDir = new DirectoryInfo(Path.Combine(cacheRoot.FullName, Uri.EscapeDataString(packageName + "@" + repoUrl)));
            var cacheFile = new FileInfo(Path.Combine(cacheDir.FullName, "versions"));

            // Cached.
            if (cacheFile.Exists && (DateTime.Now - cacheFile.LastWriteTime).TotalMinutes < 5)
            {
                var versions =  File.ReadAllText(cacheFile.FullName)
                    .Split('\n')
                    .Select(r => s_regRefs.Match(r))
                    .Where(m => m.Success && (packageName.Length == 0 || packageName == m.Groups[4].Value))
                    .Select(m =>m.Groups[2].Value + "," + m.Groups[3].Value + "," + m.Groups[4].Value);
                callback(versions);
            }
            else
            {
                var script = "Packages/com.coffee.upm-git-extension/Editor/InternalAccessBridge/get-available-refs.sh";
                var args = string.Format("{0} {1} {2}", repoUrl, cacheDir.FullName, UnityEngine.Application.unityVersion);
                ExecuteShell(script, args, (success) =>
                {
                    if (success)
                        GetRefs(packageName, repoUrl, callback);
                    else
                        callback(Enumerable.Empty<string>());
                });
            }
        }

        static void ExecuteShell(string script, string args, Action<bool> callback)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                FileName = script,
                UseShellExecute = true,
            };

            Debug.LogFormat("[ExecuteShell] script {0}, args = {1}", script, args);
            var launchProcess = System.Diagnostics.Process.Start(startInfo);
            if (launchProcess == null || launchProcess.HasExited == true || launchProcess.Id == 0)
            {
                Debug.LogErrorFormat("[ExecuteShell] failed: script = {0}, args = {1}");
                callback(false);
            }
            else
            {
                //Add process callback.
                launchProcess.Exited += (sender, e) =>
                {
                    bool success = 0 == launchProcess.ExitCode;
                    if (!success)
                    {
                        Debug.LogErrorFormat("Error: {0}\n\n{1}", args, s_sbError);
                    }
                    callback(success);
                };

                launchProcess.EnableRaisingEvents = true;
            }
        }

    }

    public static class PackageUtilsXXX
    {
        public static void InstallPackage(string packageName, string url, string refName)
        {
            const string manifestPath = "Packages/manifest.json";
            var manifest = MiniJSON.Json.Deserialize(System.IO.File.ReadAllText(manifestPath)) as Dictionary<string, object>;
            var dependencies = manifest["dependencies"] as Dictionary<string, object>;

            dependencies.Add(packageName, url + "#" + refName);

            System.IO.File.WriteAllText(manifestPath, MiniJSON.Json.Serialize(manifest));
            UnityEditor.EditorApplication.delayCall += () => UnityEditor.AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        public static void RemovePackage(string packageName)
        {
            const string manifestPath = "Packages/manifest.json";
            var manifest = MiniJSON.Json.Deserialize(System.IO.File.ReadAllText(manifestPath)) as Dictionary<string, object>;
            var dependencies = manifest["dependencies"] as Dictionary<string, object>;

            dependencies.Remove(packageName);

            System.IO.File.WriteAllText(manifestPath, MiniJSON.Json.Serialize(manifest));
            UnityEditor.EditorApplication.delayCall += () => UnityEditor.AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        public static string GetFilePath(string resolvedPath, string filePattern)
        {
            if (string.IsNullOrEmpty(resolvedPath) || string.IsNullOrEmpty(filePattern))
                return "";

            foreach (var path in Directory.GetFiles(resolvedPath, filePattern))
            {
                if (!path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    return path;
                }
            }
            return "";
        }

        public static string GetRepoHttpsUrl (string packageId)
		{
			Match m = Regex.Match (packageId, "^[^@]+@([^#]+)(#.+)?$");
			if (m.Success)
			{
				var repoUrl = m.Groups [1].Value;
				repoUrl = Regex.Replace (repoUrl, "(git:)?git@([^:]+):", "https://$2/");
				repoUrl = repoUrl.Replace ("ssh://", "https://");
				repoUrl = repoUrl.Replace ("git@", "");
				repoUrl = Regex.Replace (repoUrl, "\\.git$", "");

				return repoUrl;
			}
			return "";
		}
    }
}
