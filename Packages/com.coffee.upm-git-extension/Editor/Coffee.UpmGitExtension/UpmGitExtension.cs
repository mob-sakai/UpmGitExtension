#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
using UnityEditor;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.UpmGitExtension
{
    [InitializeOnLoad]
    internal class UpmGitExtension : VisualElement, IPackageManagerExtension
    {
        //################################
        // Constant or Static Members.
        //################################
        const string kHeader = "<b><color=#c7634c>[UpmGitExtension]</color></b> ";
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
            if (packageInfo == null)
                return;

            InitializeUI();
            packageDetailsExtension?.OnPackageSelectionChange(packageInfo);
        }

        //################################
        // Private Members.
        //################################
        VisualElement root;
        bool initialized;
        PackageDetailsExtension packageDetailsExtension;

        /// <summary>
        /// Initializes UI.
        /// </summary>
        void InitializeUI()
        {
            if (initialized || !InstallPackageWindow.IsResourceReady() || !GitButton.IsResourceReady())
                return;

            initialized = true;

            Debug.Log(kHeader, "[InitializeUI]");
            root = this.GetRoot().Q<TemplateContainer>("");

            Debug.Log(kHeader, "[InitializeUI] Setup internal bridge:");
            var internalBridge = Bridge.Instance;
            internalBridge.Setup(root);

            Debug.Log(kHeader, "[InitializeUI] Setup PackageDetails extension:");
            packageDetailsExtension = new PackageDetailsExtension();
            packageDetailsExtension.Setup(root);

            // Install package window.
            Debug.Log(kHeader, "[InitializeUI] Setup install window:");
            var installPackageWindow = new InstallPackageWindow();
            root.Add(installPackageWindow);

            // Add button to open InstallPackageWindow
            Debug.Log(kHeader, "[InitializeUI] Add button to open install window:");
            var addButton = root.Q("toolbarAddMenu") ?? root.Q("toolbarAddButton") ?? root.Q("moreAddOptionsButton");
            var gitButton = new GitButton(installPackageWindow.Open);
            addButton.parent.Insert(0, gitButton);
            gitButton.style.borderRightWidth = 1;

#if UNITY_2018
            var space = new VisualElement();
            space.style.flexGrow = 1;
            addButton.parent.Insert(addButton.parent.IndexOf(addButton), space);
#endif

        }
    }
}
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.