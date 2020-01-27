#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.
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
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace Coffee.PackageManager.UI
{
    public static class Debug
    {
        static bool logEnabled = false;
        static Debug()
        {
            logEnabled = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';', ',')
                .Any(x => x == "UGE_LOG");
        }

        static void Log(string header, string format, params object[] args)
        {
            if (logEnabled)
                UnityEngine.Debug.LogFormat(header + format, args);
        }

        public static void Log(string header, object message)
        {
            if (logEnabled)
                UnityEngine.Debug.Log(header + message);
        }

        static void Warning(string header, string format, params object[] args)
        {
            if (logEnabled)
                UnityEngine.Debug.LogWarningFormat(header + format, args);
        }

        public static void Warning(string header, object message)
        {
            if (logEnabled)
                UnityEngine.Debug.LogWarning(header + message);
        }

        public static void Error(string header, string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(header + format, args);
        }

        public static void Error(string header, object message)
        {
            UnityEngine.Debug.LogError(header + message);
        }

        public static void Exception(string header, Exception e)
        {
            UnityEngine.Debug.LogException(new Exception(header + e.Message, e.InnerException));
        }
    }

    public static class PackageUtils
    {
        const string kHeader = "<b><color=#c7634c>[PackageUtils]</color></b> ";
        static readonly Regex REG_PACKAGE_ID = new Regex("^([^@]+)@([^#]+)(#(.+))?$", RegexOptions.Compiled);

        /// <summary>
        /// Install or update package
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="repoUrl">Package url for install</param>
        /// <param name="refName">Reference name for install (option)</param>
        public static void InstallPackage(string packageName, string repoUrl, string refName)
        {
            Debug.Log(kHeader, "[PackageUtils.InstallPackage] packageName = {0}, repoUrl = {1}, refName = {2}", packageName, repoUrl, refName);
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
            Debug.Log(kHeader, "[PackageUtils.UninstallPackage] packageName = {0}", packageName);
            UpdateManifestJson(dependencies => dependencies.Remove(packageName));
        }

        /// <summary>
        /// Update manifest.json
        /// </summary>
        /// <param name="actionForDependencies">Action for dependencies</param>
        static void UpdateManifestJson(Action<Dictionary<string, object>> actionForDependencies)
        {
            Debug.Log(kHeader, "[PackageUtils.UpdateManifestJson]");
            const string manifestPath = "Packages/manifest.json";
            var manifest = Json.Deserialize(File.ReadAllText(manifestPath)) as Dictionary<string, object>;
            actionForDependencies(manifest["dependencies"] as Dictionary<string, object>);

            // Save manifest.json.
            File.WriteAllText(manifestPath, Json.Serialize(manifest));
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
            if (string.IsNullOrEmpty(packageId))
                return "";

            Match m = REG_PACKAGE_ID.Match(packageId);
            if (!m.Success)
                return "";

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

        public static VisualElement GetRoot(this VisualElement element)
        {
            while (element != null && element.parent != null)
            {
                element = element.parent;
            }
            return element;
        }
    }
}

#endif // This line is added by Open Sesame Portable. DO NOT remov manually.