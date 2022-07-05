#if !UNITY_2021_2_OR_NEWER
#define SUPPORT_MENU_EXTENSIONS
#endif
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine.UIElements;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#endif

// For tests
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
            menu.AppendAction("Open manifest.json", _ => OpenManifestJson(), DropdownMenuAction.Status.Normal);
            menu.AppendAction("UpmGitExtensions/Open cache directory", _ => GitPackageDatabase.OpenCacheDirectory(), DropdownMenuAction.Status.Normal);
            menu.AppendAction("UpmGitExtensions/Clear cache", _ => GitPackageDatabase.ClearCache(), DropdownMenuAction.Status.Normal);
            menu.AppendAction("UpmGitExtensions/Fetch packages", _ => GitPackageDatabase.Fetch(), DropdownMenuAction.Status.Normal);
        }

        void IPackageManagerMenuExtensions.OnAddMenuCreate(DropdownMenu menu)
        {
        }

        void IPackageManagerMenuExtensions.OnFilterMenuCreate(DropdownMenu menu)
        {
        }
#else
        /// <summary>
        /// Setup toolbar.
        /// </summary>
        private void OnPackageManagerToolbarSetup(PackageManagerToolbar toolbar)
        {
            var menuDropdownItem = toolbar.toolbarSettingsMenu.AddBuiltInDropdownItem();
            menuDropdownItem.text = "Open manifest.json";
            menuDropdownItem.action = OpenManifestJson;

            var openCacheMenuItem = toolbar.toolbarSettingsMenu.AddBuiltInDropdownItem();
            openCacheMenuItem.insertSeparatorBefore = true;
            openCacheMenuItem.text = "UpmGitExtensions/Open cache directory";
            openCacheMenuItem.action = GitPackageDatabase.OpenCacheDirectory;

            var clearCacheMenuItem = toolbar.toolbarSettingsMenu.AddBuiltInDropdownItem();
            clearCacheMenuItem.text = "UpmGitExtensions/Clear cache";
            clearCacheMenuItem.action = GitPackageDatabase.ClearCache;

            var fetchPackagesMenuItem = toolbar.toolbarSettingsMenu.AddBuiltInDropdownItem();
            fetchPackagesMenuItem.text = "UpmGitExtensions/Fetch packages";
            fetchPackagesMenuItem.action = GitPackageDatabase.Fetch;
        }
#endif

        //################################
        // Private Members.
        //################################
        private bool _initialized;
        private PackageDetailsExtension _packageDetailsExtension;
        private GitPackageInstallationWindow _installationWindow;

        /// <summary>
        /// Open manifest.json in current project.
        /// </summary>
        private void OpenManifestJson()
        {
            // json files will be opend with code editor.
            var extensions = EditorSettings.projectGenerationUserExtensions;
            if (!extensions.Contains("json"))
            {
                EditorSettings.projectGenerationUserExtensions = extensions.Concat(new[] { "json" }).ToArray();
                AssetDatabase.SaveAssets();
            }

            // Open manifest.json with current code editor.
            Unity.CodeEditor.CodeEditor.CurrentEditor.OpenProject(Path.GetFullPath("./Packages/manifest.json"));
        }

        /// <summary>
        /// Initialize.
        /// </summary>
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
            var refleshButton = root.Q<VisualElement>("refreshButton").Get("clickable") as Clickable;
            refleshButton.clicked += GitPackageDatabase.ResetCacheTime;
        }
    }
}
