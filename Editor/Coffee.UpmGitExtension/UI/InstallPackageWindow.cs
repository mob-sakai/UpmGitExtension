#if IGNORE_ACCESS_CHECKS // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.UpmGitExtension
{
    internal class InstallPackageWindow : VisualElement
    {
        //################################
        // Constant or Static Members.
        //################################
        const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
        const string TemplatePath = ResourcesPath + "InstallPackageWindow.uxml";
        const string StylePath = ResourcesPath + "InstallPackageWindow.uss";

        public static bool IsResourceReady()
        {
            return EditorGUIUtility.Load(TemplatePath) && EditorGUIUtility.Load(StylePath);
        }

        //################################
        // Public Members.
        //################################
        public InstallPackageWindow()
        {
            VisualTreeAsset asset = EditorGUIUtility.Load(TemplatePath) as VisualTreeAsset;

#if UNITY_2019_1_OR_NEWER
			root = asset.CloneTree ();
			styleSheets.Add (AssetDatabase.LoadAssetAtPath<StyleSheet> (StylePath));
#else
            root = asset.CloneTree(null);
            AddStyleSheetPath(StylePath);
#endif
            // Add ui elements and class (for miximize).
            AddToClassList("maximized");
            root.AddToClassList("maximized");
            root.AddToClassList("installPackageWindow");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "right");
            Add(root);

            // Find ui elements.
            repoUrlText = root.Q<TextField>("repoUrlText");
            findVersionsButton = root.Q<Button>("findVersionsButton");
            findVersionsError = root.Q("findVersionsError");

            versionContainer = root.Q("versionContainer");
            versionSelectButton = root.Q<Button>("versionSelectButton");

            packageContainer = root.Q("packageContainer");
            packageNameLabel = root.Q<Label>("packageNameLabel");
            installPackageButton = root.Q<Button>("installPackageButton");

            closeButton = root.Q<Button>("closeButton");


            // Url container
#if UNITY_2019_1_OR_NEWER
			repoUrlText.RegisterValueChangedCallback ((evt) => onChange_RepoUrl (evt.newValue));
#else
            repoUrlText.OnValueChanged((evt) => onChange_RepoUrl(evt.newValue));
#endif
            findVersionsButton.clickable.clicked += onClick_FindVersions;

            // Version container
            versionSelectButton.clickable.clicked += onClick_SelectVersions;

            // Package container
            installPackageButton.clickable.clicked += onClick_InstallPackage;

            // Controll container
            closeButton.clickable.clicked += onClick_Close;

            onClick_Close();
        }

        public void Open()
        {
            UIUtils.SetElementDisplay(this, true);
        }

        //################################
        // Private Members.
        //################################
        static readonly Regex regSemVer = new Regex(@"^\d+", RegexOptions.Compiled);
        readonly VisualElement root;
        readonly VisualElement versionContainer;
        readonly VisualElement packageContainer;
        readonly VisualElement findVersionsError;
        readonly Button closeButton;
        readonly Button installPackageButton;
        readonly Button findVersionsButton;
        readonly Button versionSelectButton;
        readonly Label packageNameLabel;
        readonly TextField repoUrlText;
        IEnumerable<AvailableVersion> versions = new AvailableVersion[0];
        AvailableVersion currentVersion = null;

        void EnableVersionContainer(bool flag)
        {
            versionContainer.SetEnabled(flag);
            versionSelectButton.SetEnabled(flag);
            versionSelectButton.text = "-- Select package version --";
        }

        void EnablePackageContainer(bool flag, string name = "")
        {
            packageContainer.SetEnabled(flag);
            installPackageButton.SetEnabled(flag);
            packageNameLabel.text = name;
        }

        void onClick_Close()
        {
            UIUtils.SetElementDisplay(this, false);

            repoUrlText.value = "";
            findVersionsButton.SetEnabled(false);

            EnableVersionContainer(false);
            EnablePackageContainer(false);
        }

        void onChange_RepoUrl(string url)
        {
            var valid = !string.IsNullOrEmpty(url);
            findVersionsButton.SetEnabled(valid);

            EnableVersionContainer(false);
            EnablePackageContainer(false);

            findVersionsError.visible = false;
            currentVersion = null;
        }

        void onClick_FindVersions()
        {
            root.SetEnabled(false);
            EnableVersionContainer(false);

            var repoUrl = GetRepoUrl(repoUrlText.value);
            AvailableVersions.Clear(repoUrl: repoUrl);
            AvailableVersionExtensions.UpdateAvailableVersions(repoUrl: repoUrl, callback: exitCode =>
            {
                bool success = exitCode == 0;
                root.SetEnabled(true);
                EnableVersionContainer(success);
                findVersionsError.visible = !success;
            });
        }

        void onClick_SelectVersions()
        {
            var menu = new GenericMenu();
            GenericMenu.MenuFunction2 callback = (v) =>
            {
                var version = v as AvailableVersion;
                currentVersion = version;
                versionSelectButton.text = version.refNameText;
                EnablePackageContainer(true, version.packageName);
            };

            var repoUrl = GetRepoUrl(repoUrlText.value);
            foreach (var version in AvailableVersions.GetVersions(repoUrl: repoUrl).OrderByDescending(v => v.version))
            {
                var text = version.refNameText;
                menu.AddItem(new GUIContent(text), versionSelectButton.text == text, callback, version);
            }

            menu.DropDown(versionSelectButton.worldBound);
        }

        void onClick_InstallPackage()
        {
            PackageUtils.InstallPackage(currentVersion.packageName, currentVersion.repoUrl, currentVersion.refName);
            onClick_Close();
        }

        public static string GetRepoUrl(string url)
        {
            Match m = Regex.Match(url, "(git@[^:]+):(.*)");
            string ret = m.Success ? string.Format("ssh://{0}/{1}", m.Groups[1].Value, m.Groups[2].Value) : url;
#if UNITY_2019_1_OR_NEWER
            return ret.EndsWith(".git") ? ret : "git+" + ret;
#else
            return ret.EndsWith(".git") ? ret : ret + ".git";
#endif
        }
    }
}
#endif // [ASMDEFEX] DO NOT REMOVE THIS LINE MANUALLY.