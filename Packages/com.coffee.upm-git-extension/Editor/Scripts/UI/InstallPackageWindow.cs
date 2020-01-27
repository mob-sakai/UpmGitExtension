#if OPEN_SESAME // This line is added by Open Sesame Portable. DO NOT remov manually.
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.PackageManager.UI
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
            UIUtils.SetElementDisplay(this, false);
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

            SetPhase(Phase.InputRepoUrl);
        }

        public void Open()
        {
            UIUtils.SetElementDisplay(this, true);
            SetPhase(Phase.InputRepoUrl);
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
        // IEnumerable<string> versions = new string[0];
        IEnumerable<AvailableVersion> versions = new AvailableVersion[0];
        string refName = "";

        enum Phase
        {
            InputRepoUrl,
            FindVersions,
            SelectVersion,
            FindPackage,
            InstallPackage,
        }

        void SetPhase(Phase phase)
        {
            bool canFindVersions = Phase.FindVersions <= phase;
            repoUrlText.value = canFindVersions ? repoUrlText.value : "";
            findVersionsButton.SetEnabled(canFindVersions);
            if (phase == Phase.FindVersions)
                repoUrlText.Focus();

            bool canSelectVersion = Phase.SelectVersion <= phase;
            versionContainer.SetEnabled(canSelectVersion);
            versionSelectButton.SetEnabled(canSelectVersion);
            versionSelectButton.text = canSelectVersion ? versionSelectButton.text : "-- Select package version --";
            if (canSelectVersion)
            {
                findVersionsError.visible = false;
            }

            bool canInstallPackage = Phase.InstallPackage <= phase;
            packageContainer.SetEnabled(canInstallPackage);
            packageNameLabel.text = canInstallPackage ? packageNameLabel.text : "";
            if (canInstallPackage || phase == Phase.InputRepoUrl)
            {
                findVersionsError.visible = false;
            }
        }

        void onClick_Close()
        {
            UIUtils.SetElementDisplay(this, false);
        }

        void onChange_RepoUrl(string url)
        {
            SetPhase(string.IsNullOrEmpty(url) ? Phase.InputRepoUrl : Phase.FindVersions);
        }

        void onClick_FindVersions()
        {
            SetPhase(Phase.FindVersions);
            root.SetEnabled(false);
            AvailableVersions.Clear(repoUrl: repoUrlText.value);
            AvailableVersions.UpdateAvailableVersions(repoUrl: repoUrlText.value, callback: (s, e)=>{
                root.SetEnabled(true);
            });
            SetPhase(Phase.SelectVersion);
        }

        void onClick_SelectVersions()
        {
            var menu = new GenericMenu();
            var currentRefName = versionSelectButton.text;

            GenericMenu.MenuFunction2 callback = (v) =>
            {
                var version = v as AvailableVersion;
                versionSelectButton.text = version.refNameText;
                packageNameLabel.text = version.packageName;
                refName = version.refName;
                SetPhase(Phase.InstallPackage);
            };

            foreach (var version in AvailableVersions.GetVersions(repoUrl: repoUrlText.value).OrderByDescending(v => v.version))
            {
                var text = version.refNameText;
                menu.AddItem(new GUIContent(text), versionSelectButton.text == text, callback, version);
            }

            menu.DropDown(versionSelectButton.worldBound);
        }

        void onClick_InstallPackage()
        {
            PackageUtils.InstallPackage(packageNameLabel.text, GetRepoUrl(repoUrlText.value), refName);
            onClick_Close();
        }

        public static string GetRepoUrl(string url)
        {
            Match m = Regex.Match(url, "(git@[^:]+):(.*)");
            string ret = m.Success ? string.Format("ssh://{0}/{1}", m.Groups[1].Value, m.Groups[2].Value) : url;
            return ret.EndsWith(".git", System.StringComparison.Ordinal) ? ret : ret + ".git";
        }
    }
}
#endif // This line is added by Open Sesame Portable. DO NOT remov manually.