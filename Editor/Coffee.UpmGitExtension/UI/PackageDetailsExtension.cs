using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#else
using UnityEditor.PackageManager.UI;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Coffee.UpmGitExtension
{
    internal class PackageDetailsExtension
    {
        //################################
        // Public Members.
        //################################
        public void Setup(VisualElement root)
        {
            _root = root;
            _packageDetails = root.Q<PackageDetails>();

            var hostButton = _packageDetails.Q<Button>("hostButton");
            if (hostButton == null)
            {
                hostButton = new Button(ViewRepoOnBrowser) { name = "hostButton", tooltip = "View on browser" };
                hostButton.RemoveFromClassList("unity-button");
                hostButton.RemoveFromClassList("button");
                hostButton.AddToClassList("link");
                hostButton.style.marginRight = 2;
                hostButton.style.marginLeft = 2;
                hostButton.style.width = 16;
                hostButton.style.height = 16;

                root.Q("detailVersion").parent.Add(hostButton);
            }

            // Update/Install button.
            _updateButton = _packageDetails.Q<Button>("PackageAddButton") ?? _packageDetails.Q<Button>("update");
            if (_clickableToUpdate == null)
            {
                _clickableToUpdate = _updateButton.clickable;
                _updateButton.RemoveManipulator(_updateButton.clickable);
                _updateButton.clickable = new Clickable(UpdatePackage);
                _updateButton.AddManipulator(_updateButton.clickable);
            }

#if !UNITY_2020_2_OR_NEWER
            var detailSourcePathContainer = _packageDetails.Q("detailSourcePathContainer");
            if (detailSourcePathContainer == null)
            {
                var upmGitExtension = _packageDetails.Q<UpmGitExtension>();
                upmGitExtension.Add(new Label("Installed From") { name = "detailSourcePathHeader", style = { unityFontStyleAndWeight = FontStyle.Bold } });
                upmGitExtension.Add(new Label() { name = "detailSourcePath" });
            }
#endif

            // Register callbacks.
            EditorApplication.delayCall += () =>
            {
#if UNITY_2021_2_OR_NEWER || UNITY_2021_1_20 || UNITY_2021_1_21 || UNITY_2021_1_22 || UNITY_2021_1_23 || UNITY_2021_1_24 || UNITY_2021_1_25 || UNITY_2021_1_26 || UNITY_2021_1_27 || UNITY_2021_1_28
                _pageManager.onVisualStateChange += _ => EditorApplication.delayCall += RefleshVersionItems;
                _pageManager.onListUpdate += _ => EditorApplication.delayCall += RefleshVersionItems;
#else
                _pageManager.onVisualStateChange += _ => RefleshVersionItems();
                _pageManager.onListUpdate += (_, __, ___, ____) => RefleshVersionItems();
#endif
            };
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == null) return;

            // var _current = packageInfo;
            bool isGit = packageInfo.source == PackageSource.Git;

            // Show/hide hosting service logo.
            var hostButton = _packageDetails.Q<Button>("hostButton");
            if (hostButton != null)
            {
                hostButton.style.backgroundImage = GetHostLogo(packageInfo.packageId);
                UIUtils.SetElementDisplay(hostButton, isGit);
            }

#if !UNITY_2020_2_OR_NEWER
            // Show/hide source path.
            var detailSourcePath = _packageDetails.Q<Label>("detailSourcePath");
            if (detailSourcePath != null)
            {
                detailSourcePath.text = packageInfo.GetSourceUrl();
                UIUtils.SetElementDisplay(_packageDetails.Q<UpmGitExtension>(), isGit);
            }
#endif

            if (isGit)
            {
                var button = new Button(ViewRepoOnBrowser) { text = "View repository" };
                button.AddClasses("link");

#if UNITY_2021_2_OR_NEWER
                var links = _packageDetails.Q<PackageDetailsLinks>();
                var left = links.Q(classes: new[] { "left" });
                links.Call("AddToLinks", left, button, true);
#else
                _packageDetails.Call("AddToLinks", button);
#endif

                _targetVersion = null;
                var packageVersion = GitPackageDatabase.GetAvailablePackageVersions(preRelease: true).FirstOrDefault(v => v.packageInfo.packageId == packageInfo.packageId);
                if (packageVersion != null)
                {
                    var package = GitPackageDatabase.GetPackage(packageVersion);
                    _targetVersion = package?.versions?.installed?.uniqueId == packageInfo.packageId ? package.versions.recommended : packageVersion;
                    if (_targetVersion != null)
                    {
                        _updateButton.text = _updateButton.text.Replace(_targetVersion.version.ToString(), _targetVersion.versionString);
                    }
                }
                else
                {
                    var package = GitPackageDatabase.GetPackage(packageInfo.name);
                    _targetVersion = package?.versions?.installed != null ? package?.versions?.recommended : package?.versions?.primary;
                }

#if UNITY_2022_2_OR_NEWER
                RefleshVersionItems();
#endif
            }
        }


        //################################
        // Private Members.
        //################################
        private PackageDetails _packageDetails;
        private IPackageVersion _targetVersion;
        private Clickable _clickableToUpdate;
        private Button _updateButton;
        private VisualElement _root;
#if UNITY_2020_2_OR_NEWER
        private static PageManager _pageManager => ScriptableSingleton<ServicesContainer>.instance.Resolve<PageManager>();
#else
        private static IPageManager _pageManager => PageManager.instance;
#endif

        /// <summary>
        /// Get hosting service logo.
        /// </summary>
        private Texture2D GetHostLogo(string packageId)
        {
            const string packageDir = "Packages/com.coffee.upm-git-extension/Editor/Resources/Logos/";
            if (packageId.Contains("github.com/"))
            {
                return EditorGUIUtility.isProSkin
                    ? AssetDatabase.LoadMainAssetAtPath(packageDir + "GitHub-Logo-Light.png") as Texture2D
                    : AssetDatabase.LoadMainAssetAtPath(packageDir + "GitHub-Logo-Dark.png") as Texture2D;
            }
            else if (packageId.Contains("bitbucket.org/"))
            {
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "Bitbucket-Logo.png") as Texture2D;
            }
            else if (packageId.Contains("gitlab.com/"))
            {
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "GitLab-Logo.png") as Texture2D;
            }
            else if (packageId.Contains("azure.com/"))
            {
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "AzureRepos-Logo.png") as Texture2D;
            }

            return EditorGUIUtility.isProSkin
                ? EditorGUIUtility.FindTexture("d_buildsettings.web.small")
                : EditorGUIUtility.FindTexture("buildsettings.web.small");
        }

        /// <summary>
        /// On click 'View repository' callback.
        /// </summary>
        private void ViewRepoOnBrowser()
        {
            if (_targetVersion != null)
            {
                Application.OpenURL(_targetVersion.packageInfo.GetRepositoryUrlForBrowser());
            }
        }

        private void UpdatePackage()
        {
            if (_targetVersion?.packageInfo?.source == PackageSource.Git)
                GitPackageDatabase.Install(_targetVersion.packageInfo.packageId);
            else
                _clickableToUpdate?.Call("Invoke", new MouseDownEvent());
        }

        private void RefleshVersionItems()
        {
#if UNITY_2022_2_OR_NEWER
            var items = _root.Query<PackageDetailsVersionHistoryItem>().Build()
                .Select(item => new { label = item.Q<Toggle>("versionHistoryItemToggle")?.Q<Label>(), version = item.version as UpmPackageVersionEx });
#else
            var items = _root.Query<PackageVersionItem>().Build().ToList()
                .Select(item => new { label = item.Q<Label>("versionLabel"), version = item.version as UpmPackageVersionEx });
#endif
            foreach(var item in items)
            {
                if (item.label != null && item.version != null)
                    item.label.text = item.version.fullVersionString;
            }
        }
    }
}
