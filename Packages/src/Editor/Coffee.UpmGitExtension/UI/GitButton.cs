using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Coffee.UpmGitExtension
{
    internal class GitButton : ToolbarButton
    {
        //################################
        // Constant or static members.
        //################################
        private const string ResourcesPath = "Packages/com.coffee.upm-git-extension/Editor/Resources/";
        private const string StylePath = ResourcesPath + "GitButton.uss";

        public GitButton(Action action) : base(action)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath));

            AddToClassList("git-button");

            var image = new VisualElement();
            image.AddToClassList("git-button-image");

            Add(image);

#if UNITY_2023_2_OR_NEWER
            image.style.width = new StyleLength(18);
            image.style.height = new StyleLength(18);
#endif
        }

        public static bool IsResourceReady()
        {
            return EditorGUIUtility.Load(StylePath);
        }
    }
}
