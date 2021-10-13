using System;
using UnityEngine.UIElements;

namespace Coffee.UpmGitExtension
{
    internal static class VisualElementExtension
    {
        public static void OverwriteCallback(this Button button, Action action)
        {
            button.RemoveManipulator(button.clickable);
            button.clickable = new Clickable(action);
            button.AddManipulator(button.clickable);
        }

        public static VisualElement GetRoot(this VisualElement element)
        {
            while (element != null && element.parent != null)
            {
                element = element.parent;
            }

            return element;
        }
    }
}

