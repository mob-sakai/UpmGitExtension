#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Coffee.UpmGitExtension
{
    [Serializable]
    public class AvailableVersion : IEquatable<AvailableVersion>
    {
        public string packageName = "";
        public string version = "";
        public string refName = "";
        public string repoUrl = "";

        public string refNameText { get { return version == refName ? version : version + " - " + refName; } }
        public string refNameVersion { get { return version == refName ? version : version + "-" + refName; } }

        bool IEquatable<AvailableVersion>.Equals(AvailableVersion other)
        {
            return other != null
                && packageName == other.packageName
                && version == other.version
                && repoUrl == other.repoUrl
                && refName == other.refName;
        }

        public override int GetHashCode()
        {
            return packageName.GetHashCode()
                + version.GetHashCode()
                + repoUrl.GetHashCode()
                + refName.GetHashCode();
        }
    }

    [Serializable]
    public class ResultInfo
    {
        public AvailableVersion[] versions;
    }

    public class AvailableVersions : ScriptableSingleton<AvailableVersions>
    {
        const string kPackageDir = "Library/UGE/packages";

        public AvailableVersion[] versions = new AvailableVersion[0];
        
        public static event Action OnChanged = () => { };

        public static void ClearAll()
        {
            instance.versions = new AvailableVersion[0];

            if (Directory.Exists(kPackageDir))
                Directory.Delete(kPackageDir, true);
        }

        public static void Clear(string packageName = null, string repoUrl = null)
        {
            instance.versions = instance.versions
                .Where(x => string.IsNullOrEmpty(packageName) || x.packageName != packageName)
                .Where(x => string.IsNullOrEmpty(repoUrl) || x.repoUrl != repoUrl)
                .ToArray();
        }

        public static IEnumerable<AvailableVersion> GetVersions(string packageName = null, string repoUrl = null)
        {
            return instance.versions
                .Where(x => string.IsNullOrEmpty(packageName) || x.packageName == packageName)
                .Where(x => string.IsNullOrEmpty(repoUrl) || x.repoUrl == repoUrl);
        }

        public static void Dump()
        {
            var sb = new StringBuilder("[AvailableVersions] Dump:\n");
            foreach(var v in instance.versions.OrderBy(x=>x.packageName).ThenBy(x=>x.version))
            {
                sb.AppendLine(JsonUtility.ToJson(v));
            }
            UnityEngine.Debug.Log(sb);
        }

        public static void AddVersions(IEnumerable<AvailableVersion> add)
        {
            if (add == null || !add.Any())
                return;

            var length = instance.versions.Length;
            var versions = instance.versions
                .Union(add)
                .ToArray();

            if (versions.Length != length)
            {
                instance.versions = versions;
                OnChanged();
            }
        }
    }
}
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.