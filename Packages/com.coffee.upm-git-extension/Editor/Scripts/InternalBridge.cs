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

namespace UnityEditor.PackageManager.UI.InternalBridge
{
    public class Bridge
    {
        private static Bridge instance = new Bridge();
        public static Bridge Instance { get { return instance; } }

        LoadingSpinner loadingSpinner = null;
        PackageList packageList = null;
        PackageDetails packageDetails = null;

        bool reloading;

		LoadingSpinner GetLoadingSpinner () { return loadingSpinner as LoadingSpinner; }
        PackageList GetPackageList() { return packageList as PackageList; }
        PackageDetails GetPackageDetails() { return packageDetails as PackageDetails; }

#if UNITY_2019_3_OR_NEWER
        PackageInfo GetSelectedPackage() { return GetSelectedVersion().packageInfo; }
        UpmPackageVersion GetSelectedVersion() { return Expose.FromObject(packageDetails).Get("targetVersion").As<UpmPackageVersion>(); }
#elif UNITY_2019_1_OR_NEWER
        PackageManager.PackageInfo GetSelectedPackage() { return GetSelectedVersion().Info; }
        PackageInfo GetSelectedVersion() { return Expose.FromObject(packageDetails).Get("TargetVersion").As<PackageInfo>(); }
#else
        PackageManager.PackageInfo GetSelectedPackage() { return GetSelectedVersion().Info; }
        PackageInfo GetSelectedVersion() { return Expose.FromObject(packageDetails).Get("SelectedPackage").As<PackageInfo>(); }
#endif

        private Bridge() { }

        public void Setup(VisualElement root)
        {
            loadingSpinner = root.Q<LoadingSpinner>();
            packageList = root.Q<PackageList>();
            packageDetails = root.Q<PackageDetails>();
            Debug.LogFormat("[Bridge.Setup] {0}, {1}, {2},", loadingSpinner, packageList, packageDetails);

#if UNITY_2019_3_OR_NEWER
            GetPackageList().onPackageListLoaded -= UpdateGitPackages;
            GetPackageList().onPackageListLoaded += UpdateGitPackages;

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
            GetPackageList().OnLoaded -= UpdateGitPackages;
            GetPackageList().OnLoaded += UpdateGitPackages;
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
			return PackageCollection.packages.Values.Distinct();
        }
#else
        static IEnumerable<Package> GetAllPackages()
        {
            var collection = PackageCollection.Instance;
            return collection?.LatestListPackages
                .Select(x => x.Name)
                .Distinct()
                .Select(collection.GetPackageByName)
                .Distinct() ?? Enumerable.Empty<Package>();
        }
#endif

        /// <summary>
        /// On click 'Update package' callback.
        /// </summary>
        public void UpdateClick()
        {
            Debug.LogFormat("[Bridge.UpdateClick]");
            reloading = false;
            var selectedPackage = GetSelectedPackage();
            if (selectedPackage.source == PackageSource.Git)
            {
                string packageId = selectedPackage.packageId;
                string url = PackageUtils.GetRepoUrl(packageId);
#if UNITY_2019_3_OR_NEWER
                string refName = GetSelectedVersion().packageInfo.git.revision;
#else
                string refName = GetSelectedVersion().VersionId.Split('@')[1];
#endif
                PackageUtils.UninstallPackage(selectedPackage.name);
                PackageUtils.InstallPackage(selectedPackage.name, url, refName);
            }
            else
            {
                packageDetails.UpdateClick();
                // Expose.FromObject(packageDetails).Call("UpdateClick");
            }
        }

        /// <summary>
        /// On click 'Remove package' callback.
        /// </summary>
        public void RemoveClick()
        {
            Debug.LogFormat("[Bridge.UpdateClick]");
            reloading = false;
            Debug.LogFormat("[RemoveClick]");
            var selectedPackage = GetSelectedPackage();
            if (selectedPackage.source == PackageSource.Git)
            {
                PackageUtils.UninstallPackage(selectedPackage.name);
            }
            else
            {
                packageDetails.RemoveClick();
            }
        }

        /// <summary>
        /// Update all infomations of git packages.
        /// </summary>
        public void UpdateGitPackages()
        {
            Debug.LogFormat("[Bridge.UpdateGitPackages] reloading = {0}", reloading);
            if (reloading)
            {
                reloading = false;
                return;
            }

            // Get git packages.
            var gitPackages = GetAllPackages()
#if UNITY_2019_3_OR_NEWER
                .Where(x => x != null && x.installedVersion != null && x.installedVersion.HasTag(PackageTag.Git))
#else
                .Where(x => x != null && x.Current != null && (x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99))
#endif
                .ToArray();

            if (gitPackages.Length == 0) return;

            // Start job.
#if UNITY_2019_3_OR_NEWER
            HashSet<string> jobs = new HashSet<string>(gitPackages.Select(p => p.installedVersion.name));
#else
            HashSet<string> jobs = new HashSet<string>(gitPackages.Select(p => p.Current.Name));
#endif

            // Update
            foreach (var p in gitPackages)
            {
                var package = p;
#if UNITY_2019_3_OR_NEWER
                var pInfo = p.installedVersion as UpmPackageVersion;
                var packageName = p.name;
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.uniqueId);

                // Get available branch / tag names with package version. (e.g. "refs/tags/1.1.0,1.1.0")
                GitUtils.GetRefs(pInfo.name, repoUrl, refs =>
                {
                    UpdatePackageVersions(package, refs);
                    jobs.Remove(packageName);
                    if (jobs.Count == 0)
                    {
                        // StopSpinner();
                        reloading = true;
                        UpdatePackageCollection();
                        reloading = false;
                    }
                });
#else
                var pInfo = p.Current;
                pInfo.IsLatest = false;
                Debug.LogFormat("[UpdateGitPackages] packageName = {0}", pInfo.Name);

                var packageName = pInfo.Name;
                pInfo.Origin = (PackageSource)99;
                var json = JsonUtility.ToJson(pInfo);
                var repoUrl = PackageUtils.GetRepoUrl(pInfo.PackageId);

                // Get available branch/tag names with package version. (e.g. "refs/tags/1.1.0,1.1.0")
                GitUtils.GetRefs(pInfo.Name, repoUrl, refs =>
                {
                    UpdatePackageVersions(package, refs);
                    jobs.Remove(packageName);
                    if (jobs.Count == 0)
                    {
                        // StopSpinner();
                        reloading = true;
                        UpdatePackageCollection();
                        reloading = false;
                    }
                });
#endif
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

            Debug.LogFormat("[UpdatePackageVersions] packageName = {0}, count = {1}, current = {2}", package.name, versions.Count(), pInfo.version);
            var versionInfos = versions
                .Select(ver =>
                {
                    Debug.LogFormat("[UpdatePackageVersions] version = {0}", ver);
                    var splited = ver.Split(',');
                    var refName = splited[0];
                    var version = splited[1];
                    var semver = SemVersion.Parse(version == refName ? version : version + "-" + refName);

                    var info = JsonUtility.FromJson<PackageInfo>(json);
                    Expose.FromObject(info).Set("m_Version", version);
                    Expose.FromObject(info).Set("m_Git", new GitInfo("", refName));

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

                    Expose.FromObject(p).Set("m_Tag", tag);
                    Expose.FromObject(p).Set("m_IsFullyFetched", true);
                    Expose.FromObject(p).Set("m_PackageId", string.Format("{0}@{1}#{2}", packageName, repoUrl, semver));

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
                    var tag = Expose.FromObject(latest).Get("m_Tag").As<PackageTag>();
                    tag |= PackageTag.Verified;
                    Expose.FromObject(latest).Set("m_Tag", tag);
                }

                // Unlock version tag.
                var t = Expose.FromObject(pInfo).Get("m_Tag").As<PackageTag>();
                Expose.FromObject(pInfo).Set("m_Tag", t & ~PackageTag.VersionLocked);

                Debug.LogFormat("[UpdatePackageVersions] package source changing");
                package.UpdateVersions(versionInfos);
            }
        }

        void UpdatePackageCollection()
        {
            Debug.LogFormat("[UpdatePackageCollection]");
            var empty = Enumerable.Empty<IPackage>();
            var updated = GetAllPackages()
                .Where(x => x != null && x.installedVersion != null && x.installedVersion.HasTag(PackageTag.Git));

            foreach (var p in updated)
            {
                Debug.LogFormat("  -> {0}, {1}", p.name, p.installedVersion.version);
            }

            (PageManager.instance.GetCurrentPage() as Page).OnPackagesChanged(empty, empty, empty, updated);
        }
#else
        void UpdatePackageVersions(Package package, IEnumerable<string> versions)
        {
            Debug.LogFormat("[UpdatePackageVersions] packageName = {0}, count = {1}", package.Current.Name, versions.Count());
            var pInfo = package.Current;
            var json = JsonUtility.ToJson(pInfo);
            var versionInfos = versions
                .Select(ver =>
                {
                    var splited = ver.Split(',');
                    var refName = splited[0];
                    var version = splited[1];
                    var newPInfo = JsonUtility.FromJson(json, typeof(PackageInfo)) as PackageInfo;

                    newPInfo.Version = SemVersion.Parse(version == refName ? version : version + "-" + refName);

                    var exPackageInfo = Expose.FromObject(newPInfo);
					var memberName = 0 < Application.unityVersion.CompareTo("2019.2") ? "IsInstalled" : "IsCurrent";
					exPackageInfo.Set(memberName, false);

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
                Debug.LogFormat("[UpdatePackageVersions] package source changing");
                versionInfos.OrderBy(v => v.Version).Last().IsLatest = true;
                Expose.FromObject(package).Set("source", versionInfos);
            }
        }

        void UpdatePackageCollection()
        {
            Debug.LogFormat("[UpdatePackageCollection]");
            var packageWindow = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>().FirstOrDefault();
            packageWindow.Collection.UpdatePackageCollection(false);
        }
#endif

#if UNITY_2018
        public void ViewDocClick()
        {
            var packageInfo = GetSelectedPackage();
            if (packageInfo.source == PackageSource.Git)
            {
                var docsFolder = Path.Combine(packageInfo.resolvedPath, "Documentation~");
                if (!Directory.Exists(docsFolder))
                    docsFolder = Path.Combine(packageInfo.resolvedPath, "Documentation");
                if (Directory.Exists(docsFolder))
                {
                    var mdFiles = Directory.GetFiles(docsFolder, "*.md", SearchOption.TopDirectoryOnly);
                    var docsMd = mdFiles.FirstOrDefault(d => Path.GetFileName(d).ToLower() == "index.md")
                        ?? mdFiles.FirstOrDefault(d => Path.GetFileName(d).ToLower() == "tableofcontents.md") ?? mdFiles.FirstOrDefault();
                    if (!string.IsNullOrEmpty(docsMd))
                    {
                        Application.OpenURL(new Uri(docsMd).AbsoluteUri);
                        return;
                    }
                }
            }
            Expose.FromObject(GetPackageDetails()).Call("ViewDocClick");
        }

        public void ViewChangelogClick()
        {
            var packageInfo = GetSelectedPackage();
            if (packageInfo.source == PackageSource.Git)
            {
                var changelogFile = Path.Combine(packageInfo.resolvedPath, "CHANGELOG.md");
                if (File.Exists(changelogFile))
                {
                    Application.OpenURL(new Uri(changelogFile).AbsoluteUri);
                    return;
                }
            }
            Expose.FromObject(GetPackageDetails()).Call("ViewChangelogClick");
        }

        public void ViewLicensesClick()
        {
            var packageInfo = GetSelectedPackage();
            if (packageInfo.source == PackageSource.Git)
            {
                var licenseFile = Path.Combine(packageInfo.resolvedPath, "LICENSE.md");
                if (File.Exists(licenseFile))
                {
                    Application.OpenURL(new Uri(licenseFile).AbsoluteUri);
                    return;
                }
            }
            Expose.FromObject(GetPackageDetails()).Call("ViewLicensesClick");
        }
#endif
    }
}
#endif // This line is added by Open Sesame Portable. DO NOT remov manually.