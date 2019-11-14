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
#if !UNITY_2019_2_OR_NEWER
using Semver;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif


[assembly: IgnoresAccessChecksTo("Unity.PackageManagerUI.Editor")]
[assembly: IgnoresAccessChecksTo("UnityEditor")]
namespace UnityEditor.PackageManager.UI
{
    public static class ButtonExtension
    {
        public static void OverwriteCallback(this Button button, Action action)
        {
            button.RemoveManipulator(button.clickable);
            button.clickable = new Clickable(action);
            button.AddManipulator(button.clickable);
        }
    }

    public class InternalBridge
    {

        private static InternalBridge instance = new InternalBridge();
        public static InternalBridge Instance { get { return instance; } }

        VisualElement loadingSpinner = null;
        VisualElement packageList = null;
        VisualElement packageDetails = null;

#if UNITY_2019_1_OR_NEWER
        object SelectedPackage { get { return Expose.FromObject (packageDetails).Get ("TargetVersion").As<PackageInfo>(); } }
#else
        object SelectedPackage { get { return Expose.FromObject (packageDetails).Get ("SelectedPackage").As<PackageInfo> (); } }
#endif

        private InternalBridge() { }

        public void Setup(VisualElement loadingSpinner, VisualElement packageList, VisualElement packageDetails)
        {

            Debug.Log("Setup");

            this.loadingSpinner = loadingSpinner as LoadingSpinner;
            this.packageList = packageList as PackageList;
            this.packageDetails = packageDetails as PackageDetails;

            (packageList as PackageList).OnLoaded -= UpdateGitPackages;
            (packageList as PackageList).OnLoaded += UpdateGitPackages;
        }

        void ViewDocmentationClick(string filePattern, Action<string> action, Action defaultAction)
        {
            var selectedPackage = SelectedPackage as PackageInfo;
            if (selectedPackage.Info.source == PackageSource.Git)
            {
                action(PackageUtilsXXX.GetFilePath(selectedPackage.Info.resolvedPath, filePattern));
            }
            else
            {
                defaultAction();
            }
        }


        public void ViewDocClick(Action<string> action)
        {
            ViewDocmentationClick("README.*", action, ()=>Expose.FromObject(packageDetails).Call("ViewDocClick"));
        }
        public void ViewChangelogClick(Action<string> action)
        {
            ViewDocmentationClick("CHANGELOG.*", action, ()=>Expose.FromObject(packageDetails).Call("ViewChangelogClick"));
        }
        public void ViewLicensesClick(Action<string> action)
        {
            ViewDocmentationClick("LICENSE.*", action, ()=>Expose.FromObject(packageDetails).Call("ViewLicensesClick"));
        }

        public void ViewRepoClick()
        {
            var selectedPackage = SelectedPackage as PackageInfo;
            Application.OpenURL(PackageUtilsXXX.GetRepoHttpsUrl(selectedPackage.Info.packageId));
        }


        static IEnumerable<Package> GetAllPackages()
        {
#if UNITY_2019_1_OR_NEWER
            return PackageCollection.packages.Values.Distinct();
#else
            var collection = PackageCollection.Instance;
            return collection?.LatestListPackages
                .Select(x => x.Name)
                .Distinct()
                .Select(collection.GetPackageByName)
                .Distinct() ?? Enumerable.Empty<Package>();
#endif
        }


        public void StartSpinner()
        {
            (loadingSpinner as LoadingSpinner)?.Start();
        }

        public void StopSpinner()
        {
            (loadingSpinner as LoadingSpinner)?.Stop();
        }
        static readonly Regex s_RepoUrl = new Regex("^([^@]+)@([^#]+)(#.+)?$", RegexOptions.Compiled);

        public void UpdateClick()
        {
            var packageInfo = SelectedPackage as PackageInfo;
            if (packageInfo.Info.source == PackageSource.Git)
            {
                string packageId = packageInfo.Info.packageId;
                string url = s_RepoUrl.Replace(packageId, "$2");
                string refName = packageInfo.VersionId.Split('@')[1];
                PackageUtilsXXX.RemovePackage(packageInfo.Name);
                PackageUtilsXXX.InstallPackage(packageInfo.Name, url, refName);
            }
            else
            {
                Expose.FromObject(packageDetails).Call("UpdateClick");
            }
        }

        public void RemoveClick()
        {
            var packageInfo = SelectedPackage as PackageInfo;
            if (packageInfo.Info.source == PackageSource.Git)
            {
                PackageUtilsXXX.RemovePackage(packageInfo.Name);
            }
            else
            {
                Expose.FromObject(packageDetails).Call("RemoveClick");
            }
        }

        int frameCount = 0;
        bool reloading;

        public void UpdateGitPackages()
        {
            Debug.Log("UpdateGitPackages");
            if (reloading) return;

            // Get git packages.
            var gitPackages = GetAllPackages()
                .Where(x => x != null && x.Current != null && (x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99))
                .ToArray();

            if (gitPackages.Length == 0) return;

            // Start job.
            // StartSpinner();
            HashSet<string> jobs = new HashSet<string>(gitPackages.Select(p => p.Current.Name));

            // Update
            foreach (var p in gitPackages)
            {
                var package = p;
                var pInfo = p.Current;
                pInfo.IsLatest = false;

                var packageName = pInfo.Name;
                pInfo.Origin = (PackageSource)99;
                var json = JsonUtility.ToJson(pInfo);
                var repoUrl = s_RepoUrl.Replace(pInfo.PackageId, "$2");

                // Get available branch/tag names with package version. (e.g. "refs/tags/1.1.0,1.1.0")
                GitUtils.GetRefs(pInfo.Name, repoUrl, refs =>
                {
                    UpdatePackageVersions(package, refs);
                    jobs.Remove(packageName);
                    if (jobs.Count == 0)
                    {
                        // StopSpinner();
                        frameCount = Time.frameCount;
                        reloading = true;
                        UpdatePackageCollection();
                        reloading = false;

                    }
                });
            }
        }

        void UpdatePackageVersions(Package package, IEnumerable<string> versions)
        {
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
                versionInfos.OrderBy(v => v.Version).Last().IsLatest = true;
                Expose.FromObject(package).Set("source", versionInfos);
            }
        }

        void UpdatePackageCollection()
        {
            var packageWindow = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>().FirstOrDefault();
            packageWindow.Collection.UpdatePackageCollection(false);
        }
    }
}


namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class IgnoresAccessChecksToAttribute : Attribute
	{
		public IgnoresAccessChecksToAttribute(string assemblyName)
		{
			AssemblyName = assemblyName;
		}

		public string AssemblyName { get; }
	}
}
