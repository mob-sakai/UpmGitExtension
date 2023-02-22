// IPackageVersion.packageInfo has been removed. (2020.3.41f1-, 2021.3.12f1-)
#if UNITY_2020_3_0 || UNITY_2020_3_1 || UNITY_2020_3_2 || UNITY_2020_3_3 || UNITY_2020_3_4 || UNITY_2020_3_5 || UNITY_2020_3_6 || UNITY_2020_3_7 || UNITY_2020_3_8  || UNITY_2020_3_9
#elif UNITY_2020_3_10 || UNITY_2020_3_11 || UNITY_2020_3_12 || UNITY_2020_3_13 || UNITY_2020_3_14 || UNITY_2020_3_15 || UNITY_2020_3_16 || UNITY_2020_3_17 || UNITY_2020_3_18  || UNITY_2020_3_19
#elif UNITY_2020_3_20 || UNITY_2020_3_21 || UNITY_2020_3_22 || UNITY_2020_3_23 || UNITY_2020_3_24 || UNITY_2020_3_25 || UNITY_2020_3_26 || UNITY_2020_3_27 || UNITY_2020_3_28  || UNITY_2020_3_29
#elif UNITY_2020_3_30 || UNITY_2020_3_31 || UNITY_2020_3_32 || UNITY_2020_3_33 || UNITY_2020_3_34 || UNITY_2020_3_35 || UNITY_2020_3_36 || UNITY_2020_3_37 || UNITY_2020_3_38  || UNITY_2020_3_39  || UNITY_2020_3_40
#elif UNITY_2021_3_0 || UNITY_2021_3_1 || UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5 || UNITY_2021_3_6 || UNITY_2021_3_7 || UNITY_2021_3_8  || UNITY_2021_3_9 || UNITY_2021_3_10 || UNITY_2021_3_11
#elif UNITY_2020_3 || UNITY_2021_3 || UNITY_2022_2_OR_NEWER
#define PACKAGE_INFO_HAS_BEEN_REMOVED
#endif
#if UNITY_2020_1_0 || UNITY_2020_1_1 || UNITY_2020_1_2 || UNITY_2020_1_3 || UNITY_2020_1_4
#define CONSTRACTOR_V1
#endif
    
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#else
using UnityEditor.PackageManager.UI;
#endif

namespace Coffee.UpmGitExtension
{
    [Serializable]
    internal class UpmPackageVersionEx : UpmPackageVersion
    {
        private static readonly Regex regex = new Regex("^(\\d +)\\.(\\d +)\\.(\\d +)(.*)$", RegexOptions.Compiled);
        private static SemVersion? unityVersion;

#if PACKAGE_INFO_HAS_BEEN_REMOVED
        [SerializeField]
        private UnityEditor.PackageManager.PackageInfo m_PackageInfo;
        public UnityEditor.PackageManager.PackageInfo packageInfo => m_PackageInfo;
#endif
        
        public UpmPackageVersionEx(UnityEditor.PackageManager.PackageInfo packageInfo, bool isInstalled, bool isUnityPackage)
#if CONSTRACTOR_V1
            : base(packageInfo, isInstalled)
#else
            : base(packageInfo, isInstalled, isUnityPackage)
#endif
        {
        }

        public UpmPackageVersionEx(UnityEditor.PackageManager.PackageInfo packageInfo, bool isInstalled, SemVersion? version, string displayName, bool isUnityPackage)
#if CONSTRACTOR_V1
            : base(packageInfo, isInstalled, version, displayName)
#else
            : base(packageInfo, isInstalled, version, displayName, isUnityPackage)
#endif
        {
        }

        public UpmPackageVersionEx(UpmPackageVersion packageVersion)
#if CONSTRACTOR_V1
            : base(packageVersion.GetPackageInfo(), packageVersion.isInstalled)
#else
            : base(packageVersion.GetPackageInfo(), packageVersion.isInstalled, packageVersion.isUnityPackage)
#endif
        {
#if PACKAGE_INFO_HAS_BEEN_REMOVED
            m_PackageInfo = packageVersion.GetPackageInfo();
#endif
            m_MinimumUnityVersion = UnityVersionToSemver(Application.unityVersion).ToString();
            OnAfterDeserialize();
        }

        public string fullVersionString { get; private set; }
        public SemVersion semVersion { get; private set; }

        [SerializeField]
        private string m_MinimumUnityVersion;

        public bool isValid { get; private set; }

        private static SemVersion UnityVersionToSemver(string version)
        {
            return SemVersionParser.Parse(regex.Replace(version, "$1.$2.$3+$4"));
        }

        public bool IsPreRelease()
        {
            return semVersion.Major == 0 || !string.IsNullOrEmpty(semVersion.Prerelease);
        }

        private void UpdateTag()
        {
#if UNITY_2023_1_OR_NEWER
            var tag = PackageTag.Git | PackageTag.UpmFormat;
#else
            var tag = PackageTag.Git | PackageTag.Installable | PackageTag.Removable;
#endif

            if (IsPreRelease())
            {
#if UNITY_2021_1_OR_NEWER
                tag |= PackageTag.PreRelease;
#else
                tag |= PackageTag.Preview;
#endif
            }
            else
                tag |= PackageTag.Release;

            this.Set("m_Tag", tag);
        }


        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            semVersion = m_Version ?? new SemVersion();
            var revision = packageInfo?.git?.revision ?? "";
            if (!revision.Contains(m_VersionString) && 0 < revision.Length)
            {
                fullVersionString = $"{m_Version} ({revision})";
            }
            else
            {
                fullVersionString = m_Version.ToString();
            }

            try
            {
                if (!unityVersion.HasValue)
                    unityVersion = UnityVersionToSemver(Application.unityVersion);
                var supportedUnityVersion = UnityVersionToSemver(m_MinimumUnityVersion);

                isValid = supportedUnityVersion <= unityVersion.Value;

                if (HasTag(PackageTag.Git))
                    UpdateTag();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

        }
    }
}