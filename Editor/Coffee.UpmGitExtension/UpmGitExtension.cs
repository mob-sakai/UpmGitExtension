#if !UNITY_2021_2_OR_NEWER
#define SUPPORT_MENU_EXTENSIONS
#endif
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine.UIElements;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("Unity.InternalAPIEngineBridgeDev.001")]

namespace Coffee.UpmGitExtension
{
    internal class UpmGitExtension : VisualElement, IPackageManagerExtension
#if SUPPORT_MENU_EXTENSIONS
    , IPackageManagerMenuExtensions
#endif
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            var ext = new UpmGitExtension();
            PackageManagerExtensions.RegisterExtension(ext as IPackageManagerExtension);
#if SUPPORT_MENU_EXTENSIONS
            PackageManagerExtensions.RegisterExtension(ext as IPackageManagerMenuExtensions);
#endif
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

#if SUPPORT_MENU_EXTENSIONS
        void IPackageManagerMenuExtensions.OnAdvancedMenuCreate(DropdownMenu menu)
        {
            menu.AppendAction("Open manifest.json", _ => Unity.CodeEditor.CodeEditor.CurrentEditor.OpenProject(Path.GetFullPath( "Packages/manifest.json")), DropdownMenuAction.Status.Normal);
        }

        void IPackageManagerMenuExtensions.OnAddMenuCreate(DropdownMenu menu)
        {
        }

        void IPackageManagerMenuExtensions.OnFilterMenuCreate(DropdownMenu menu)
        {
        }
#else
        void OnPackageManagerToolbarSetup(PackageManagerToolbar toolbar)
        {
            MenuDropdownItem menuDropdownItem = toolbar.toolbarSettingsMenu.AddBuiltInDropdownItem();
            menuDropdownItem.text = "Open manifest.json";
            menuDropdownItem.action = () => Unity.CodeEditor.CodeEditor.CurrentEditor.OpenProject(Path.GetFullPath("./Packages/manifest.json"));
        }
#endif

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

#if !SUPPORT_MENU_EXTENSIONS
            // Setup toolbar menus
            OnPackageManagerToolbarSetup(root.Q<PackageManagerToolbar>());
#endif
        }
    }
}
