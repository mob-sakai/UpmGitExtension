#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
#if UNITY_2019_1_9 || UNITY_2019_1_10 || UNITY_2019_1_11 || UNITY_2019_1_12 || UNITY_2019_1_13 || UNITY_2019_1_14 || UNITY_2019_2_OR_NEWER
#define UNITY_2019_1_9_OR_NEWER
#endif
using System.Linq;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
#if !UNITY_2019_1_9_OR_NEWER
using Semver;
#endif
#if UNITY_2019_3_OR_NEWER
using Package = UnityEditor.PackageManager.UI.UpmPackage;
using PackageInfo = UnityEditor.PackageManager.UI.UpmPackageVersion;
#else
using Package = UnityEditor.PackageManager.UI.Package;
using PackageInfo = UnityEditor.PackageManager.UI.PackageInfo;
using PackageCollection = UnityEditor.PackageManager.UI.PackageCollection;
#endif

namespace Coffee.UpmGitExtension
{
    internal class Bridge
    {
        const string kHeader = "<b><color=#c7634c>[Bridge]</color></b> ";

        private static Bridge instance = new Bridge();
        public static Bridge Instance { get { return instance; } }

        PackageList packageList;
        PackageDetails packageDetails;

        bool reloading;

        private Bridge() { }

        public void Setup(VisualElement root)
        {
            packageList = root.Q<PackageList>();
            packageDetails = root.Q<PackageDetails>();
            Debug.Log(kHeader, $"[Setup] {packageList}, {packageDetails},");

#if UNITY_2019_3_OR_NEWER
            packageList.onPackageListLoaded -= UpdateGitPackageVersions;
            packageList.onPackageListLoaded += UpdateGitPackageVersions;

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
            UpdateGitPackageVersions();
            UpdateAvailableVersionsForGitPackages();
        }

        /// <summary>
        /// Update available versions for git packages.
        /// </summary>
        public static void UpdateAvailableVersionsForGitPackages()
        {
            // Start update task.
            foreach (var package in PackageExtensions.GetGitPackages())
            {
                var pInfo = package.GetInstalledVersion().GetPackageInfo();
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.packageId);
                Debug.Log(kHeader, $"[UpdateAvailableVersionsForGitPackages] {pInfo.packageId} => {pInfo.name}, {repoUrl}");
                AvailableVersionExtensions.UpdateAvailableVersions(pInfo.name, repoUrl);
            }
        }

        /// <summary>
        /// Update all infomations of git packages.
        /// </summary>
        public static void UpdateGitPackageVersions()
        {
            bool changed = false;
            // Start update task.
            foreach (var package in PackageExtensions.GetGitPackages())
            {
                var pInfo = package.GetInstalledVersion().GetPackageInfo();
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.packageId);
                var versions = AvailableVersions.GetVersions(package.GetName(), repoUrl);
                Debug.Log(kHeader, $"[UpdateGitPackageVersions] {pInfo.packageId} => {package.GetName()}, {repoUrl}, {versions.Count()}");
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
            Debug.Log(kHeader, $"[UpdatePackageVersions] {package.GetName()} has {versions.Count()} versions");
            var pInfoCurrent = package.GetInstalledVersion();
            pInfoCurrent.UnlockVersion();

            var versionInfos = versions
                .Select(v => v.ToPackageVersion(pInfoCurrent))
                .Concat(new[] { pInfoCurrent })
                .Where(pInfo => pInfo == pInfoCurrent || pInfo.GetVersion() != pInfoCurrent.GetVersion())
                .OrderBy(pInfo => pInfo.GetVersion())
                .ToArray();

            if (package.GetVersionCount() != versionInfos.Length)
            {
                Debug.Log(kHeader, "[UpdatePackageVersions] package source changing");
                package.UpdateVersions(versionInfos);
                return true;
            }
            return false;
        }

        public static void UpdatePackageCollection()
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

        internal static IEnumerable<UpmPackageVersion> GetGitPackageInfos()
        {
            return GetGitPackages().Select(x=>x.installedVersion).Cast<UpmPackageVersion>();
        }

        internal static UpmPackageVersion GetInstalledVersion(this UpmPackage self)
        {
            return self.installedVersion as UpmPackageVersion;
        }

        internal static SemVersion GetVersion(this UpmPackageVersion self)
        {
            return self.version ?? new SemVersion(0);
        }

        internal static UnityEditor.PackageManager.PackageInfo GetPackageInfo(this PackageInfo self)
        {
            return self.packageInfo;
        }

        internal static string GetName(this UpmPackage self)
        {
            return self.name;
        }

        internal static int GetVersionCount(this UpmPackage self)
        {
            return self.versionList.all.Count();
        }

        internal static void UnlockVersion(this UpmPackageVersion self)
        {
            self.m_Tag = self.m_Tag & ~PackageTag.VersionLocked;
        }

        internal static void UpdatePackageCollection()
        {
            var empty = Enumerable.Empty<IPackage>();
            (PageManager.instance.GetCurrentPage() as Page).OnPackagesChanged(empty, empty, empty, GetGitPackages());
        }

        internal static UpmPackageVersion ToPackageVersion(this AvailableVersion self, UpmPackageVersion baseInfo)
        {
            var semver = SemVersion.Parse(self.refNameVersion);

            var newPInfo = JsonUtility.FromJson<UnityEditor.PackageManager.PackageInfo>(JsonUtility.ToJson(baseInfo.packageInfo));
            newPInfo.m_Version = self.version;
            newPInfo.m_Git = new GitInfo("", self.refName);

            var p = new UpmPackageVersion(newPInfo, false, semver, newPInfo.displayName);

            // Update tag.
            PackageTag tag = PackageTag.Git | PackageTag.Installable | PackageTag.Removable;
            if (semver.Major == 0 || !string.IsNullOrEmpty(semver.Prerelease))
                tag |= PackageTag.Preview;
            else if (semver.IsRelease())
                tag |= PackageTag.Release;

            p.m_Tag = tag;
            p.m_IsFullyFetched = true;
            p.m_PackageId = string.Format("{0}@{1}#{2}", self.packageName, self.repoUrl, self.refName);
            return p;
        }
#else
        internal static IEnumerable<Package> GetGitPackages()
        {
#if UNITY_2019_1_OR_NEWER
            return PackageCollection.packages.Values
#else
            return PackageCollection.Instance.packages.Values
#endif
                .Distinct()
                .Where(x => x != null && x.Current != null && (x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99));
        }

        internal static IEnumerable<PackageInfo> GetGitPackageInfos()
        {
            return GetGitPackages().Select(x => x.Current);
        }

        internal static PackageInfo GetInstalledVersion(this Package self)
        {
            return self.Current;
        }

        internal static void UnlockVersion(this PackageInfo self)
        {
            self.Origin = (PackageSource)99;
            self.IsLatest = false;
        }

        internal static SemVersion GetVersion(this PackageInfo self)
        {
            return self.Version;
        }

        internal static UnityEditor.PackageManager.PackageInfo GetPackageInfo(this PackageInfo self)
        {
            return self.Info;
        }

        internal static string GetName(this Package self)
        {
            return self.Name;
        }

        internal static int GetVersionCount(this Package self)
        {
            return self.source.Count();
        }

        internal static void UpdateVersions(this Package self, IEnumerable<PackageInfo> versions)
        {
            var latest = versions.OrderBy(v => v.GetVersion()).Last();
            versions = versions.Select(v =>
            {
                v.IsLatest = v == latest;
                v.State = v == latest ? PackageState.UpToDate : PackageState.Outdated;
                return v;
            });

            self.Set("source", versions);
        }

        internal static void UpdatePackageCollection()
        {
            UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>().FirstOrDefault().Collection.UpdatePackageCollection(false);
        }

        internal static PackageInfo ToPackageVersion(this AvailableVersion self, PackageInfo baseInfo)
        {
            var newPInfo = JsonUtility.FromJson<PackageInfo>(JsonUtility.ToJson(baseInfo));
            newPInfo.Version = SemVersion.Parse(self.refNameVersion);
#if UNITY_2019_2_OR_NEWER
            newPInfo.IsInstalled = false;
#else
            newPInfo.IsCurrent = false;
#endif
            newPInfo.Info = baseInfo.Info;
            newPInfo.PackageId = string.Format("{0}@{1}", newPInfo.Name, self.refNameVersion);
            return newPInfo;
        }
#endif
    }
}
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.