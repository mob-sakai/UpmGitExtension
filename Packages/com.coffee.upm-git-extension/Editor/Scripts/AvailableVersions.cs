#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
// using UnityEditor.PackageManager.UI.InternalBridge;
// using Debug = UnityEditor.PackageManager.UI.InternalBridge.Debug;


namespace Coffee.PackageManager.UI
{
    [Serializable]
    public class AvailableVersion : IEquatable<AvailableVersion>, ISerializationCallbackReceiver
    {
        public string packageName = "";
        public string version = "";
        public string refName = "";
        public string repoUrl = "";

        public string refNameText { get { return version == refName ? version : version + " - " + refName; } }

        bool IEquatable<AvailableVersion>.Equals(AvailableVersion other)
        {
            return other != null
                && packageName == other.packageName
                && version == other.version
                && repoUrl == other.repoUrl
                && refName == other.refName;
        }

        public override int GetHashCode()
        {
            return packageName.GetHashCode()
                + version.GetHashCode()
                + repoUrl.GetHashCode()
                + refName.GetHashCode();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            repoUrl = PackageUtils.GetRepoUrl(repoUrl);
        }
    }

    public class AvailableVersions : ScriptableSingleton<AvailableVersions>
    {
        const string kResultDir = "Library/UpmGitExtension/results";
        const string kHeader = "<b><color=#c7634c>[AvailableVersions]</color></b> ";
        const string kGetVersionsJs = "Packages/com.coffee.upm-git-extension/Editor/Commands/get-available-versions.js";

        public AvailableVersion[] versions = new AvailableVersion[0];

        public static event Action OnChanged = () => { };

        [Serializable]
        public class ResultInfo
        {
            public AvailableVersion[] versions;
        }

        public static void ClearAll()
        {
            instance.versions = new AvailableVersion[0];
        }

        public static void Clear(string packageName = null, string repoUrl = null)
        {
            repoUrl = string.IsNullOrEmpty(repoUrl) ? repoUrl : PackageUtils.GetRepoUrl(repoUrl);
            instance.versions = instance.versions
                .Where(x => string.IsNullOrEmpty(packageName) || x.packageName != packageName)
                .Where(x => string.IsNullOrEmpty(repoUrl) || x.repoUrl != repoUrl)
                .ToArray();
        }

        public static IEnumerable<AvailableVersion> GetVersions(string packageName = null, string repoUrl = null)
        {
            repoUrl = string.IsNullOrEmpty(repoUrl) ? repoUrl : PackageUtils.GetRepoUrl(repoUrl);
            return instance.versions
                .Where(x => string.IsNullOrEmpty(packageName) || x.packageName == packageName)
                .Where(x => string.IsNullOrEmpty(repoUrl) || x.repoUrl == repoUrl);
        }

        public static void AddVersions(IEnumerable<AvailableVersion> add)
        {
            if (add == null || !add.Any())
                return;

            var length = instance.versions.Length;
            var versions = instance.versions
                .Union(add)
                .ToArray();

            if (versions.Length != length)
            {
                instance.versions = versions;
                OnChanged();
            }
        }

        public static void UpdateAvailableVersions(string packageName = "*", string repoUrl = "", EventHandler callback = null)
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
            Debug.Log(kHeader, "{0} {1}", psi.FileName, psi.Arguments);
            new UnityEditor.Utils.Program(psi).Start(callback);
        }

        static void OnResultCreated(object o, FileSystemEventArgs e)
        {
            if (Path.GetExtension(e.Name) == ".json")
                OnResultCreated(e.Name);
        }

        static void OnResultCreated(string file)
        {
            try
            {
                var info = JsonUtility.FromJson<ResultInfo>(File.ReadAllText(file));
                EditorApplication.delayCall += () => AvailableVersions.AddVersions(info.versions);
            }
            finally
            {
                File.Delete(file);
            }
        }


        [InitializeOnLoadMethod]
        static void WatchResultJson()
        {
#if !UNITY_EDITOR_WIN
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
            if (!Directory.Exists(kResultDir))
                Directory.CreateDirectory(kResultDir);

            foreach (var file in Directory.GetFiles(kResultDir, "*.json"))
                OnResultCreated(file);

            var watcher = new FileSystemWatcher()
            {
                Path = kResultDir,
                NotifyFilter = NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };

            watcher.Created += OnResultCreated;
        }
    }
}
#endif // This line is added by Open Sesame Portable. DO NOT remov manually.