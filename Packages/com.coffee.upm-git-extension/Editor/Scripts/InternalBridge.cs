#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.
#if UNITY_2019_1_9 || UNITY_2019_1_10 || UNITY_2019_1_11 || UNITY_2019_1_12 || UNITY_2019_1_13 || UNITY_2019_1_14 || UNITY_2019_2_OR_NEWER
#define UNITY_2019_1_9_OR_NEWER
#endif
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.CompilerServices;
#if !UNITY_2019_1_9_OR_NEWER
using Semver;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using PackageInfo = UnityEditor.PackageManager.UI.PackageInfo;

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

#if UNITY_2019_3_OR_NEWER
        PackageInfo GetSelectedPackage() { return GetSelectedVersion().packageInfo; }
        UpmPackageVersion GetSelectedVersion() { return packageDetails.TargetVersion; }
#elif UNITY_2019_1_OR_NEWER
        UnityEditor.PackageManager.PackageInfo GetSelectedPackage() { return GetSelectedVersion().Info; }
        PackageInfo GetSelectedVersion() { return packageDetails.TargetVersion; }
#else
        UnityEditor.PackageManager.PackageInfo GetSelectedPackage() { return GetSelectedVersion().Info; }
        PackageInfo GetSelectedVersion() { return packageDetails.SelectedPackage; }
#endif

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

            PackageDatabase.instance.onPackagesChanged += (added, removed, _, updated) =>
            {
                // Removed or updated.
                if (removed.Concat(updated).Any(x => x?.installedVersion?.packageInfo?.source == PackageSource.Git))
                {
                    EditorApplication.delayCall += UpdatePackageCollection;
                }

                // Installed with git
                if (added.Concat(updated).Any(x => x?.installedVersion?.packageInfo?.source == PackageSource.Git))
                {
                    EditorApplication.delayCall += UpdateGitPackages;
                }
            };
#else
            packageList.OnLoaded -= UpdateGitPackageVersions;
            packageList.OnLoaded += UpdateGitPackageVersions;
#endif
        }

        /// <summary>
        /// On click 'View repository' callback.
        /// </summary>
        public void ViewRepoClick()
        {
            Application.OpenURL(PackageUtils.GetRepoUrl(GetSelectedPackage().packageId, true));
        }

#if UNITY_2019_3_OR_NEWER
        static IEnumerable<UpmPackage> GetAllPackages()
        {
            return PackageDatabase.instance.upmPackages.Cast<UpmPackage>();
        }
#elif UNITY_2019_1_OR_NEWER
		static IEnumerable<Package> GetAllPackages()
        {
			return UnityEditor.PackageManager.UI.PackageCollection.packages.Values.Distinct();
        }
#else
        static IEnumerable<Package> GetAllPackages()
        {
            var collection = UnityEditor.PackageManager.UI.PackageCollection.Instance;
            return collection?.LatestListPackages
                .Select(x => x.Name)
                .Distinct()
                .Select(collection.GetPackageByName)
                .Distinct();
        }
#endif

        /// <summary>
        /// Update all infomations of git packages.
        /// </summary>
        public void UpdateGitPackageVersions()
        {
            // Get packages installed with git.
            var gitPackageInfos = GetAllPackages()
#if UNITY_2019_3_OR_NEWER
                .Where(x => x != null && x.installedVersion != null && x.installedVersion.HasTag(PackageTag.Git))
                .Select(x => x.installedVersion);
#else
                .Where(x => x != null && x.Current != null && (x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99))
                .Select(x => x.Current);
#endif

            // Start update task.
            foreach (var pInfo in gitPackageInfos)
            {
                var packageName = pInfo.Name;
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.PackageId);
                AvailableVersions.UpdateAvailableVersions(pInfo.Name, repoUrl);
            }
        }

        /// <summary>
        /// Update package info.
        /// </summary>
#if UNITY_2019_3_OR_NEWER
        void UpdatePackageVersions(UpmPackage package, IEnumerable<string> versions)
        {
            var pInfo = package.installedVersion as UpmPackageVersion;
            var json = JsonUtility.ToJson(pInfo.packageInfo);

            string packageName, repoUrl, installedRefName;
            PackageUtils.SplitPackageId(pInfo.uniqueId, out packageName, out repoUrl, out installedRefName);

            Debug.Log(kHeader, "[UpdatePackageVersions] packageName = {0}, count = {1}, current = {2}", package.name, versions.Count(), pInfo.version);
            var versionInfos = versions
                .Select(ver =>
                {
                    Debug.Log(kHeader, "[UpdatePackageVersions] version = {0}", ver);
                    var splited = ver.Split(',');
                    var refName = splited[0];
                    var version = splited[1];
                    var semver = SemVersion.Parse(version == refName ? version : version + "-" + refName);

                    var info = JsonUtility.FromJson<PackageInfo>(json);
                    info.m_Version = version;
                    info.m_Git = new GitInfo("", refName);

                    var p = new UpmPackageVersion(info, false, semver, pInfo.displayName);

                    // Update tag.
                    PackageTag tag = PackageTag.Git | PackageTag.Installable | PackageTag.Removable;
                    if ((semver.Major == 0 && string.IsNullOrEmpty(semver.Prerelease)) ||
                    PackageTag.Preview.ToString().Equals(semver.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
                        tag |= PackageTag.Preview;

                    if (semver.IsRelease())
                    {
                        tag |= PackageTag.Release;
                    }
                    else
                    {
                        if ((semver.Major == 0 && string.IsNullOrEmpty(semver.Prerelease)) ||
                            PackageTag.Preview.ToString().Equals(semver.Prerelease.Split('.')[0], StringComparison.InvariantCultureIgnoreCase))
                            tag |= PackageTag.Preview;
                    }

                    p.m_Tag = tag;
                    p.m_IsFullyFetched = true;
                    m_PackageId = string.Format("{0}@{1}#{2}", packageName, repoUrl, semver);
                    return p;
                })
                .Concat(new[] { pInfo })
                .Where(p => p == pInfo || p.version != pInfo.version)
                .OrderBy(x => x.version)
                .ToArray();

            if (0 < versionInfos.Length)
            {
                // Add verify tag on latest version.
                var latest = versionInfos
                    .Where(x=>x.version.IsRelease())
                    .LastOrDefault();
                if(latest != null)
                {
                    latest.m_Tag |= PackageTag.Verified;
                }

                // Unlock version tag.
                pInfo.m_Tag = pInfo.m_Tag & ~PackageTag.VersionLocked;
                Debug.Log(kHeader, "[UpdatePackageVersions] package source changing");
                package.UpdateVersions(versionInfos);
            }
        }

        void UpdatePackageCollection()
        {
            Debug.Log(kHeader, "[UpdatePackageCollection]");
            var empty = Enumerable.Empty<IPackage>();
            var updated = GetAllPackages()
                .Where(x => x != null && x.installedVersion != null && x.installedVersion.HasTag(PackageTag.Git));

            foreach (var p in updated)
            {
                Debug.Log(kHeader, "  -> {0}, {1}", p.name, p.installedVersion.version);
            }

            (PageManager.instance.GetCurrentPage() as Page).OnPackagesChanged(empty, empty, empty, updated);
        }
#else
        void UpdatePackageVersions(Package package, IEnumerable<string> versions)
        {
            Debug.Log(kHeader, "[UpdatePackageVersions] packageName = {0}, count = {1}", package.Current.Name, versions.Count());
            var pInfo = package.Current;
            var json = JsonUtility.ToJson(pInfo);
            var versionInfos = versions
                .Select(ver =>
                {
                    var splited = ver.Split(',');
                    var refName = splited[0];
                    var version = splited[1];
                    var newPInfo = JsonUtility.FromJson<PackageInfo>(json);

                    newPInfo.Version = SemVersion.Parse(version == refName ? version : version + "-" + refName);
                #if UNITY_2019_2_OR_NEWER
                    newPInfo.IsInstalled = false;
                #else
                    newPInfo.IsCurrent = false;
                #endif

                    newPInfo.IsVerified = false;
                    newPInfo.Origin = (PackageSource)99;
                    newPInfo.Info = pInfo.Info;
                    newPInfo.PackageId = string.Format("{0}@{1}", newPInfo.Name, refName);
                    return newPInfo;
                })
                .Concat(new[] { pInfo })
                .Where(p => p == pInfo || p.Version != pInfo.Version)
                .ToArray();

            if (0 < versionInfos.Length)
            {
                Debug.Log(kHeader, "[UpdatePackageVersions] package source changing");
                versionInfos.OrderBy(v => v.Version).Last().IsLatest = true;
                package.source = versionInfos;
            }
        }

        void UpdatePackageCollection()
        {
            Debug.Log(kHeader, "[UpdatePackageCollection]");
            var packageWindow = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>().FirstOrDefault();
            packageWindow.Collection.UpdatePackageCollection(false);
        }
#endif
    }
}
#endif // This line is added by Open Sesame Portable. DO NOT remov manually.