using UnityEditor;
using UnityEngine.UIElements;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#else
using UnityEditor.PackageManager.UI;
#endif
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Coffee.UpmGitExtension
{
    internal class GitPackageInstallationWindow : VisualElement
    {
        //################################
        // Constant or Static Members.
        //################################
        const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
        const string TemplatePath = ResourcesPath + "GitPackageInstallationWindow.uxml";
        const string StylePath = ResourcesPath + "GitPackageInstallationWindow.uss";

        public static bool IsResourceReady()
        {
            return EditorGUIUtility.Load(TemplatePath) && EditorGUIUtility.Load(StylePath);
        }

        //################################
        // Public Members.
        //################################
        public GitPackageInstallationWindow()
        {
            VisualTreeAsset asset = EditorGUIUtility.Load(TemplatePath) as VisualTreeAsset;

            var root = asset.CloneTree();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath));

            // Add ui elements and class (for miximize).
            AddToClassList("maximized");
            root.AddToClassList("maximized");
            root.AddToClassList("installationWindow");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "right");
            Add(root);

            _rootContainer = root.Q("rootContainer");

            // Find ui elements.
            _loadingSpinner = new LoadingSpinner();
            root.Q("titleContainer").Add(_loadingSpinner);

            _urlContainer = root.Q("urlContainer");
            _repoUrlText = root.Q<TextField>("repoUrlText");
            _findVersionsButton = root.Q<Button>("findVersionsButton");
            _findVersionsError = root.Q("findVersionsError");

            _subDirContainer = root.Q("subDirContainer");
            _pathText = root.Q<TextField>("subDirText");

            _versionContainer = root.Q("versionContainer");
            _versionSelectButton = root.Q<Button>("versionSelectButton");

            _packageNameLabel = root.Q<Label>("packageNameLabel");
            _installPackageButton = root.Q<Button>("installPackageButton");

            _closeButton = root.Q<Button>("closeButton");

            // Url container
            _repoUrlText.RegisterValueChangedCallback((evt) => OnChange_RepoUrl(evt.newValue));
            _pathText.RegisterValueChangedCallback((evt) => OnChange_RepoUrl(_repoUrlText.value));

            _findVersionsButton.clickable.clicked += OnClick_FindVersions;
            _versionSelectButton.clickable.clicked += OnClick_SelectVersions;
            _installPackageButton.clickable.clicked += OnClick_InstallPackage;
            _closeButton.clickable.clicked += OnClick_Close;

            // Search view.
            root.Add(new SearchResultListView(_repoUrlText, () => GitPackageDatabase.GetCachedRepositoryUrls()));

            OnClick_Close();

            // 
            GitPackageDatabase._upmClient.onAddOperation += op => {
                _loadingSpinner.Start();
                _rootContainer.SetEnabled(false);

                op.onOperationFinalized += _ =>
                {
                    _loadingSpinner.Stop();
                    _rootContainer.SetEnabled(true);
                    OnClick_Close();
                };
            };
        }

        public void Open()
        {
            _loadingSpinner.Stop();
            UIUtils.SetElementDisplay(this, true);
            SetState(State.None);

            EditorApplication.delayCall += _repoUrlText.Focus;
        }

        //################################
        // Private Members.
        //################################
        private enum State
        {
            None,
            UrlEntered,
            VersionFound,
            VersionSelected,
            Error,
            Busy,
            NonBusy,
        }

        private readonly VisualElement _rootContainer;
        private readonly VisualElement _urlContainer;
        private readonly VisualElement _subDirContainer;
        private readonly VisualElement _versionContainer;
        private readonly VisualElement _findVersionsError;
        private readonly LoadingSpinner _loadingSpinner;
        private readonly Button _closeButton;
        private readonly Button _installPackageButton;
        private readonly Button _findVersionsButton;
        private readonly Button _versionSelectButton;
        private readonly Label _packageNameLabel;
        private readonly TextField _repoUrlText;
        private readonly TextField _pathText;
        private UpmPackageVersion _currentVersion = null;

        private void SetState(State state)
        {
            switch (state)
            {
                case State.None:
                    _findVersionsError.visible = false;
                    _repoUrlText.value = "";
                    _pathText.value = "";
                    _urlContainer.SetEnabled(true);
                    _subDirContainer.SetEnabled(true);
                    _findVersionsButton.SetEnabled(false);
                    _versionContainer.SetEnabled(false);
                    _versionSelectButton.text = "-- Select package to install --";
                    _packageNameLabel.text = "";
                    break;
                case State.UrlEntered:
                    _findVersionsError.visible = false;
                    _findVersionsButton.SetEnabled(!string.IsNullOrEmpty(_repoUrlText.value));
                    _versionContainer.SetEnabled(false);
                    _versionSelectButton.text = "-- Select package to install --";
                    _packageNameLabel.text = "";
                    break;
                case State.VersionFound:
                    _findVersionsError.visible = false;
                    _versionContainer.SetEnabled(true);
                    _versionSelectButton.SetEnabled(true);
                    _versionSelectButton.text = "-- Select package to install --";
                    _installPackageButton.SetEnabled(false);
                    break;
                case State.VersionSelected:
                    _installPackageButton.SetEnabled(true);
                    _packageNameLabel.text = _currentVersion.uniqueId;
                    break;
                case State.Error:
                    _findVersionsError.visible = true;
                    break;
                case State.Busy:
                    _rootContainer.SetEnabled(false);
                    _loadingSpinner.Start();
                    break;
                case State.NonBusy:
                    _rootContainer.SetEnabled(true);
                    _loadingSpinner.Stop();
                    UIUtils.SetElementDisplay(_loadingSpinner, false);
                    break;
            }
        }

        private void OnClick_Close()
        {
            UIUtils.SetElementDisplay(this, false);
        }

        private void OnChange_RepoUrl(string url)
        {
            SetState(string.IsNullOrEmpty(url) ? State.None : State.UrlEntered);
        }

        private void OnClick_FindVersions()
        {
            SetState(State.Busy);

            var repoUrl = GetRepoUrl(_repoUrlText.value, _pathText.value);
            GitPackageDatabase.Fetch(repoUrl, exitCode =>
            {
                EditorApplication.delayCall += () =>
                {
                    SetState(State.NonBusy);
                    SetState(exitCode == 0 ? State.VersionFound : State.Error);
                };
            });
        }

        private void OnClick_SelectVersions()
        {
            var menu = new GenericMenu();
            GenericMenu.MenuFunction2 callback = (v) =>
            {
                var version = v as UpmPackageVersionEx;
                _currentVersion = version;
                _versionSelectButton.text = GetShortPackageId(version);
                SetState(State.VersionSelected);
            };

            var repoUrl = GetRepoUrl(_repoUrlText.value, _pathText.value);
            foreach (var version in GitPackageDatabase.GetAvailablePackageVersions(repoUrl: repoUrl, preRelease: true).OrderByDescending(v => v.semVersion))
            {
                var text = GetShortPackageId(version);
                menu.AddItem(new GUIContent(text), _versionSelectButton.text == text, callback, version);
            }

            menu.ShowAsContext();
        }

        private void OnClick_InstallPackage()
        {
            GitPackageDatabase.Install(_currentVersion.uniqueId);
        }

        private static string GetRepoUrl(string url, string path)
        {
            // Trim revision from url.
            var sharp = url.IndexOf('#');
            if (0 <= sharp)
                url = url.Substring(0, sharp);

            // scp to ssh
            url = PackageExtensions.GetSourceUrl(url);

            path = path.Trim('/');
            return 0 < path.Length ? url + "?path=" + path : url;
        }

        private static string GetShortPackageId(UpmPackageVersionEx self)
        {
            var semver = self.semVersion.ToString();
            var revision = self.packageInfo.git.revision;
            return revision.Contains(semver)
                ? $"{self.packageUniqueId}/{semver}"
                : $"{self.packageUniqueId}/{semver} ({revision})";
        }
    }
}
