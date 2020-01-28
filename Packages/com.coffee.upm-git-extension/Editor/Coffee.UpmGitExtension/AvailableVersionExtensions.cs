#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace Coffee.UpmGitExtension
{
    public class AvailableVersionExtensions
    {
        const string kCacheDir = "Library/UpmGitExtension";
        const string kResultDir = kCacheDir + "/results";
        const string kHeader = "<b><color=#c7634c>[AvailableVersionExtensions]</color></b> ";
        const string kGetVersionsJs = "Packages/com.coffee.upm-git-extension/Editor/Commands/get-available-versions.js";

        public static void UpdateAvailableVersions(string packageName = "all", string repoUrl = "", Action<int> callback = null)
        {
            var unity = Application.unityVersion;
#if UNITY_EDITOR_WIN
            var node = Path.Combine(EditorApplication.applicationContentsPath, "Tools/nodejs/node.exe").Replace('/', '\\');
#else
            var node = Path.Combine(EditorApplication.applicationContentsPath, "Tools/nodejs/bin/node");
#endif

            var psi = new ProcessStartInfo()
            {
                FileName = node,
                Arguments = string.Format("\"{0}\" {1} {2} {3}", kGetVersionsJs, packageName, repoUrl, unity)
            };
            Debug.Log(kHeader, $"{psi.FileName} {psi.Arguments}");

            var p = new UnityEditor.Utils.Program(psi);
            if (callback != null)
                p.Start((_, __) => callback(p._process.ExitCode));
            else
                p.Start();
        }

        static void OnResultCreated(string file)
        {
            if(string.IsNullOrEmpty(file) || Path.GetExtension(file) != ".json" || !File.Exists(file))
                return;

            var text = File.ReadAllText(file, System.Text.Encoding.UTF8);
            File.Delete(file);
            UnityEngine.Debug.Log($"{kHeader} OnResultCreated {file}:\n{text}");

            try
            {
                AvailableVersions.AddVersions(JsonUtility.FromJson<ResultInfo>(text).versions);
            }
            catch (Exception ex)
            {
                Debug.Exception(kHeader, ex);
            }
        }

        [InitializeOnLoadMethod]
        static void WatchResultJson()
        {
            Debug.Log(kHeader, $"Start to watch .json in {kResultDir}");

#if !UNITY_EDITOR_WIN
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
            if (!Directory.Exists(kResultDir))
                Directory.CreateDirectory(kResultDir);

            foreach (var file in Directory.GetFiles(kResultDir, "*.json"))
                EditorApplication.delayCall += () => OnResultCreated(file);

            var watcher = new FileSystemWatcher()
            {
                Path = kResultDir,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };

            watcher.Created += (s, e)=>
            {
                EditorApplication.delayCall += () => OnResultCreated(e.Name);
            };
        }
    }
}
#endif // This line is added by Open Sesame Portable. DO NOT remov manually.