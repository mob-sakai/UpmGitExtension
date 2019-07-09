using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.PackageManager
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
		public InstallPackageWindow ()
		{
			UIUtils.SetElementDisplay (this, false);
			VisualTreeAsset asset = EditorGUIUtility.Load (TemplatePath) as VisualTreeAsset;

#if UNITY_2019_1_OR_NEWER
			root = asset.CloneTree ();
			styleSheets.Add (AssetDatabase.LoadAssetAtPath<StyleSheet> (StylePath));
#else
			root = asset.CloneTree(null);
			AddStyleSheetPath (StylePath);
#endif
			// Add ui elements and class (for miximize).
			AddToClassList ("maximized");
			root.AddToClassList ("maximized");
			root.AddToClassList ("installPackageWindow");
			root.AddToClassList (EditorGUIUtility.isProSkin ? "dark" : "right");
			Add (root);

			// Find ui elements.
			repoUrlText = root.Q<TextField> ("repoUrlText");
			findVersionsButton = root.Q<Button> ("findVersionsButton");
			findVersionsError = root.Q ("findVersionsError");

			versionContainer = root.Q ("versionContainer");
			versionSelectButton = root.Q<Button> ("versionSelectButton");
			findPackageButton = root.Q<Button> ("findPackageButton");
			findPackageError = root.Q ("findPackageError");

			packageContainer = root.Q ("packageContainer");
			packageNameLabel = root.Q<Label> ("packageNameLabel");
			installPackageButton = root.Q<Button> ("installPackageButton");
			installPackageError = root.Q ("installPackageError");

			closeButton = root.Q<Button> ("closeButton");

			// Url container
#if UNITY_2019_1_OR_NEWER
			repoUrlText.RegisterValueChangedCallback ((evt) => onChange_RepoUrl (evt.newValue));
#else
			repoUrlText.OnValueChanged((evt) => onChange_RepoUrl (evt.newValue));
#endif
			findVersionsButton.clickable.clicked += onClick_FindVersions;

			// Version container
			versionSelectButton.clickable.clicked += onClick_SelectVersions;
			findPackageButton.clickable.clicked += onClick_FindPackage;

			// Package container
			installPackageButton.clickable.clicked += onClick_InstallPackage;
			
			// Controll container
			closeButton.clickable.clicked += onClick_Close;

			SetPhase (Phase.InputRepoUrl);
		}

		public void Open ()
		{
			UIUtils.SetElementDisplay (this, true);
			SetPhase (Phase.InputRepoUrl);
		}

		//################################
		// Private Members.
		//################################
		static readonly Regex regSemVer = new Regex(@"^\d+", RegexOptions.Compiled);
		readonly VisualElement root;
		readonly VisualElement versionContainer;
		readonly VisualElement packageContainer;
		readonly VisualElement findVersionsError;
		readonly VisualElement findPackageError;
		readonly VisualElement installPackageError;
		readonly Button closeButton;
		readonly Button installPackageButton;
		readonly Button findVersionsButton;
		readonly Button versionSelectButton;
		readonly Button findPackageButton;
		readonly Label packageNameLabel;
		readonly TextField repoUrlText;
		IEnumerable<string> versions = new string [0];

		enum Phase
		{
			InputRepoUrl,
			FindVersions,
			SelectVersion,
			FindPackage,
			InstallPackage,
		}

		void SetPhase (Phase phase)
		{
			bool canFindVersions = Phase.FindVersions <= phase;
			repoUrlText.value = canFindVersions ? repoUrlText.value : "";
			findVersionsButton.SetEnabled (canFindVersions);
			if (phase == Phase.FindVersions)
				repoUrlText.Focus ();

			bool canSelectVersion = Phase.SelectVersion <= phase;
			versionContainer.SetEnabled (canSelectVersion);
			versionSelectButton.SetEnabled (canSelectVersion);
			versionSelectButton.text = canSelectVersion ? versionSelectButton.text : "-- Select package version --";
			if (canSelectVersion)
			{
				findVersionsError.visible = false;
				findPackageError.visible = false;
				installPackageError.visible = false;
			}

			bool canFindPackage = Phase.FindPackage <= phase;
			findPackageButton.SetEnabled (canFindPackage);

			bool canInstallPackage = Phase.InstallPackage <= phase;
			packageContainer.SetEnabled (canInstallPackage);
			packageNameLabel.text = canInstallPackage ? packageNameLabel.text : "";
			if (canInstallPackage || phase == Phase.InputRepoUrl)
			{
				findVersionsError.visible = false;
				findPackageError.visible = false;
				installPackageError.visible = false;
			}
		}

		void onClick_Close ()
		{
			UIUtils.SetElementDisplay (this, false);
		}

		void onChange_RepoUrl (string url)
		{
			SetPhase (string.IsNullOrEmpty (url) ? Phase.InputRepoUrl : Phase.FindVersions);
		}

		void onClick_FindVersions ()
		{
			SetPhase (Phase.FindVersions);
			root.SetEnabled (false);
			GitUtils.GetRefs (repoUrlText.value, refs =>
			{
				root.SetEnabled (true);
				bool hasError = !refs.Any ();
				findVersionsError.visible = hasError;
				if (!hasError)
				{
					versions = refs;
					SetPhase (Phase.SelectVersion);
				}
			});
		}

		void onClick_SelectVersions ()
		{
			SetPhase (Phase.SelectVersion);

			var menu = new GenericMenu ();
			var currentRefName = versionSelectButton.text;

			GenericMenu.MenuFunction2 callback = (x) =>
			{
				versionSelectButton.text = x as string;
				SetPhase (Phase.FindPackage);
			};

			var orderdVers = versions.OrderByDescending(x => x).ToArray();

			foreach (var t in orderdVers.Where(x => regSemVer.IsMatch(x)))
				menu.AddItem(new GUIContent(t), currentRefName == t, callback, t);

			menu.AddDisabledItem(GUIContent.none);
			
			foreach (var t in orderdVers.Where(x => !regSemVer.IsMatch(x)))
				menu.AddItem(new GUIContent(t), currentRefName == t, callback, t);

			menu.DropDown(versionSelectButton.worldBound);
		}

		void onClick_FindPackage ()
		{
			root.SetEnabled (false);
			SetPhase (Phase.FindPackage);
			GitUtils.GetPackageJson (repoUrlText.value, versionSelectButton.text, packageName =>
			{
				root.SetEnabled (true);
				bool hasError = string.IsNullOrEmpty (packageName);
				findPackageError.visible = hasError;
				if (!hasError)
				{
					EditorApplication.delayCall += () => packageNameLabel.text = packageName;
					SetPhase (Phase.InstallPackage);
				}
			});
		}

		void onClick_InstallPackage ()
		{
			root.SetEnabled (false);
			var url = PackageUtils.GetRepoUrl (repoUrlText.value);
			var _packageId = string.Format ("{0}@{1}#{2}", packageNameLabel.text, url, versionSelectButton.text);
			PackageUtils.AddPackage (_packageId, req =>
			{
				root.SetEnabled (true);
				if (req.Status == StatusCode.Success)
				{
					onClick_Close ();
				}
				else if (req.Status == StatusCode.Failure)
				{
					installPackageError.tooltip = req.Error.message;
					installPackageError.visible = true;
				}
			});
		}
	}
}