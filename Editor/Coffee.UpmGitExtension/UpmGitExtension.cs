using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine.UIElements;

[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("Unity.InternalAPIEngineBridgeDev.001")]

namespace Coffee.UpmGitExtension
{
    internal class UpmGitExtension : VisualElement, IPackageManagerExtension
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            var ext = new UpmGitExtension();
            PackageManagerExtensions.RegisterExtension(ext as IPackageManagerExtension);
        }

        //################################
        // IPackageManagerExtension Members.
        //################################
        VisualElement IPackageManagerExtension.CreateExtensionUI()
        {
            _initialized = false;
            return this;
        }

        void IPackageManagerExtension.OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
        }

        void IPackageManagerExtension.OnPackageRemoved(PackageInfo packageInfo)
        {
        }

        void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == null) return;

            Initialize();
            _packageDetailsExtension?.OnPackageSelectionChange(packageInfo);
        }

        //################################
        // Private Members.
        //################################
        private bool _initialized;
        private PackageDetailsExtension _packageDetailsExtension;
        private GitPackageInstallationWindow _installationWindow;

        private void Initialize()
        {
            if (_initialized || !GitPackageInstallationWindow.IsResourceReady() || !GitButton.IsResourceReady()) return;

            _initialized = true;

            // Find root element.
            var root = this.GetRoot().Q<TemplateContainer>();

            // Setup PackageDetails extension.
            _packageDetailsExtension = new PackageDetailsExtension();
            _packageDetailsExtension.Setup(root);

            // Setup GitPackageInstallationWindow.
            _installationWindow = new GitPackageInstallationWindow();
            root.Add(_installationWindow);

            // Add button to open GitPackageInstallationWindow
            var gitButton = new GitButton(_installationWindow.Open);
            gitButton.style.borderRightWidth = 0;
            var addButton = root.Q("toolbarAddMenu");
            addButton.parent.Insert(0, gitButton);
        }
    }
}
