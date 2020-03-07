#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
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
        const string kCacheDir = "Library/UGE";
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
            var args = string.Format("\"{0}\" {1} {2} {3}", Path.GetFullPath(kGetVersionsJs), packageName, repoUrl, unity);
            Debug.Log(kHeader, $"{node} {args}");

            var p = new UnityEditorInternal.NativeProgram(node, args);
            p.Start((_, __) =>
            {
#if UGE_LOG
                UnityEngine.Debug.Log(p.GetAllOutput());
#endif
                if (callback != null)
                    callback(p._process.ExitCode);
            });
        }

        static void OnResultCreated(string file)
        {
            if (string.IsNullOrEmpty(file) || Path.GetExtension(file) != ".json" || !File.Exists(file))
                return;

            var text = File.ReadAllText(file, System.Text.Encoding.UTF8);
            File.Delete(file);

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
            var resultDir = Path.GetFullPath(kResultDir);
            Debug.Log(kHeader, $"Start to watch .json in {resultDir}");
            if (!Directory.Exists(resultDir))
                Directory.CreateDirectory(resultDir);

            foreach (var file in Directory.GetFiles(resultDir, "*.json"))
                EditorApplication.delayCall += () => OnResultCreated(file);

            var watcher = new FileSystemWatcher()
            {
                Path = resultDir,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };

            watcher.Created += (s, e) =>
            {
                EditorApplication.delayCall += () => OnResultCreated(e.Name);
            };
        }
    }
}
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.