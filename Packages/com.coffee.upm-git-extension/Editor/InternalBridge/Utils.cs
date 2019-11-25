#if UNITY_2019_1_9 || UNITY_2019_1_10 || UNITY_2019_1_11 || UNITY_2019_1_12 || UNITY_2019_1_13 || UNITY_2019_1_14 || UNITY_2019_2_OR_NEWER
#define UNITY_2019_1_9_OR_NEWER
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace UnityEditor.PackageManager.UI.InternalBridge
{
    public class Debug
    {
        [Conditional("DEBUG")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("DEBUG")]
        public static void LogFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }

        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }

        public static void LogException(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
    }


    public static class UIUtils
    {
        const string kDisplayNone = "display-none";
        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;

            SetElementClass(element, kDisplayNone, !value);
            element.visible = value;
        }

        public static bool IsElementDisplay(VisualElement element)
        {
            return !HasElementClass(element, kDisplayNone);
        }

        public static void SetElementClass(VisualElement element, string className, bool value)
        {
            if (element == null)
                return;

            if (value)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        public static bool HasElementClass(VisualElement element, string className)
        {
            if (element == null)
                return false;

            return element.ClassListContains(className);
        }

        public static VisualElement GetRoot(VisualElement element)
        {
            while (element != null && element.parent != null)
            {
                element = element.parent;
            }
            return element;
        }
    }

    public static class GitUtils
    {
        static readonly Regex REG_REFS = new Regex("refs/(tags|remotes/origin)/([^/]+),(.+),([^\\s\\r\\n]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly string GET_REFS_SCRIPT = "Packages/com.coffee.upm-git-extension/Editor/Commands/get-available-refs";

        /// <summary>
        /// Fetch the all branch/tag names where the package can be installed from the repository.
        ///   - package.json and package.json.meta exist in root
        ///   - Support current unity verison
        ///   - Branch name is not nested
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="repoUrl">Repo url</param>
        /// <param name="callback">Callback. The argument is "[tagOrBranchName],[version],[packageName]"</param>
        public static void GetRefs(string packageName, string repoUrl, Action<IEnumerable<string>> callback)
        {
            Debug.LogFormat("[GitUtils.GetRefs] Start get refs: {0}, {1}", packageName, repoUrl);
            if (string.IsNullOrEmpty(repoUrl))
            {
                callback(Enumerable.Empty<string>());
                return;
            }

            var appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cacheRoot = new DirectoryInfo(Path.Combine(appDir, "UpmGitExtension"));
            var cacheDir = new DirectoryInfo(Path.Combine(cacheRoot.FullName, Uri.EscapeDataString(packageName + "@" + repoUrl)));
            var cacheFile = new FileInfo(Path.Combine(cacheDir.FullName, "versions"));

            // Results are cached for 5 minutes.
            if (cacheFile.Exists && (DateTime.Now - cacheFile.LastWriteTime).TotalMinutes < 5)
            {
                Debug.LogFormat("[GitUtils.GetRefs] Refs has been cached: {0}", cacheFile);
                var versions = REG_REFS.Matches(File.ReadAllText(cacheFile.FullName))
                    .Cast<Match>()
                    .Where(m => packageName.Length == 0 || packageName == m.Groups[4].Value)
                    .Select(m => string.Format("{0},{1},{2}", m.Groups[2], m.Groups[3], m.Groups[4]));
                callback(versions);
            }
            // Run script and cache result.
            else
            {
                string execute, args;
                var unity = Application.unityVersion;
                var dir = cacheDir.FullName;

                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    execute = "cmd.exe";
                    args = string.Format("/C \"\"{0}.bat\" \"{1}\" \"{2}\" \"{3}\"\"", Path.GetFullPath(GET_REFS_SCRIPT), repoUrl, dir, unity);
                }
                else
                {
                    execute = "/bin/sh";
                    args = string.Format("\"{0}.sh\" \"{1}\" \"{2}\" \"{3}\"", Path.GetFullPath(GET_REFS_SCRIPT), repoUrl, dir, unity);
                }

                ExecuteShell(execute, args, (success) =>
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
            Debug.LogFormat("[GitUtils.ExecuteShell] script = {0}, args = {1}", script, args);
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                FileName = script,
                UseShellExecute = false,
            };

            var launchProcess = System.Diagnostics.Process.Start(startInfo);
            if (launchProcess == null || launchProcess.HasExited == true || launchProcess.Id == 0)
            {
                Debug.LogErrorFormat("[GitUtils.ExecuteShell] failed: script = {0}, args = {1}", script, args);
                callback(false);
            }
            else
            {
                //Add process callback.
                launchProcess.Exited += (sender, e) =>
                {
                    Debug.LogFormat("[GitUtils.ExecuteShell] exit {0}", launchProcess.ExitCode);
                    bool success = 0 == launchProcess.ExitCode;
                    if (!success)
                    {
                        Debug.LogErrorFormat("[Utils.ExecuteShell] failed: script = {0}, args = {1}", script, args);
                    }
                    callback(success);
                };

                launchProcess.EnableRaisingEvents = true;
            }
        }

    }

    public static class JsonUtils
    {
        public static Dictionary<string, object> DeserializeFile(string file)
        {
            var text = File.ReadAllText(file);
#if UNITY_2019_1_9_OR_NEWER
			return Json.Deserialize(text) as Dictionary<string, object>;
#else
            return Expose.FromType(Type.GetType("UnityEditor.Json, UnityEditor")).Call("Deserialize", text).As<Dictionary<string, object>>();
#endif
        }

        public static void SerializeFile(string file, Dictionary<string, object> json)
        {
#if UNITY_2019_1_9_OR_NEWER
			var text = Json.Serialize(json);
#elif UNITY_2019_1_OR_NEWER
            var text = Expose.FromType(Type.GetType("UnityEditor.Json, UnityEditor")).Call("Serialize", json, false, "  ").As<string>();
#else
            var text = Expose.FromType(Type.GetType("UnityEditor.Json, UnityEditor")).Call("Serialize", json).As<string>();
#endif
			File.WriteAllText(file, text);
        }
    }

    public static class PackageUtils
    {
        static readonly Regex REG_PACKAGE_ID = new Regex("^([^@]+)@([^#]+)(#(.+))?$", RegexOptions.Compiled);

        /// <summary>
        /// Install or update package
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="repoUrl">Package url for install</param>
        /// <param name="refName">Reference name for install (option)</param>
        public static void InstallPackage(string packageName, string repoUrl, string refName)
        {
            Debug.LogFormat("[PackageUtils.InstallPackage] packageName = {0}, repoUrl = {1}, refName = {2}", packageName, repoUrl, refName);
            UpdateManifestJson(dependencies =>
            {
                // Remove from dependencies.
                dependencies.Remove(packageName);
                if (!string.IsNullOrEmpty(repoUrl))
                {
                    // Add to dependencies.
                    if (!string.IsNullOrEmpty(refName))
                        dependencies.Add(packageName, repoUrl + "#" + refName);
                    else
                        dependencies.Add(packageName, repoUrl);
                }
            });
        }

        /// <summary>
        /// Uninstall package
        /// </summary>
        /// <param name="packageName">Package name</param>
        public static void UninstallPackage(string packageName)
        {
            Debug.LogFormat("[PackageUtils.UninstallPackage] packageName = {0}", packageName);
            UpdateManifestJson(dependencies => dependencies.Remove(packageName));
        }

        /// <summary>
        /// Update manifest.json
        /// </summary>
        /// <param name="actionForDependencies">Action for dependencies</param>
        static void UpdateManifestJson(Action<Dictionary<string, object>> actionForDependencies)
        {
            Debug.LogFormat("[PackageUtils.UpdateManifestJson]");
            const string manifestPath = "Packages/manifest.json";
            var manifest = JsonUtils.DeserializeFile(manifestPath);
            actionForDependencies(manifest["dependencies"] as Dictionary<string, object>);

            // Save manifest.json.
            JsonUtils.SerializeFile(manifestPath, manifest);
            EditorApplication.delayCall += () => AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Get a file path in package directory with pattern.
        /// </summary>
        /// <param name="dir">Directory path</param>
        /// <param name="pattern">Pattern</param>
        /// <returns>If the file exists, return the file path. Otherwise, return empty.</returns>
        public static string GetFilePathWithPattern(string dir, string pattern)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(pattern))
                return "";

            return Directory.GetFiles(dir, pattern)
                .FirstOrDefault(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                ?? "";
        }

        /// <summary>
        /// Get repo url for package from package id.
        /// </summary>
        /// <param name="packageId">Package id([packageName]@[repoUrl]#[ref])</param>
        /// <param name="asHttps">Convert repo url to https url</param>
        /// <returns>Repo url</returns>
        public static string GetRepoUrl(string packageId, bool asHttps = false)
        {
            Match m = REG_PACKAGE_ID.Match(packageId);
            if (m.Success)
            {
                var repoUrl = m.Groups[2].Value;
                if (asHttps)
                {
                    repoUrl = Regex.Replace(repoUrl, "(git:)?git@([^:]+):", "https://$2/");
                    repoUrl = repoUrl.Replace("ssh://", "https://");
                    repoUrl = repoUrl.Replace("git@", "");
                    repoUrl = Regex.Replace(repoUrl, "\\.git$", "");
                }
                return repoUrl;
            }
            return "";
        }

        public static void SplitPackageId(string packageId, out string packageName, out string repoUrl, out string refName)
        {
            Match m = REG_PACKAGE_ID.Match(packageId);
            packageName = m.Groups[1].Value;
            repoUrl = m.Groups[2].Value;
            refName = m.Groups[4].Value
                ;
        }
    }

    public static class ButtonExtension
    {
        public static void OverwriteCallback(this Button button, Action action)
        {
            button.RemoveManipulator(button.clickable);
            button.clickable = new Clickable(action);
            button.AddManipulator(button.clickable);
        }
    }
}
