using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace Coffee.PackageManager
{
	internal class GitButton : ToolbarButton
	{
		//################################
		// Constant or Static Members.
		//################################
		const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
		const string StylePath = ResourcesPath + "GitButton.uss";

		public static bool IsResourceReady()
		{
			return EditorGUIUtility.Load(StylePath);
		}

		public GitButton (System.Action action) : base (action)
		{
#if UNITY_2019_1_OR_NEWER
			styleSheets.Add (UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath));
#else
			AddStyleSheetPath (StylePath);
#endif

			AddToClassList ("git-button");

			var image = new VisualElement ();
			image.AddToClassList ("git-button-image");

			Add (image);
		}
	}
}