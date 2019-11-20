using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager.UI.InternalBridge;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Diagnostics;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.PackageManager
{
    public class Debug
    {
        [Conditional("DEBUG_UGE_DEVELOP")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("DEBUG_UGE_DEVELOP")]
        public static void LogFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }
    }

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
            var host = Settings.GetHostData(packageInfo.packageId);
            var hostButton = root.Q<Button>("hostButton");
            hostButton.style.backgroundImage = host.Logo;
            hostButton.visible = packageInfo.source == PackageSource.Git;
        }

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

            Debug.Log("[UpmGitExtension.InitializeUI]");
            root = UIUtils.GetRoot(this).Q<TemplateContainer>("");

            Debug.Log("[UpmGitExtension.InitializeUI] Setup internal bridge:");
            var internalBridge = Bridge.Instance;
            internalBridge.Setup(root);

            Debug.Log("[UpmGitExtension.InitializeUI] Setup host button:");
            var hostButton = root.Q<Button>("hostButton");
            if (hostButton == null)
            {
                hostButton = new Button(internalBridge.ViewRepoClick) { name = "hostButton", tooltip = "View on browser" };
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

            // Install package window.
            Debug.Log("[UpmGitExtension.InitializeUI] Setup install window:");
            var installPackageWindow = new InstallPackageWindow();
            root.Add(installPackageWindow);

            // Add button to open InstallPackageWindow
            Debug.Log("[UpmGitExtension.InitializeUI] Add button to open install window:");
            var addButton = root.Q("toolbarAddMenu") ?? root.Q("toolbarAddButton") ?? root.Q("moreAddOptionsButton");
            var gitButton = new GitButton(installPackageWindow.Open);
            addButton.parent.Insert(0, gitButton);

#if UNITY_2018
            var space = new VisualElement();
            space.style.flexGrow = 1;
            addButton.parent.Insert(addButton.parent.IndexOf(addButton), space);

            Debug.Log("[UpmGitExtension.InitializeUI] Setup document actions:");
            root.Q<Button>("viewDocumentation").OverwriteCallback(internalBridge.ViewDocClick);
            root.Q<Button>("viewChangelog").OverwriteCallback(internalBridge.ViewChangelogClick);
            root.Q<Button>("viewLicenses").OverwriteCallback(internalBridge.ViewLicensesClick);
#endif

            Debug.Log("[UpmGitExtension.InitializeUI] Setup update button:");
            var updateButton = root.Q<Button>("update");
            updateButton.OverwriteCallback(internalBridge.UpdateClick);

            Debug.Log("[UpmGitExtension.InitializeUI] Setup remove button:");
            var removeButton = root.Q<Button>("remove");
            removeButton.OverwriteCallback(internalBridge.RemoveClick);

            internalBridge.UpdateGitPackages();
        }
    }
}
