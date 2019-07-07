using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using UnityEditor.PackageManager;

namespace Coffee.PackageManager
{
	/// <summary>
	/// Document actions ui.
	/// Display links to repositories and documents.
	/// </summary>
	internal class DocumentActions : VisualElement
	{
		//################################
		// Constant or Static Members.
		//################################
		const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
		const string TemplatePath = ResourcesPath + "DocumentActions.uxml";
		const string StylePath = ResourcesPath + "DocumentActions.uss";

		public static bool IsResourceReady()
		{
			return EditorGUIUtility.Load(TemplatePath) && EditorGUIUtility.Load(StylePath);
		}

		public DocumentActions (VisualElement detailActions)
		{
			originDetailActions = detailActions;
			originDetailActions.parent.Add (this);

			VisualTreeAsset asset = EditorGUIUtility.Load (TemplatePath) as VisualTreeAsset;

#if UNITY_2019_1_OR_NEWER
			root = asset.CloneTree ();
			styleSheets.Add (AssetDatabase.LoadAssetAtPath<StyleSheet> (StylePath));
#else
			root = asset.CloneTree(null);
			AddStyleSheetPath (StylePath);
#endif

			Add (root);

			// Find ui elements.
			hostButton = root.Q<Button> ("hostButton");
			viewDocumentationButton = root.Q<Button> ("viewDocumentationButton");
			viewChangelogButton = root.Q<Button> ("viewChangelogButton");
			viewLicenseButton = root.Q<Button> ("viewLicenseButton");

			// Adjust host icon.
			hostButton.RemoveFromClassList ("unity-button");
			hostButton.RemoveFromClassList ("button");

			// Add callbacks
			hostButton.clickable.clicked += () => Application.OpenURL (PackageUtils.GetRepoHttpUrl (packageInfo));
			viewDocumentationButton.clickable.clicked += () => MarkdownUtils.OpenInBrowser (PackageUtils.GetFilePath (packageInfo, "README.*"));
			viewChangelogButton.clickable.clicked += () => MarkdownUtils.OpenInBrowser (PackageUtils.GetFilePath (packageInfo, "CHANGELOG.*"));
			viewLicenseButton.clickable.clicked += () => MarkdownUtils.OpenInBrowser (PackageUtils.GetFilePath (packageInfo, "LICENSE.*"));
		}

		public void SetPackageInfo (UnityEditor.PackageManager.PackageInfo info)
		{
			packageInfo = info;

			if (packageInfo == null)
				return;

			var isGit = packageInfo.source == PackageSource.Git;
			UIUtils.SetElementDisplay (this, isGit);
			UIUtils.SetElementDisplay (originDetailActions, !isGit);

			var host = Settings.GetHostData (packageInfo.packageId);
			hostButton.tooltip = "View on " + host.Name;
			hostButton.Q ("logo").style.backgroundImage = host.Logo;
		}


		//################################
		// Private Members.
		//################################
		readonly VisualElement root;
		readonly VisualElement originDetailActions;
		readonly Button hostButton;
		readonly Button viewDocumentationButton;
		readonly Button viewChangelogButton;
		readonly Button viewLicenseButton;
		UnityEditor.PackageManager.PackageInfo packageInfo;
	}
}