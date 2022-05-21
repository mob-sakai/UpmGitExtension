using UnityEngine.UIElements;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.PackageManager.UI.Internal;
#else
using UnityEditor.PackageManager.UI;
#endif
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Coffee.UpmGitExtension
{
    internal class SearchResultListView : ListView
    {
        private string _searchText = "";
        private string[] _searchedItems = new string[0];
        private readonly Func<string[]> _searchFunc = null;

#if UNITY_2021_2_OR_NEWER
        private float _itemHeight { get { return fixedItemHeight; } set { fixedItemHeight = value; } }
#else
        private float _itemHeight { get { return itemHeight; } set { itemHeight = (int)value; } }
#endif

        public void UpdateSearchText(string text = "")
        {
            if (_searchText == text) return;
            _searchText = text;
            UpdateSearchedItems();
        }

        public void UpdateSearchedItems()
        {
            _searchedItems = _searchFunc()
                .Where(repo => 0 <= repo.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var count = _searchedItems.Length;
            if (count == 0)
                UIUtils.SetElementDisplay(this, false);
            else
                style.height = Mathf.Min(count, 5) * _itemHeight;

            itemsSource = _searchedItems;

#if UNITY_2021_2_OR_NEWER
            RefreshItems();
#endif
        }

        private void Adjust(VisualElement v)
        {
            var r = v.worldBound;
            style.position = Position.Absolute;
            style.left = r.x;
            style.top = r.y;
            style.width = r.width;
            style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.098f, 0.098f, 0.098f, 0.85f) : new Color(0.541f, 0.541f, 0.541f, 0.85f);
        }

        public SearchResultListView(TextField textField, Func<string[]> searchFunc) : base()
        {
            UIUtils.SetElementDisplay(this, false);
            _searchFunc = searchFunc;

            _itemHeight = 16;
            selectionType = SelectionType.Single;

            makeItem = () => new Label();
            bindItem = (e, i) => (e as Label).text = _searchedItems[i];
#if UNITY_2022_2_OR_NEWER
            selectionChanged += o =>
#else
            onSelectionChange += o =>
#endif
            {
                textField.value = o.FirstOrDefault() as string ?? "";
                UIUtils.SetElementDisplay(this, false);
            };

            textField.RegisterCallback<FocusOutEvent>(_ => UIUtils.SetElementDisplay(this, false));
            textField.RegisterCallback<FocusInEvent>(_ =>
            {
                Adjust(textField);
                UIUtils.SetElementDisplay(this, true);
                UpdateSearchedItems();
            });
            textField.RegisterValueChangedCallback(e =>
            {
                UpdateSearchText(e.newValue);
                UIUtils.SetElementDisplay(this, true);
            });
        }
    }
}
