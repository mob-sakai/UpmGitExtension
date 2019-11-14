using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.PackageManager
{
    [InitializeOnLoad]
    internal class UpmGitExtension : VisualElement, IPackageManagerExtension
    {
        //################################
        // Constant or Static Members.
        //################################
        static UpmGitExtension()
        {
            PackageManagerExtensions.RegisterExtension(new UpmGitExtension());
        }

        //################################
        // Public Members.
        //################################
        /// <summary>
        /// Creates the extension UI visual element.
        /// </summary>
        /// <returns>A visual element that represents the UI or null if none</returns>
        public VisualElement CreateExtensionUI()
        {
			initialized = false;
            return this;
        }

        /// <summary>
        /// Called by the Package Manager UI when a package is added or updated.
        /// </summary>
        /// <param name="packageInfo">The package information</param>
        public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
        }

        /// <summary>
        /// Called by the Package Manager UI when a package is removed.
        /// </summary>
        /// <param name="packageInfo">The package information</param>
        public void OnPackageRemoved(PackageInfo packageInfo)
        {
        }

        /// <summary>
        /// Called by the Package Manager UI when the package selection changed.
        /// </summary>
        /// <param name="packageInfo">The newly selected package information (can be null)</param>
        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            InitializeUI();
            if (!initialized || packageInfo == null)
                return;

            // Update document actions.

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


            var host = Settings.GetHostData(packageInfo.packageId);
            var hostButton = root.Q<Button>("hostButton");
            hostButton.style.backgroundImage = host.Logo;
            hostButton.visible = packageInfo.source == PackageSource.Git;
        }

        //################################
        // Private Static Members.
        //################################



        //################################
        // Private Members.
        //################################
        VisualElement root;
		bool initialized;



        /// <summary>
        /// Initializes UI.
        /// </summary>
        void InitializeUI()
        {
            if (initialized || !InstallPackageWindow.IsResourceReady() || !GitButton.IsResourceReady())
                return;

			initialized = true;


            root = UIUtils.GetRoot(this).Q("container");

            var internalBridge = InternalBridge.Instance;
            internalBridge.Setup(root.Q("packageSpinner"), root.Q("packageList"), root.Q("detailsGroup"));


            // Document actions.
            root.Q<Button>("viewDocumentation").OverwriteCallback(()=>internalBridge.ViewDocClick(MarkdownUtils.OpenInBrowser));
            root.Q<Button>("viewChangelog").OverwriteCallback(()=>internalBridge.ViewChangelogClick(MarkdownUtils.OpenInBrowser));
            root.Q<Button>("viewLicenses").OverwriteCallback(()=>internalBridge.ViewLicensesClick(MarkdownUtils.OpenInBrowser));

            var hostButton = root.Q<Button>("hostButton");
            if (hostButton == null)
            {
                hostButton = new Button(internalBridge.ViewRepoClick) { name = "hostButton", tooltip = "View on browser" };
                hostButton.RemoveFromClassList("unity-button");
                hostButton.RemoveFromClassList("button");
                hostButton.AddToClassList("link");
				hostButton.style.marginRight = 2;
				hostButton.style.marginLeft = 2;

#if !UNITY_2019_1_OR_NEWER
				hostButton.style.sliceBottom = 0;
				hostButton.style.sliceTop = 0;
				hostButton.style.sliceRight = 0;
				hostButton.style.sliceLeft = 0;
				#endif

				hostButton.style.width = 16;
				hostButton.style.height = 16;
                root.Q("detailVersion").parent.Add(hostButton);
            }

            // Install package window.
            var installPackageWindow = new InstallPackageWindow();
            root.Add(installPackageWindow);

            // Add button to open InstallPackageWindow
            var addButton = root.Q("toolbarAddButton") ?? root.Q("moreAddOptionsButton");
            var gitButton = new GitButton(installPackageWindow.Open);
            addButton.parent.Insert(addButton.parent.IndexOf(addButton) + 1, gitButton);

#if UNITY_2018
            var space = new VisualElement();
            space.style.flexGrow = 1;
            addButton.parent.Insert(addButton.parent.IndexOf(addButton), space);
#endif


#if UNITY_2019_1_OR_NEWER
			var updateButton = root.Q("packageToolBar").Q<Button>("update");
#else
            // OnPackageListLoaded ();
            internalBridge.UpdateGitPackages();
            var updateButton = root.Q("updateCombo").Q<Button>("update");
#endif

            var detailView = Expose.FromObject(root.Q("detailsGroup"));
            var removeButton = root.Q<Button>("remove");


            updateButton.OverwriteCallback(internalBridge.UpdateClick);
            removeButton.OverwriteCallback(internalBridge.RemoveClick);

        }

    }
}
