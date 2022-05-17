using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
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

        public UpmPackageVersionEx(UnityEditor.PackageManager.PackageInfo packageInfo, bool isInstalled, bool isUnityPackage) : base(packageInfo, isInstalled, isUnityPackage)
        {
        }

        public UpmPackageVersionEx(UnityEditor.PackageManager.PackageInfo packageInfo, bool isInstalled, SemVersion? version, string displayName, bool isUnityPackage) : base(packageInfo, isInstalled, version, displayName, isUnityPackage)
        {
        }

        public UpmPackageVersionEx(UpmPackageVersion packageVersion) : base(packageVersion.packageInfo, packageVersion.isInstalled, packageVersion.isUnityPackage)
        {
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
            PackageTag tag = PackageTag.Git | PackageTag.Installable | PackageTag.Removable;
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