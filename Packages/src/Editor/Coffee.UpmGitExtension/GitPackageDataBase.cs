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

#if UNITY_6000_0_OR_NEWER
        public UpmPackageVersion[] versions;
#else
        public UpmPackageVersionEx[] versions;
#endif

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_6000_0_OR_NEWER
            versions = versions.ToArray();
#else
            versions = versions
                .Where(v => v.isValid)
                .ToArray();
#endif
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

    internal class GitPackageDatabase : ScriptableSingleton<GitPackageDatabase>
    {
        private static string _workingDirectory => InternalEditorUtility.unityPreferencesFolder + "/GitPackageDatabase";
        private static string _serializeVersion => "2.0.2";
        private static string _resultsDir => _workingDirectory + "/Results-" + _serializeVersion;
        private static FileSystemWatcher _watcher;
        private static bool _isPaused;
        private static readonly HashSet<FetchResult> _resultCaches = new HashSet<FetchResult>();

        private static PackageManagerProjectSettings _settings =>
            ScriptableSingleton<PackageManagerProjectSettings>.instance;

#if UNITY_2020_1
        internal static IUpmClient _upmClient => UpmClient.instance;
        internal static IPackageDatabase _packageDatabase => PackageDatabase.instance;
#else
        internal static UpmClient _upmClient => ScriptableSingleton<ServicesContainer>.instance.Resolve<UpmClient>();
        internal static PackageDatabase _packageDatabase =>
            ScriptableSingleton<ServicesContainer>.instance.Resolve<PackageDatabase>();
#endif

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

#if UNITY_6000_0_OR_NEWER
        internal static List<UpmPackageVersion> GetPackageVersion(string packageName, string versionUniqueId)
        {
            var result = _resultCaches
                .SelectMany(r => r.versions)
                .Where(v => v.name == packageName).ToList();

            return result;
        }
#else
        internal static IPackageVersion GetPackageVersion(string packageUniqueId, string versionUniqueId)
        {
            IPackage package;
            IPackageVersion version;
            _packageDatabase.GetPackageAndVersion(packageUniqueId, versionUniqueId, out package, out version);
            return version;
        }
#endif

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
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }

            Debug.Log("[GitPackageDatabase] Clear Cache");
            WatchResultJson();
        }

        public static void ResetCacheTime()
        {
            _isPaused = true;
            var resultDir = Path.GetFullPath(_resultsDir);
            if (Directory.Exists(resultDir))
            {
                foreach (var file in Directory.GetFiles(resultDir, "*.json"))
                {
                    File.SetLastWriteTime(file, DateTime.Now.AddMinutes(-10));
                }
            }

            _isPaused = false;
        }

#if UNITY_6000_0_OR_NEWER
        public static IEnumerable<UpmPackageVersion> GetAvailablePackageVersions(string repoUrl = null)
        {
            var result = _resultCaches
                .SelectMany(r => r.versions)
                .Where(v => string.IsNullOrEmpty(repoUrl) || v.uniqueId.Contains(repoUrl));

            return result;
        }
#else
        public static IEnumerable<UpmPackageVersionEx> GetAvailablePackageVersions(string repoUrl = null)
        {
            return _resultCaches.SelectMany(r => r.versions)
                .Where(v => v.isValid && (string.IsNullOrEmpty(repoUrl) || v.uniqueId.Contains(repoUrl)));
        }
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
                    if (!isInstalled)
                    {
                        return null;
                    }

                    // Git mode: Register all installable package versions.

                    var upmPackage = _packageDatabase.GetPackage(versions.Key) as UpmPackage;
                    var installedVersion = upmPackage?.versions.installed as UpmPackageVersion;
                    if (installedVersion.GetPackageInfo().source != PackageSource.Git)
                    {
                        return upmPackage;
                    }

                    // Unlock.
                    installedVersion.UnlockVersion();

#if UNITY_6000_0_OR_NEWER
                    upmPackage = upmPackage.UpdateVersionsSafety();
#else
                    var newVersions = new[] { new UpmPackageVersionEx(installedVersion) }
                        .Concat(versions.Where(v => v.uniqueId != installedVersion.uniqueId))
                        .OrderBy(v => v.semVersion)
                        .ThenBy(v => v.isInstalled)
                        .ToArray();
                    upmPackage = upmPackage.UpdateVersionsSafety(newVersions);
#endif

                    return upmPackage;
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
            if (_isPaused || string.IsNullOrEmpty(file) || Path.GetExtension(file) != ".json" || !File.Exists(file))
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

#if UNITY_6000_3_OR_NEWER
            var addOp = _upmClient.Get("addAndRemoveOperation") as UpmAddAndRemoveOperation;
            if (addOp != null)
            {
                addOp.onOperationFinalized += _ =>
                {
                    RequestUpdateGitPackageVersions();
                };
            }
#else
            _upmClient.onAddOperation += op => op.onOperationFinalized += _ => RequestUpdateGitPackageVersions();
#endif
        }

        public static string GetShortPackageId(UpmPackageVersion self)
        {
            var semver = self.versionString;
            var revision = ExtractGitRevision(self.uniqueId);

            return !string.IsNullOrEmpty(revision) && !revision.Contains(semver)
                ? $"{self.name}/{revision} ({semver})"
                : $"{self.name}/{semver}";
        }

        public static string GetShortVersion(UpmPackageVersion self)
        {
            var semver = self.versionString;
            var revision = ExtractGitRevision(self.uniqueId);

            return !string.IsNullOrEmpty(revision) && !revision.Contains(semver)
                ? $"{revision} ({semver})"
                : $"{semver}";
        }

        private static string ExtractGitRevision(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                return null;
            }

            var hashIndex = uniqueId.LastIndexOf('#');
            if (hashIndex < 0 || hashIndex == uniqueId.Length - 1)
            {
                return null;
            }

            return uniqueId.Substring(hashIndex + 1);
        }
    }
}
