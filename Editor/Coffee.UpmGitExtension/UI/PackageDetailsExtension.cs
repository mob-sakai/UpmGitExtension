#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Coffee.UpmGitExtension
{
    internal class PackageDetailsExtension
    {
        //################################
        // Constant or Static Members.
        //################################
        const string kHeader = "<b><color=#c7634c>[PackageDetailsExtension]</color></b> ";

        //################################
        // Public Members.
        //################################
        public void Setup(VisualElement root)
        {
            this.root = root;
            packageDetails = root.Q<PackageDetails>();

            Debug.Log(kHeader, "[InitializeUI] Setup host button:");
            var hostButton = packageDetails.Q<Button>("hostButton");
            if (hostButton == null)
            {
                hostButton = new Button(ViewRepoClick) { name = "hostButton", tooltip = "View on browser" };
                hostButton.RemoveFromClassList("unity-button");
                hostButton.RemoveFromClassList("button");
                hostButton.AddToClassList("link");
                hostButton.style.marginRight = 2;
                hostButton.style.marginLeft = 2;
                hostButton.style.width = 16;
                hostButton.style.height = 16;
                root.Q("detailVersion").parent.Add(hostButton);

#if !UNITY_2019_1_OR_NEWER
                hostButton.style.sliceBottom = 0;
                hostButton.style.sliceTop = 0;
                hostButton.style.sliceRight = 0;
                hostButton.style.sliceLeft = 0;
#endif
            }


#if UNITY_2018
            Debug.Log(kHeader, "[InitializeUI] Setup document actions:");
            packageDetails.Q<Button>("viewDocumentation").OverwriteCallback(ViewDocClick);
            packageDetails.Q<Button>("viewChangelog").OverwriteCallback(ViewChangelogClick);
            packageDetails.Q<Button>("viewLicenses").OverwriteCallback(ViewLicensesClick);
#endif

            Debug.Log(kHeader, "[InitializeUI] Setup update button:");
            var updateButton = packageDetails.Q<Button>("update");
            updateButton.OverwriteCallback(UpdateClick);

            Debug.Log(kHeader, "[InitializeUI] Setup remove button:");
            var removeButton = packageDetails.Q<Button>("remove");
            removeButton.OverwriteCallback(RemoveClick);
        }


        /// <summary>
        /// Called by the Package Manager UI when the package selection changed.
        /// </summary>
        /// <param name="packageInfo">The newly selected package information (can be null)</param>
        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return;

            Debug.Log(kHeader, $"OnPackageSelectionChange {packageInfo.packageId}");
            if (packageInfo.source == PackageSource.Git)
            {
                // Show remove button for git package.
                var removeButton = root.Q<Button>("remove");
                UIUtils.SetElementDisplay(removeButton, true);
                removeButton.SetEnabled(true);

                // Show git tag.
                var tagGit = root.Q("tag-git");
                UIUtils.SetElementDisplay(tagGit, true);
            }

            // Show hosting service logo.
            var hostButton = root.Q<Button>("hostButton");
            if (hostButton != null)
            {
                hostButton.style.backgroundImage = GetHostLogo(packageInfo.packageId);
                hostButton.visible = packageInfo.source == PackageSource.Git;
            }
        }


        //################################
        // Private Members.
        //################################
        VisualElement root;
        PackageDetails packageDetails;

#if UNITY_2019_3_OR_NEWER
        PackageInfo GetSelectedPackage() { return GetSelectedVersion().packageInfo; }
        UpmPackageVersion GetSelectedVersion() { return packageDetails.targetVersion as UpmPackageVersion; }
#elif UNITY_2019_1_OR_NEWER
        PackageInfo GetSelectedPackage() { return GetSelectedVersion().Info; }
        UnityEditor.PackageManager.UI.PackageInfo GetSelectedVersion() { return packageDetails.TargetVersion; }
#else
        PackageInfo GetSelectedPackage() { return GetSelectedVersion().Info; }
        UnityEditor.PackageManager.UI.PackageInfo GetSelectedVersion() { return packageDetails.SelectedPackage; }
#endif

        /// <summary>
        /// Get host logo.
        /// </summary>
        public Texture2D GetHostLogo(string packageId)
        {
            const string packageDir = "Packages/com.coffee.upm-git-extension/Editor/Resources/Logos/";
            if (packageId.Contains("github.com/"))
                return EditorGUIUtility.isProSkin
                    ? AssetDatabase.LoadMainAssetAtPath(packageDir + "GitHub-Logo-Light.png") as Texture2D
                    : AssetDatabase.LoadMainAssetAtPath(packageDir + "GitHub-Logo-Dark.png") as Texture2D;
            else if (packageId.Contains("bitbucket.org/"))
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "Bitbucket-Logo.png") as Texture2D;
            else if (packageId.Contains("gitlab.com/"))
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "GitLab-Logo.png") as Texture2D;
            else if (packageId.Contains("azure.com/"))
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "AzureRepos-Logo.png") as Texture2D;

            return EditorGUIUtility.isProSkin
                ? EditorGUIUtility.FindTexture("d_buildsettings.web.small")
                : EditorGUIUtility.FindTexture("buildsettings.web.small");
        }

        /// <summary>
        /// On click 'Update package' callback.
        /// </summary>
        public void UpdateClick()
        {
            Debug.Log(kHeader, "[UpdateClick]");
            var selectedPackage = GetSelectedPackage();
            if (selectedPackage.source == PackageSource.Git)
            {
                string packageId = selectedPackage.packageId;
                string url = PackageUtils.GetRepoUrl(packageId);
#if UNITY_2019_3_OR_NEWER
                string refName = GetSelectedVersion().packageInfo.git.revision;
#else
                string refName = GetSelectedVersion().VersionId.Split('@')[1];
                var originRefName = refName;

                // Find correct reference (branch or tag) name.
                while(!AvailableVersions.GetVersions(selectedPackage.name, url).Any(x=>x.refName == refName))
                {
                    var index = refName.IndexOf('-');
                    if(index < 0 || refName.Length < 1)
                        throw new Exception($"Cannot install '{packageId}'. The branch or tag is not found in repository.");
                    refName = refName.Substring(index+1);
                }
#endif
                PackageUtils.UninstallPackage(selectedPackage.name);
                PackageUtils.InstallPackage(selectedPackage.name, url, refName);
            }
            else
            {
                packageDetails.UpdateClick();
            }
        }

        /// <summary>
        /// On click 'Remove package' callback.
        /// </summary>
        public void RemoveClick()
        {
            Debug.Log(kHeader, "[RemoveClick]");
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
        /// On click 'View repository' callback.
        /// </summary>
        public void ViewRepoClick()
        {
            Application.OpenURL(PackageUtils.GetRepoUrl(GetSelectedPackage().packageId, true));
        }

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
            packageDetails.ViewDocClick();
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
            packageDetails.ViewChangelogClick();
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
            packageDetails.ViewLicensesClick();
        }
#endif
    }
}
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.