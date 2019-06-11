using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Utils = Coffee.PackageManager.UpmGitExtensionUtils;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Coffee.PackageManager
{
	[InitializeOnLoad]
	internal class UpmGitExtensionUI : VisualElement, IPackageManagerExtension
	{
		//################################
		// Constant or Static Members.
		//################################
#if UPM_GIT_EXT_PROJECT
		const string ResourcesPath = "Assets/UpmGitExtension/Editor/Resources/";
#else
		const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
#endif
		const string TemplatePath = ResourcesPath + "UpmGitExtension.uxml";
		const string StylePath = ResourcesPath + "UpmGitExtension.uss";

		static UpmGitExtensionUI ()
		{
			PackageManagerExtensions.RegisterExtension (new UpmGitExtensionUI ());
		}

		//################################
		// Public Members.
		//################################
		/// <summary>
		/// Creates the extension UI visual element.
		/// </summary>
		/// <returns>A visual element that represents the UI or null if none</returns>
		public VisualElement CreateExtensionUI ()
		{
			_initialized = false;
			return this;
		}

		/// <summary>
		/// Called by the Package Manager UI when a package is added or updated.
		/// </summary>
		/// <param name="packageInfo">The package information</param>
		public void OnPackageAddedOrUpdated (PackageInfo packageInfo)
		{
		}

		/// <summary>
		/// Called by the Package Manager UI when a package is removed.
		/// </summary>
		/// <param name="packageInfo">The package information</param>
		public void OnPackageRemoved (PackageInfo packageInfo)
		{
		}

		/// <summary>
		/// Called by the Package Manager UI when the package selection changed.
		/// </summary>
		/// <param name="packageInfo">The newly selected package information (can be null)</param>
		public void OnPackageSelectionChange (PackageInfo packageInfo)
		{
			InitializeUI ();
			if (!_initialized || packageInfo == null || _packageInfo == packageInfo)
				return;

			_packageInfo = packageInfo;

			var isGit = packageInfo.source == PackageSource.Git;

			Utils.SetElementDisplay (_gitDetailActoins, isGit);
			Utils.SetElementDisplay (_originalDetailActions, !isGit);

			Utils.SetElementClass (_hostingIcon, "github", true);
			Utils.SetElementClass (_hostingIcon, "dark", EditorGUIUtility.isProSkin);
		}


		//################################
		// Private Members.
		//################################
		bool _initialized = false;
		PackageInfo _packageInfo;
		Button _hostingIcon { get { return _gitDetailActoins.Q<Button> ("hostingIcon"); } }
		Button _viewDocumentation { get { return _gitDetailActoins.Q<Button> ("viewDocumentation"); } }
		Button _viewChangelog { get { return _gitDetailActoins.Q<Button> ("viewChangelog"); } }
		Button _viewLicense { get { return _gitDetailActoins.Q<Button> ("viewLicense"); } }
		VisualElement _documentationContainer;
		VisualElement _originalDetailActions;
		VisualElement _gitDetailActoins;

		/// <summary>
		/// Initializes UI.
		/// </summary>
		void InitializeUI ()
		{
			if (_initialized)
				return;

			var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset> (TemplatePath);
			if (!asset)
				return;

#if UNITY_2019_1_OR_NEWER
			gitDetailActoins = asset.CloneTree().Q("detailActions");
            gitDetailActoins.styleSheets.Add(EditorGUIUtility.Load(StylePath) as StyleSheet);
#else
			_gitDetailActoins = asset.CloneTree (null).Q ("detailActions");
			_gitDetailActoins.AddStyleSheetPath (StylePath);
#endif

			// Add callbacks
			_hostingIcon.clickable.clicked += () => Application.OpenURL (Utils.GetRepoURL (_packageInfo));
			_viewDocumentation.clickable.clicked += () => Application.OpenURL (Utils.GetFileURL (_packageInfo, "README.md"));
			_viewChangelog.clickable.clicked += () => Application.OpenURL (Utils.GetFileURL (_packageInfo, "CHANGELOG.md"));
			_viewLicense.clickable.clicked += () => Application.OpenURL (Utils.GetFileURL (_packageInfo, "LICENSE.md"));

			// Move element to documentationContainer
			_documentationContainer = parent.parent.Q ("documentationContainer");
			_originalDetailActions = _documentationContainer.Q ("detailActions");
			_documentationContainer.Add (_gitDetailActoins);
			_initialized = true;
		}
	}
}
