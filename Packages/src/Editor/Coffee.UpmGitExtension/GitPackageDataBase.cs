using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;

#else
using UnityEditor.PackageManager.UI;
#endif

#if UNITY_2023_1_OR_NEWER
using UpmPackage = UnityEditor.PackageManager.UI.Internal.Package;
#endif

namespace Coffee.UpmGitExtension
{
    [Serializable]
    internal class FetchResult : ISerializationCallbackReceiver
    {
        public string id;
        public string url;
        public int hash;
        public UpmPackageVersionEx[] versions;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            versions = versions
                .Where(v => v.isValid)
                .ToArray();
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public override bool Equals(object obj)
        {
            return (obj as FetchResult)?.hash == hash;
        }
    }

    /// <summary>
    /// Database of packages installed via Git
    /// </summary>
    //[FilePath("GitPackageDatabase.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal class GitPackageDatabase : ScriptableSingleton<GitPackageDatabase>
    {
        //################################
        // Public Members.
        //################################
        // public static event Action OnChangedPackages;

        public static void Install(string packageId)
        {
            _upmClient.AddByUrl(packageId);
        }

        public static void Uninstall(string packageId)
        {
            var i = packageId.IndexOf('@');
            var packageName = packageId.Substring(0, i);
            _upmClient.RemoveByName(packageName);
        }

        public static IEnumerable<UpmPackage> GetUpmPackages()
        {
            return _packageDatabase.allPackages
                .OfType<UpmPackage>()
                .Where(x => x.versions.primary.HasTag(PackageTag.Git));
        }

        public static IEnumerable<UpmPackage> GetInstalledGitPackages()
        {
            return GetUpmPackages()
                .Where(p => p.GetInstalledVersion()?.HasTag(PackageTag.Git) == true);
        }

        /// <summary>
        /// Fetch the available git package versions.
        /// </summary>
        public static void Fetch(string url, Action<int> callback = null)
        {
            const string kFetchPackagesJs = "Packages/com.coffee.upm-git-extension/Editor/Commands/fetch-packages.js";
            NodeJs.Run(_workingDirectory, Path.GetFullPath(kFetchPackagesJs), url, code =>
            {
                if (code == 0)
                {
                    GitRepositoryUrlList.AddUrl(url);
                }

                callback?.Invoke(code);
            });
        }

        internal static IPackage GetPackage(IPackageVersion packageVersion)
        {
            return _packageDatabase.GetPackage(packageVersion.name);
        }

        internal static IPackage GetPackage(string packageName)
        {
            return _packageDatabase.GetPackage(packageName);
        }

        internal static IPackageVersion GetPackageVersion(string packageUniqueId, string versionUniqueId)
        {
            IPackage package;
            IPackageVersion version;
            _packageDatabase.GetPackageAndVersion(packageUniqueId, versionUniqueId, out package, out version);
            return version;
        }

        /// <summary>
        /// Update available versions for git packages.
        /// </summary>
        public static void Fetch()
        {
            GetInstalledGitPackages()
                .Select(p => p?.versions?.primary?.GetPackageInfo()?.GetSourceUrl())
                .Where(url => !string.IsNullOrEmpty(url))
                .Distinct()
                .ForEach(url => Fetch(url));
        }

        public static void OpenCacheDirectory()
        {
            if (Directory.Exists(_workingDirectory))
            {
                EditorUtility.RevealInFinder(_workingDirectory);
            }
        }

        public static void ClearCache()
        {
            _resultCaches.Clear();

            if (Directory.Exists(_workingDirectory))
            {
                foreach (var dir in Directory.GetDirectories(_workingDirectory))
                {
                    Directory.Delete(dir, true);
                }
            }

            Debug.Log("[GitPackageDatabase] Clear Cache");
            WatchResultJson();
        }

        public static void ResetCacheTime()
        {
            isPaused = true;
            var resultDir = Path.GetFullPath(_resultsDir);
            if (Directory.Exists(resultDir))
            {
                foreach (var file in Directory.GetFiles(resultDir, "*.json"))
                {
                    File.SetLastWriteTime(file, DateTime.Now.AddMinutes(-10));
                }
            }

            isPaused = false;
        }

        public static IEnumerable<UpmPackageVersionEx> GetAvailablePackageVersions(string repoUrl = null)
        {
            return _resultCaches
                .SelectMany(r => r.versions)
                .Where(v => v.isValid && (string.IsNullOrEmpty(repoUrl) || v.uniqueId.Contains(repoUrl)));
        }

        //################################
        // Private Members.
        //################################
        private static string _workingDirectory => InternalEditorUtility.unityPreferencesFolder + "/GitPackageDatabase";
        private static string _serializeVersion => "2.0.2";
        private static string _resultsDir => _workingDirectory + "/Results-" + _serializeVersion;
        private static FileSystemWatcher _watcher;
        private static bool isPaused;
        private static readonly HashSet<FetchResult> _resultCaches = new HashSet<FetchResult>();
        private static PackageManagerProjectSettings _settings =>
            ScriptableSingleton<PackageManagerProjectSettings>.instance;
#if UNITY_2020_2_OR_NEWER
        internal static UpmClient _upmClient => ScriptableSingleton<ServicesContainer>.instance.Resolve<UpmClient>();
        internal static PackageDatabase _packageDatabase =>
            ScriptableSingleton<ServicesContainer>.instance.Resolve<PackageDatabase>();
#else
        internal static IUpmClient _upmClient => UpmClient.instance;
        internal static IPackageDatabase _packageDatabase => PackageDatabase.instance;
#endif

#if UNITY_2021_1_OR_NEWER
        private static bool _enablePreReleasePackages => _settings.enablePreReleasePackages;
#else
        private static bool _enablePreReleasePackages => _settings.enablePreviewPackages;
#endif

        public static void RequestUpdateGitPackageVersions()
        {
            EditorApplication.delayCall -= UpdateGitPackageVersions;
            EditorApplication.delayCall += UpdateGitPackageVersions;
        }

        private static void UpdateGitPackageVersions()
        {
            var installedIds = new HashSet<string>(
                GetUpmPackages()
                    .Where(p => p.GetInstalledVersion() != null)
                    .Select(p => p.name)
            );

            var packages = GetAvailablePackageVersions()
                .ToLookup(v => v.name)
                .Select(versions =>
                {
                    var isInstalled = installedIds.Contains(versions.Key);
                    if (!isInstalled) return null;

                    {
                        // Git mode: Register all installable package versions.
                        var upmPackage = _packageDatabase.GetPackage(versions.Key) as UpmPackage;
                        var installedVersion = upmPackage.versions.installed as UpmPackageVersion;
                        if (installedVersion.GetPackageInfo().source != PackageSource.Git)
                        {
                            return upmPackage;
                        }

                        // Unlock.
                        installedVersion.UnlockVersion();

                        var newVersions = new[] { new UpmPackageVersionEx(installedVersion) }
                            .Concat(versions.Where(v => v.uniqueId != installedVersion.uniqueId))
                            .OrderBy(v => v.semVersion)
                            .ThenBy(v => v.isInstalled)
                            .ToArray();
                        upmPackage = upmPackage.UpdateVersionsSafety(newVersions);

                        return upmPackage;
                    }
                })
                .Where(p => p != null);

            EditorApplication.delayCall += () => UpdatePackages(packages);

#if UNITY_2021_1_OR_NEWER
            if (!_settings.seeAllPackageVersions)
            {
                _settings.seeAllPackageVersions = true;
                _settings.Save();
            }
#endif
        }

        private static void UpdatePackages(IEnumerable<IPackage> packages)
        {
#if UNITY_2023_1_OR_NEWER
            _packageDatabase.UpdatePackages(packages.ToList());
#else
            _packageDatabase.Call("OnPackagesChanged", packages);
#endif
        }

        private static void OnResultFileCreated(string file)
        {
            if (isPaused || string.IsNullOrEmpty(file) || Path.GetExtension(file) != ".json" || !File.Exists(file))
            {
                return;
            }

            try
            {
                var text = File.ReadAllText(file, Encoding.UTF8);
                var result = JsonUtility.FromJson<FetchResult>(text);

                _resultCaches.RemoveWhere(r => r.url == result.url);
                _resultCaches.Add(result);
                RequestUpdateGitPackageVersions();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [InitializeOnLoadMethod]
        private static void WatchResultJson()
        {
            _resultCaches.Clear();

#if !UNITY_EDITOR_WIN
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
            var resultDir = Path.GetFullPath(_resultsDir);
            if (!Directory.Exists(resultDir))
            {
                Directory.CreateDirectory(resultDir);
            }

            RequestUpdateGitPackageVersions();
            foreach (var file in Directory.GetFiles(resultDir, "*.json"))
            {
                EditorApplication.delayCall += () => OnResultFileCreated(Path.Combine(resultDir, file));
            }

            _watcher?.Dispose();
            _watcher = new FileSystemWatcher
            {
                Path = resultDir,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += (s, e) => EditorApplication.delayCall += () => OnResultFileCreated(e.FullPath);

            _upmClient.onAddOperation += op => op.onOperationFinalized += _ => RequestUpdateGitPackageVersions();
        }
    }
}
