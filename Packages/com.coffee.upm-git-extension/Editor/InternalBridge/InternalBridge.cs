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
namespace UnityEditor.PackageManager.UI.InternalBridge
{
    public class Bridge
    {
        private static Bridge instance = new Bridge();
        public static Bridge Instance { get { return instance; } }

        VisualElement loadingSpinner = null;
        VisualElement packageList = null;
        VisualElement packageDetails = null;

#if UNITY_2019_1_OR_NEWER
        object SelectedPackage { get { return Expose.FromObject (packageDetails).Get ("TargetVersion").As<PackageInfo>(); } }
#else
        object SelectedPackage { get { return Expose.FromObject (packageDetails).Get ("SelectedPackage").As<PackageInfo> (); } }
#endif

        private Bridge() { }

        /// <summary>
		/// Setup bridge.
		/// </summary>
		/// <param name="loadingSpinner"></param>
		/// <param name="packageList"></param>
		/// <param name="packageDetails"></param>
        public void Setup(VisualElement loadingSpinner, VisualElement packageList, VisualElement packageDetails)
        {
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
                action(PackageUtils.GetFilePathWithPattern(selectedPackage.Info.resolvedPath, filePattern));
            }
            else
            {
                defaultAction();
            }
        }

		/// <summary>
		/// On click 'View Documentation' callback.
		/// </summary>
		public void ViewDocClick(Action<string> action)
        {
            ViewDocmentationClick("README.*", action, ()=>Expose.FromObject(packageDetails).Call("ViewDocClick"));
        }

		/// <summary>
		/// On click 'View changelog' callback.
		/// </summary>
		public void ViewChangelogClick(Action<string> action)
        {
            ViewDocmentationClick("CHANGELOG.*", action, ()=>Expose.FromObject(packageDetails).Call("ViewChangelogClick"));
        }

		/// <summary>
		/// On click 'View licenses' callback.
		/// </summary>
		public void ViewLicensesClick(Action<string> action)
        {
            ViewDocmentationClick("LICENSE.*", action, ()=>Expose.FromObject(packageDetails).Call("ViewLicensesClick"));
        }

		/// <summary>
		/// On click 'View repository' callback.
		/// </summary>
		public void ViewRepoClick()
        {
            var selectedPackage = SelectedPackage as PackageInfo;
            Application.OpenURL(PackageUtils.GetRepoUrl(selectedPackage.Info.packageId, true));
        }

		/// <summary>
		/// Get all installed packages.
		/// </summary>
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


		/// <summary>
		/// Start spinner.
		/// </summary>
		public void StartSpinner()
        {
            (loadingSpinner as LoadingSpinner)?.Start();
        }

		/// <summary>
		/// Stop spinner.
		/// </summary>
		public void StopSpinner()
        {
            (loadingSpinner as LoadingSpinner)?.Stop();
        }

		bool reloading;

		/// <summary>
		/// On click 'Update package' callback.
		/// </summary>
		public void UpdateClick()
        {
            var packageInfo = SelectedPackage as PackageInfo;
            if (packageInfo.Info.source == PackageSource.Git)
            {
                string packageId = packageInfo.Info.packageId;
				string url = PackageUtils.GetRepoUrl(packageId);
                string refName = packageInfo.VersionId.Split('@')[1];
				PackageUtils.UninstallPackage (packageInfo.Name);
				PackageUtils.InstallPackage (packageInfo.Name, url, refName);
			}
            else
            {
                Expose.FromObject(packageDetails).Call("UpdateClick");
            }
        }

		/// <summary>
		/// On click 'Remove package' callback.
		/// </summary>
		public void RemoveClick()
        {
            var packageInfo = SelectedPackage as PackageInfo;
            if (packageInfo.Info.source == PackageSource.Git)
            {
                PackageUtils.UninstallPackage(packageInfo.Name);
            }
            else
            {
                Expose.FromObject(packageDetails).Call("RemoveClick");
            }
        }


		/// <summary>
		/// Update all infomations of git packages.
		/// </summary>
		public void UpdateGitPackages()
        {
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
            }
        }

		/// <summary>
		/// Update package info.
		/// </summary>
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

		/// <summary>
		/// Update package collection to reflesh package list.
		/// </summary>
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
