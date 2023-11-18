using System.Collections.Generic;
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
        public static void UpdateVersionsSafety(this UpmPackage self, IEnumerable<UpmPackageVersion> updatedVersions)
        {
            if (self.Has("UpdateVersions", updatedVersions, 0))
            {
                self.Call("UpdateVersions", updatedVersions, 0);
            }
            else
            {
                self.Call("UpdateVersions", updatedVersions);
            }
        }
    }

    internal static class PackageExtensions
    {
        private static readonly Regex kRegexPackageId = new("^([^@]+)@([^#]+)(#(.+))?$", RegexOptions.Compiled);
        private static readonly Regex kRegexScpToSsh = new("^(git@[^/]+):", RegexOptions.Compiled);

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
