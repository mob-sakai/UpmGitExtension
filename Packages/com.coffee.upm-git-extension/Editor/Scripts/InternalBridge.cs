#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.
#if UNITY_2019_1_9 || UNITY_2019_1_10 || UNITY_2019_1_11 || UNITY_2019_1_12 || UNITY_2019_1_13 || UNITY_2019_1_14 || UNITY_2019_2_OR_NEWER
#define UNITY_2019_1_9_OR_NEWER
#endif
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
#if !UNITY_2019_1_9_OR_NEWER
using Semver;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
#if !UNITY_2019_3_OR_NEWER
using Package = UnityEditor.PackageManager.UI.Package;
using PackageInfo = UnityEditor.PackageManager.UI.PackageInfo;
#endif

namespace Coffee.PackageManager.UI
{
    public class Bridge
    {
        const string kHeader = "<b><color=#c7634c>[Bridge]</color></b> ";

        private static Bridge instance = new Bridge();
        public static Bridge Instance { get { return instance; } }

        LoadingSpinner loadingSpinner;
        PackageList packageList;
        PackageDetails packageDetails;

        bool reloading;

        private Bridge() { }

        public void Setup(VisualElement root)
        {
            loadingSpinner = root.Q<LoadingSpinner>();
            packageList = root.Q<PackageList>();
            packageDetails = root.Q<PackageDetails>();
            Debug.Log(kHeader, "[Setup] {0}, {1}, {2},", loadingSpinner, packageList, packageDetails);

#if UNITY_2019_3_OR_NEWER
            packageList.onPackageListLoaded -= UpdateGitPackages;
            packageList.onPackageListLoaded += UpdateGitPackages;

            // PackageDatabase.instance.onPackagesChanged += (added, removed, _, updated) =>
            // {
            //     // Removed or updated.
            //     if (removed.Concat(updated).Any(x => x?.installedVersion?.packageInfo?.source == PackageSource.Git))
            //     {
            //         EditorApplication.delayCall += UpdatePackageCollection;
            //     }

            //     // Installed with git
            //     if (added.Concat(updated).Any(x => x?.installedVersion?.packageInfo?.source == PackageSource.Git))
            //     {
            //         EditorApplication.delayCall += UpdateGitPackages;
            //     }
            // };
#else
            packageList.OnLoaded -= UpdateGitPackageVersions;
            packageList.OnLoaded += UpdateGitPackageVersions;
#endif
            AvailableVersions.OnChanged += UpdateGitPackageVersions;
        }

        /// <summary>
        /// Update available versions for git packages.
        /// </summary>
        public void UpdateAvailableVersionsForGitPackages()
        {
            // Start update task.
            foreach (var package in PackageExtensions.GetGitPackages())
            {
                var pInfo = package.GetInstalledVersion();
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.PackageId);
                AvailableVersions.UpdateAvailableVersions(pInfo.Name, repoUrl);
            }
        }

        [MenuItem("UpmGitExtensions/UpdateGitPackageVersions")]
        /// <summary>
        /// Update all infomations of git packages.
        /// </summary>
        static void UpdateGitPackageVersions()
        {
            bool changed = false;
            // Start update task.
            foreach (var package in PackageExtensions.GetGitPackages())
            {
                var pInfo = package.GetInstalledVersion();
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.PackageId);
                var versions = AvailableVersions.GetVersions(package.Name, repoUrl);
                changed = UpdatePackageVersions(package, versions) | changed;
            }

            if (changed)
                UpdatePackageCollection();
        }

        /// <summary>
        /// Update package info.
        /// </summary>
        static bool UpdatePackageVersions(Package package, IEnumerable<AvailableVersion> versions)
        {
            Debug.Log(kHeader, $"[UpdatePackageVersions] {package.Name} has {versions.Count()} versions");
            var pInfoCurrent = package.GetInstalledVersion();
            pInfoCurrent.UnlockVersion();

            var versionInfos = versions
                .Select(v => v.ToPackageVersion(pInfoCurrent))
                .Concat(new[] { pInfoCurrent })
                .Where(pInfo => pInfo == pInfoCurrent || pInfo.GetVersion() != pInfoCurrent.GetVersion())
                .OrderBy(pInfo => pInfo.GetVersion())
                .ToArray();

            if (package.source.Count() != versionInfos.Length)
            {
                Debug.Log(kHeader, "[UpdatePackageVersions] package source changing");
                package.UpdateVersions(versionInfos);
                return true;
            }
            return false;
        }
        static void UpdatePackageCollection()
        {
            Debug.Log(kHeader, "[UpdatePackageCollection]");
            PackageExtensions.UpdatePackageCollection();
        }
    }

    internal static class PackageExtensions
    {
#if UNITY_2019_3_OR_NEWER
        internal static IEnumerable<UpmPackage> GetGitPackages()
        {
            return PackageDatabase.instance.upmPackages
                .Cast<UpmPackage>()
                .Where(x => x != null && x.installedVersion != null && x.installedVersion.HasTag(PackageTag.Git));
        }

        internal static IEnumerable<PackageInfo> GetGitPackageInfos()
        {
            return GetGitPackages().Select(x=>x.installedVersion);
        }

        internal static UpmPackageVersion GetInstalledVersion(this Package self)
        {
            return self.installedVersion;
        }

        internal static SemVersion GetVersion(this UpmPackageVersion self)
        {
            return self.version;
        }

        internal static void UnlockVersion(this UnityEditor.PackageManager.UI.PackageInfo self)
        {
            self.m_Tag = self.m_Tag & ~PackageTag.VersionLocked;
        }


        internal static void UpdatePackageCollection()
        {
            var empty = Enumerable.Empty<IPackage>();
            var updated = GetAllPackages()
                .Where(x => x != null && x.installedVersion != null && x.installedVersion.HasTag(PackageTag.Git));

            (PageManager.instance.GetCurrentPage() as Page).OnPackagesChanged(empty, empty, empty, updated);
        }
#else
        internal static IEnumerable<Package> GetGitPackages()
        {
        #if UNITY_2019_1_OR_NEWER
            return UnityEditor.PackageManager.UI.PackageCollection.packages.Values
        #else
            return UnityEditor.PackageManager.UI.PackageCollection.Instance.packages.Values
        #endif
                .Distinct()
                .Where(x => x != null && x.Current != null && (x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99));
        }

        internal static IEnumerable<PackageInfo> GetGitPackageInfos()
        {
            return GetGitPackages().Select(x => x.Current);
        }

        internal static UnityEditor.PackageManager.UI.PackageInfo GetInstalledVersion(this UnityEditor.PackageManager.UI.Package self)
        {
            return self.Current;
        }

        internal static void UnlockVersion(this UnityEditor.PackageManager.UI.PackageInfo self)
        {
            self.Origin = (PackageSource)99;
            self.IsLatest = false;
        }

        internal static SemVersion GetVersion(this UnityEditor.PackageManager.UI.PackageInfo self)
        {
            return self.Version;
        }

        internal static void UpdateVersions(this UnityEditor.PackageManager.UI.Package self, IEnumerable<UnityEditor.PackageManager.UI.PackageInfo> versions)
        {
            versions.OrderBy(v => v.GetVersion()).Last().IsLatest = true;
            self.source = versions;
            self.UpdateSource(versions);
        }

        internal static void UpdatePackageCollection()
        {
            UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>().FirstOrDefault().Collection.UpdatePackageCollection(false);
        }
#endif
    }
}
#endif // This line is added by Open Sesame Portable. DO NOT remov manually.