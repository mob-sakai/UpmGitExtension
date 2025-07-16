using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager.UI.Internal;

namespace Coffee.UpmGitExtension
{
    internal class PackageDetailsExtension
    {
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

            EditorApplication.delayCall += () =>
            {
                _pageManager.onVisualStateChange += _ => EditorApplication.delayCall += RefreshAllGitPackages;
                _pageManager.onListUpdate += _ => EditorApplication.delayCall += RefreshAllGitPackages;
            };

            _root.Q<PackageDetailsVersionsTab>().RegisterCallback<GeometryChangedEvent>(ev =>
            {
                if ((ev.target as VisualElement).style.visibility.value == Visibility.Visible)
                {
                    RefreshAllGitPackages();
                }
            });
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo == null) return;

            var isGit = packageInfo.source == PackageSource.Git;

            var hostButton = _packageDetails.Q<Button>("hostButton");
            if (hostButton != null)
            {
                hostButton.style.backgroundImage = GetHostLogo(packageInfo.packageId);
                UIUtils.SetElementDisplay(hostButton, isGit);
            }

            if (isGit)
            {
                var button = new Button(ViewRepoOnBrowser) { text = "View repository" };
                button.AddClasses("link");

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

                _targetVersion = null;
                var packageVersion = GitPackageDatabase.GetAvailablePackageVersions().FirstOrDefault(v => v.GetPackageInfo()?.packageId == packageInfo?.packageId);
                if (packageVersion != null)
                {
                    var package = GitPackageDatabase.GetPackage(packageVersion);
                    _targetVersion = package?.versions?.installed;
                }
                else
                {
                    var package = GitPackageDatabase.GetPackage(packageInfo.name);
                    _targetVersion = package?.versions?.installed;
                }

                EditorApplication.delayCall += RefreshVersionItem;
            }
            else
            {
                RemoveVisualElement();
            }
        }

        private PackageDetails _packageDetails;
        private IPackageVersion _targetVersion;
        private Clickable _clickableToUpdate;
        private Button _updateButton;
        private VisualElement _root;
        private readonly List<VisualElement> _gitVersionRows = new();

        private static PageManager _pageManager =>
            ScriptableSingleton<ServicesContainer>.instance.Resolve<PageManager>();

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

        private void ViewRepoOnBrowser()
        {
            if (_targetVersion != null)
            {
                Application.OpenURL(_targetVersion.GetPackageInfo().GetRepositoryUrlForBrowser());
            }
        }

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