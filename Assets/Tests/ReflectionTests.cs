using System.Collections;
using System.Collections.Generic;
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
        [Test]
        public void PackageDatabase_OnPackagesChanged()
        {
            Assert.True(
                GitPackageDatabase._packageDatabase.Has("OnPackagesChanged", new object[] { Enumerable.Empty<IPackage>() }),
                "void PackageDatabase.OnPackageChanged(IEnumerable<IPackage>) is not found"
            );
        }

        [Test]
        public void UpmPackageVersion_Tag()
        {
            Assert.True(
                JsonUtility.FromJson<UpmPackageVersion>("{}").Has<PackageTag>("m_Tag"),
                "PackageTag UpmPackageVersion.m_Tag is not found"
            );
        }
#if UNITY_2021_2_OR_NEWER
        [Test]
        public void PackageDetailsLinks_AddToLinks()
        {
            Assert.True(
                new PackageDetailsLinks().Has("AddToLinks", new object[] { new VisualElement(), new Button(), true }),
                "void PackageDetailsLinks.AddToLink is not found"
            );
        }
#else
        [Test]
        public void PackageDetails_AddToLinks()
        {
            Assert.True(
                new PackageDetails().Has("AddToLinks", new object[] { new Button() }),
                "void PackageDetails.AddToLink is not found"
            );
        }
#endif

        [Test]
        public void Clickable_Invoke()
        {
            Assert.True(
                new Clickable(() => { }).Has("Invoke", new object[] { new MouseDownEvent() }),
                "void Clickable.Invoke(BaseEvent) is not found"
            );
        }
    }
}
