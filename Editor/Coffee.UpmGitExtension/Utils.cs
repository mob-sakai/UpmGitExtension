#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using UnityEditor;

namespace Coffee.UpmGitExtension
{
    public static class Debug
    {
        [Conditional("UGE_LOG")]
        static void Log(string header, string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(header + format, args);
        }

        [Conditional("UGE_LOG")]
        public static void Log(string header, object message)
        {
            UnityEngine.Debug.Log(header + message);
        }

        [Conditional("UGE_LOG")]
        static void Warning(string header, string format, params object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(header + format, args);
        }

        [Conditional("UGE_LOG")]
        public static void Warning(string header, object message)
        {
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

    public static class ReflectionExtensions
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static object Inst(this object self)
        {
            return (self is Type) ? null : self;
        }

        static Type Type(this object self)
        {
            return (self as Type) ?? self.GetType();
        }

        public static object New(this Type self, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetConstructor(types)
                .Invoke(args);
        }

        public static object Call(this object self, string methodName, params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            return self.Type().GetMethod(methodName, types)
                .Invoke(self.Inst(), args);
        }

        public static object Call(this object self, Type[] genericTypes, string methodName, params object[] args)
        {
            return self.Type().GetMethod(methodName, FLAGS)
                .MakeGenericMethod(genericTypes)
                .Invoke(self.Inst(), args);
        }

        public static object Get(this object self, string memberName, MemberInfo mi = null)
        {
            mi = mi ?? self.Type().GetMember(memberName, FLAGS)[0];
            return mi is PropertyInfo
                ? (mi as PropertyInfo).GetValue(self.Inst(), new object[0])
                : (mi as FieldInfo).GetValue(self.Inst());
        }

        public static void Set(this object self, string memberName, object value, MemberInfo mi = null)
        {
            mi = mi ?? self.Type().GetMember(memberName, FLAGS)[0];
            if (mi is PropertyInfo)
                (mi as PropertyInfo).SetValue(self.Inst(), value, new object[0]);
            else
                (mi as FieldInfo).SetValue(self.Inst(), value);
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
            Debug.Log(kHeader, $"[PackageUtils.InstallPackage] packageName = {packageName}, repoUrl = {repoUrl}, refName = {refName}");
            UpdateJson("Packages/manifest.json", jsonDic =>
            {
                // Add to dependencies.
                var dependencies = jsonDic["dependencies"] as Dictionary<string, object>;
                dependencies?.Add(packageName, repoUrl + "#" + refName);
            });
        }

        /// <summary>
        /// Uninstall package
        /// </summary>
        /// <param name="packageName">Package name</param>
        public static void UninstallPackage(string packageName)
        {
            Debug.Log(kHeader, $"[PackageUtils.UninstallPackage] packageName = {packageName}");
            UpdateJson("Packages/manifest.json", jsonDic =>
            {
                // Remove from dependencies.
                var dependencies = jsonDic["dependencies"] as Dictionary<string, object>;
                dependencies?.Remove(packageName);

                // Unlock git revision.
                if (jsonDic.TryGetValue("lock", out var locks))
                    (locks as Dictionary<string, object>)?.Remove(packageName);
            });

            UpdateJson("Packages/packages-lock.json", jsonDic =>
            {
                // Unlock git revision.
                var dependencies = jsonDic["dependencies"] as Dictionary<string, object>;
                dependencies?.Remove(packageName);
            });
        }

        /// <summary>
        /// Update json file
        /// </summary>
        /// <param name="path">Json file path</param>
        /// <param name="action">Action for json dictionary.</param>
        static void UpdateJson(string path, Action<Dictionary<string, object>> action)
        {
            Debug.Log(kHeader, "[PackageUtils.UpdateJson] : " + path);
            if (!File.Exists(path)) return;

            try
            {
                var jsonDic = Json.Deserialize(File.ReadAllText(path)) as Dictionary<string, object>;

                if (jsonDic != null && action != null)
                    action(jsonDic);

                // Save manifest.json.
                File.WriteAllText(path, Json.Serialize(jsonDic, true));

                EditorApplication.delayCall += () =>
                {
#if UNITY_2020_2_OR_NEWER
                    UnityEditor.PackageManager.Client.Resolve();
#else
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
#endif
                };
            }
            catch (Exception e)
            {
                Debug.Exception(kHeader, e);
            }
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
                repoUrl = Regex.Replace(repoUrl, "^git\\+", "");
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
            refName = m.Groups[4].Value;
        }
    }

    public static class VisualElementExtension
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
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
