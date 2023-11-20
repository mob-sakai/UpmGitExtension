// UpmPackage.UpdateVersions is removed from Unity 2021.3.26

#if UNITY_2021_3_0 || UNITY_2021_3_1 || UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5 || UNITY_2021_3_6 || UNITY_2021_3_7 || UNITY_2021_3_8 || UNITY_2021_3_9
#elif UNITY_2021_3_10 || UNITY_2021_3_11 || UNITY_2021_3_12 || UNITY_2021_3_13 || UNITY_2021_3_14 || UNITY_2021_3_15 || UNITY_2021_3_16 || UNITY_2021_3_17 || UNITY_2021_3_18 || UNITY_2021_3_19
#elif UNITY_2021_3_20 || UNITY_2021_3_21 || UNITY_2021_3_22 || UNITY_2021_3_23 || UNITY_2021_3_24 || UNITY_2021_3_25
#elif UNITY_2021_3
#define UNITY_2021_3_26_OR_NEWER
#endif

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
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
    internal static class IPackageVersionExtensions
    {
        public static PackageInfo GetPackageInfo(this IPackageVersion self)
        {
            return self is UpmPackageVersionEx ex
                ? ex.packageInfo
                : PackageInfo.FindForAssetPath($"Packages/{self.name}");
        }
    }

    internal static class UpmPackageExtensions
    {
        public static UpmPackage UpdateVersionsSafety(this UpmPackage self, IEnumerable<UpmPackageVersion> versions)
        {
#if UNITY_2023_1_OR_NEWER
            var factory = UnityEditor.ScriptableSingleton<ServicesContainer>.instance.Resolve<UpmPackageFactory>();
            self = factory.CreatePackage(self.name, new UpmVersionList(versions.OrderBy(v => v.version)));
#elif UNITY_2022_2_OR_NEWER || UNITY_2021_3_26_OR_NEWER
            self = new UpmPackage(self.uniqueId, true, new UpmVersionList(versions.OrderBy(v => v.version)));
#else
            if (self.Has("UpdateVersions", versions, 0))
            {
                self.Call("UpdateVersions", versions, 0);
            }
            else if (self.Has("UpdateVersions", versions))
            {
                self.Call("UpdateVersions", versions);
            }
            else
            {
                throw new System.NotImplementedException(
                    "void UpmPackage.UpdateVersions(IEnumerable<UpmPackageVersion>, int) or void UpmPackage.UpdateVersions(IEnumerable<UpmPackageVersion>) is not found");
            }
#endif

            return self;
        }
    }

    internal static class PackageExtensions
    {
        private static readonly Regex kRegexPackageId = new Regex("^([^@]+)@([^#]+)(#(.+))?$", RegexOptions.Compiled);
        private static readonly Regex kRegexScpToSsh = new Regex("^(git@[^/]+):", RegexOptions.Compiled);

        public static string GetSourceUrl(this PackageInfo self)
        {
            return GetSourceUrl(kRegexPackageId.Replace(self?.packageId, "$2$3"));
        }

        public static string GetSourceUrl(string url)
        {
            return kRegexScpToSsh.Replace(url, "ssh://$1/");
        }

        public static string GetRepositoryUrlForBrowser(this PackageInfo self)
        {
            return GetRepoUrl(self?.packageId, true);
        }

        private static string GetRepoUrl(string packageId, bool https = false)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return "";
            }

            var m = kRegexPackageId.Match(packageId);
            if (!m.Success)
            {
                return "";
            }

            var repoUrl = m.Groups[2].Value;
            if (https)
            {
                repoUrl = Regex.Replace(repoUrl, "^git\\+", "");
                repoUrl = Regex.Replace(repoUrl, "(git:)?git@([^:]+):", "https://$2/");
                repoUrl = repoUrl.Replace("ssh://", "https://");
                repoUrl = repoUrl.Replace("git@", "");
                repoUrl = Regex.Replace(repoUrl, "\\.git$", "");
            }

            return repoUrl;
        }

        //public static string GetRepositoryUrl(this PackageInfo self)
        //{
        //    switch (self?.source)
        //    {
        //        case PackageSource.Embedded:
        //        case PackageSource.Local:
        //        case PackageSource.LocalTarball:
        //            var url = self.repository?.url;
        //            return string.IsNullOrEmpty(url)
        //                ? null
        //                : Regex.Replace(url, "^git\\+", "");
        //        case PackageSource.Git:
        //            return kRegexPackageId.Replace(self?.packageId, "$2");
        //    }
        //    return null;
        //}

        public static void UnlockVersion(this UpmPackageVersion self)
        {
            var tag = (PackageTag)self.Get("m_Tag") & ~PackageTag.VersionLocked;
            self.Set("m_Tag", tag);
        }

        public static UpmPackageVersion GetInstalledVersion(this UpmPackage self)
        {
#if UNITY_2020_1_OR_NEWER
            return self.versions?.installed as UpmPackageVersion;
#else
            return self.versions.FirstOrDefault(v => v.isInstalled) as UpmPackageVersion;
#endif
        }
    }
}
