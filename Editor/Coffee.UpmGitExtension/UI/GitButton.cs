using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Coffee.UpmGitExtension
{
    internal class GitButton : ToolbarButton
    {
        //################################
        // Constant or static members.
        //################################
        private const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
        private const string StylePath = ResourcesPath + "GitButton.uss";

        public static bool IsResourceReady()
        {
            return EditorGUIUtility.Load(StylePath);
        }

        public GitButton(System.Action action) : base(action)
        {
            styleSheets.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath));

            AddToClassList("git-button");

            var image = new VisualElement();
            image.AddToClassList("git-button-image");

            Add(image);
        }
    }
}
