using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#else
using UnityEditor.PackageManager.UI;
#endif

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

#if! UNITY_6000_0_OR_NEWER
            // Update/Install button.
            _updateButton = _packageDetails.Q<Button>("PackageGitUpdateButton") ??
                            _packageDetails.Q<Button>("PackageAddButton") ?? _packageDetails.Q<Button>("update");
            if (_updateButton != null &&_clickableToUpdate == null)
            {
                _clickableToUpdate = _updateButton.clickable;
                _updateButton.RemoveManipulator(_updateButton.clickable);
                _updateButton.clickable = new Clickable(UpdatePackage);
                _updateButton.AddManipulator(_updateButton.clickable);
            }
#endif

            // Register callbacks.
            EditorApplication.delayCall += () =>
            {
#if UNITY_6000_0_OR_NEWER
                _pageManager.onVisualStateChange += _ => EditorApplication.delayCall += RefreshAllGitPackages;
                _pageManager.onListUpdate += _ => EditorApplication.delayCall += RefreshAllGitPackages;
#elif UNITY_2021_2_OR_NEWER || UNITY_2021_1_20 || UNITY_2021_1_21 || UNITY_2021_1_22 || UNITY_2021_1_23 || UNITY_2021_1_24 || UNITY_2021_1_25 || UNITY_2021_1_26 || UNITY_2021_1_27 || UNITY_2021_1_28
                _pageManager.onVisualStateChange += _ => EditorApplication.delayCall += RefreshVersionItems;
                _pageManager.onListUpdate += _ => EditorApplication.delayCall += RefreshVersionItems;
#else
                _pageManager.onVisualStateChange += _ => RefreshVersionItems();
                _pageManager.onListUpdate += (_, __, ___, ____) => RefreshVersionItems();
#endif
                
            };

            _root.Q<PackageDetailsVersionsTab>().RegisterCallback<GeometryChangedEvent>(ev =>
            {
                if ((ev.target as VisualElement).style.visibility.value == Visibility.Visible)
                {
#if UNITY_6000_0_OR_NEWER
                    RefreshAllGitPackages();
#else
                    RefreshVersionItems();
#endif
                }
            });
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == null) return;

            var isGit = packageInfo.source == PackageSource.Git;

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
                // Add button to view repository in browser.
                var button = new Button(ViewRepoOnBrowser) { text = "View repository" };
                button.AddClasses("link");

#if UNITY_6000_0_OR_NEWER
                var links = _packageDetails
                    .Q<PackageDetailsLinks>()
                    ?.Q(classes: new[] { "left" });
                if (links != null)
                {
                    var separator = new Label("|");
                    separator.AddToClassList("separator");
                    links.Add(separator);
                    links.Add(button);
                }
#elif UNITY_2023_1_OR_NEWER
                var links = _packageDetails
                        .Q<PackageDetailsLinks>()
                        ?.Q(classes: new[] { "left" });
                    @@ -124,58 +74,38 @@ namespace Coffee.UpmGitExtension
                links.Add(separator);
                links.Add(button);
#elif UNITY_2021_2_OR_NEWER
                var links = _packageDetails.Q<PackageDetailsLinks>();
                var left = links.Q("packageDetailHeaderUPMLinks", new[] { "left" }) ??
                           links.Q(classes: new[] { "left" });
                links.Call("AddToLinks", left, button, true);
#else
                _packageDetails.Call("AddToLinks", button);
#endif
                
                _targetVersion = null;
                
#if UNITY_6000_0_OR_NEWER
                var packageVersion = GitPackageDatabase.GetAvailablePackageVersions().FirstOrDefault(v => v.GetPackageInfo()?.packageId == packageInfo?.packageId);
#else
                var packageVersion = GitPackageDatabase.GetAvailablePackageVersions().FirstOrDefault(v => v.packageInfo.packageId == packageInfo.packageId);
#endif
                
                var package = packageVersion != null ? GitPackageDatabase.GetPackage(packageVersion) : GitPackageDatabase.GetPackage(packageInfo.name);
                
#if UNITY_6000_0_OR_NEWER
                _targetVersion = package?.versions?.installed;
#else
                if (packageVersion != null)
                {
                    _targetVersion = package?.versions?.installed?.uniqueId == packageInfo.packageId
                        ? package.versions.recommended
                        : packageVersion;
                }
                else
                {
                    _targetVersion = package?.versions?.installed != null
                        ? package?.versions?.recommended
                        : package?.versions?.primary;
                }
#endif
                EditorApplication.delayCall +=
#if UNITY_6000_0_OR_NEWER
                RefreshVersionItem;
#elif UNITY_2022_2_OR_NEWER
                RefreshVersionItems;
#endif
            }
            
#if UNITY_6000_0_OR_NEWER
            else
            {
                RemoveVisualElement();
            }
#endif
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
        private readonly List<VisualElement> _gitVersionRows = new();

        private static PageManager _pageManager =>
            ScriptableSingleton<ServicesContainer>.instance.Resolve<PageManager>();
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

            if (packageId.Contains("bitbucket.org/"))
            {
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "Bitbucket-Logo.png") as Texture2D;
            }

            if (packageId.Contains("gitlab.com/"))
            {
                return AssetDatabase.LoadMainAssetAtPath(packageDir + "GitLab-Logo.png") as Texture2D;
            }

            if (packageId.Contains("azure.com/"))
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
                Application.OpenURL(_targetVersion.GetPackageInfo().GetRepositoryUrlForBrowser());
            }
        }
       
#if UNITY_6000_0_OR_NEWER
        private void UpdatePackage(string packageId)
        {
            if (_targetVersion?.GetPackageInfo()?.source == PackageSource.Git)
            {
                GitPackageDatabase.Install(packageId);
            }
            else
            {
                _clickableToUpdate?.Call("Invoke", new MouseDownEvent());
            }
        }
#else
        private void UpdatePackage()
        {
            if (_targetVersion?.GetPackageInfo()?.source == PackageSource.Git)
            {
                GitPackageDatabase.Install(_targetVersion.GetPackageInfo().packageId);
            }
            else
            {
                _clickableToUpdate?.Call("Invoke", new MouseDownEvent());
            }
        }
#endif

        private void RemovePackage(string packageId)
        {
            if (_targetVersion?.GetPackageInfo()?.source == PackageSource.Git)
            {
                GitPackageDatabase.Uninstall(packageId);
            }
            else
            {
                _clickableToUpdate?.Call("Invoke", new MouseDownEvent());
            }
        }
        
#if UNITY_6000_0_OR_NEWER
        private void RefreshAllGitPackages()
        {
            var allPackages = GitPackageDatabase.GetUpmPackages();
            var scrollView = _root.Q<PackageListScrollView>();
            if (scrollView == null)
            {
                return;
            }

            foreach (var package in allPackages)
            {
                var installed = package.versions.installed as UpmPackageVersion;
                if (installed == null)
                    continue;

                var availableVersions = GitPackageDatabase.GetPackageVersion(installed.name, installed.uniqueId);
                if (availableVersions == null || availableVersions.Count == 0)
                    continue;

                var latest = availableVersions
                    .OrderByDescending(v => v.version)
                    .FirstOrDefault();

                if (latest != null && latest.version > installed.version)
                {
                    var packageItem = scrollView.GetPackageItem(package.uniqueId);
                    if (packageItem == null)
                        continue;

                    var stateIcon = packageItem.Q<VisualElement>("stateIcon");
                    if (stateIcon == null)
                        continue;

                    stateIcon.ClearClassList();
                    stateIcon.AddToClassList("status");
                    stateIcon.AddToClassList("update-available");

                    stateIcon.style.backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("Update-Available").image;
                    stateIcon.tooltip = $"Update available: {installed.version} → {latest.version}";

                    stateIcon.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void RefreshVersionItem()
        {
            RemoveVisualElement(true);

            var packageName = _targetVersion.name;

            var availableVersions = GitPackageDatabase.GetPackageVersion(packageName, _targetVersion.uniqueId)
                .OrderByDescending(v => v.version)
                .ToList();

            foreach (var version in availableVersions)
            {
                var versionRow = new VisualElement();
                versionRow.AddToClassList("unity-list-view__item");
                versionRow.style.flexDirection = FlexDirection.Row;
                versionRow.style.justifyContent = Justify.SpaceBetween;
                versionRow.style.alignItems = Align.Center;
                versionRow.style.paddingLeft = 5;
                versionRow.style.paddingRight = 5;
                versionRow.style.height = 36;
                versionRow.style.marginBottom = 10;
                versionRow.style.marginLeft = 10;
                versionRow.style.marginRight = 10;
                versionRow.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));

                var expandIcon = new Label("▶");
                expandIcon.style.width = 8;
                expandIcon.style.color = Color.gray;
                expandIcon.style.unityFontStyleAndWeight = FontStyle.Normal;

                var shortVersion = GitPackageDatabase.GetShortVersion(version);
                var versionLabel = new Label(shortVersion);
                versionLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                versionLabel.style.fontSize = 14;

                var gitTag = new Label("Git");
                gitTag.AddToClassList("unity-label");
                gitTag.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));
                gitTag.style.color = Color.white;
                gitTag.style.paddingLeft = 4;
                gitTag.style.paddingRight = 4;
                gitTag.style.marginLeft = 6;
                gitTag.style.marginRight = 6;
                gitTag.style.fontSize = 11;
                gitTag.style.unityFontStyleAndWeight = FontStyle.Normal;

                gitTag.style.borderTopWidth = 1;
                gitTag.style.borderBottomWidth = 1;
                gitTag.style.borderLeftWidth = 1;
                gitTag.style.borderRightWidth = 1;
                gitTag.style.borderTopColor = Color.white;
                gitTag.style.borderBottomColor = Color.white;
                gitTag.style.borderLeftColor = Color.white;
                gitTag.style.borderRightColor = Color.white;
                
                var tagLabel = new Label();
                tagLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                tagLabel.style.color = new StyleColor(Color.white);
                tagLabel.style.marginRight = 6;
                
                var actionButton = new Button();
                if (version.uniqueId == _targetVersion.uniqueId && version.version == _targetVersion.version)
                {
                    tagLabel.text = "Installed";
                    actionButton.text = "Remove";
                    actionButton.clicked += () =>
                    {
                        RemovePackage(version.packageId);
                        actionButton.SetEnabled(false);
                    };
                }
                else
                {
                    actionButton.text = "Update";
                    actionButton.clicked += () =>
                    {
                        UpdatePackage(version.packageId);
                        actionButton.SetEnabled(false);
                    };
                }

                actionButton.style.width = 75;
                actionButton.style.height = 20;
                actionButton.style.marginLeft = 5;

                var leftGroup = new VisualElement();
                leftGroup.style.flexDirection = FlexDirection.Row;
                leftGroup.style.alignItems = Align.Center;
                leftGroup.style.flexGrow = 1;

                leftGroup.Add(expandIcon);
                leftGroup.Add(versionLabel);
                leftGroup.Add(gitTag);
                if (!string.IsNullOrEmpty(tagLabel.text))
                    leftGroup.Add(tagLabel);

                versionRow.Add(leftGroup);
                versionRow.Add(actionButton);

                AddVisualElement(versionRow);
            }
        }
#endif

#if !UNITY_6000_0_OR_NEWER
        private void RefreshVersionItems()
        {
#if UNITY_2022_2_OR_NEWER
            var items = _root.Query<PackageDetailsVersionHistoryItem>().Build()
                .Select(item => new
                {
                    label = item.Q<Toggle>("versionHistoryItemToggle")?.Q<Label>(),
                    version = item.version as UpmPackageVersionEx
                });
#else
            var items = _root.Query<PackageVersionItem>().Build().ToList()
                .Select(item => new { label = item.Q<Label>("versionLabel"), version =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     item.version as UpmPackageVersionEx });
#endif
            foreach (var item in items)
            {
                if (item.label != null && item.version != null)
                {
                    item.label.text = item.version.fullVersionString;
                }
            }
        }
#endif
        
        private void AddVisualElement(VisualElement versionRow)
        {
            var versionTab = _root.Q<PackageDetailsVersionsTab>();

            versionTab.Add(versionRow);
            _gitVersionRows.Add(versionRow);
        }

        private void RemoveVisualElement(bool clear = false)
        {
            var versionTab = _root.Q<PackageDetailsVersionsTab>();

            foreach (var row in _gitVersionRows)
            {
                versionTab.Remove(row);
            }

            _gitVersionRows.Clear();
        }
    }
}