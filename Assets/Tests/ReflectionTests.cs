#if UNITY_2020_1_OR_NEWER
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;

#else
using UnityEditor.PackageManager.UI;
#endif

namespace Coffee.UpmGitExtension
{
    public class ReflectionTests
    {
#if !UNITY_2023_1_OR_NEWER
        [Test]
        public void PackageDatabase_OnPackagesChanged()
        {
            Assert.True(
                GitPackageDatabase._packageDatabase.Has("OnPackagesChanged", Enumerable.Empty<IPackage>()),
                "void PackageDatabase.OnPackageChanged(IEnumerable<IPackage>) is not found"
            );
        }
#endif

        [Test]
        public void UpmPackageVersion_Tag()
        {
            Assert.True(
                JsonUtility.FromJson<UpmPackageVersion>("{}").Has<PackageTag>("m_Tag"),
                "PackageTag UpmPackageVersion.m_Tag is not found"
            );
        }

#if UNITY_2023_1_OR_NEWER
#elif UNITY_2021_2_OR_NEWER
        [Test]
        public void PackageDetailsLinks_AddToLinks()
        {
            Assert.True(
                new PackageDetailsLinks().Has("AddToLinks", new VisualElement(), new Button(), true),
                "void PackageDetailsLinks.AddToLink is not found"
            );
        }
#else
        [Test]
        public void PackageDetails_AddToLinks()
        {
            Assert.True(
                new PackageDetails().Has("AddToLinks", new Button()),
                "void PackageDetails.AddToLink is not found"
            );
        }
#endif

        [Test]
        public void Clickable_Invoke()
        {
            Assert.True(
                new Clickable(() => { }).Has("Invoke", new MouseDownEvent()),
                "void Clickable.Invoke(BaseEvent) is not found"
            );
        }
    }
}
#endif
