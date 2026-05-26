using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.IMGUI.Controls;

namespace TinyDataTable.Editor
{
    public class DataSheetFieldOrderPopup : PopupWindowContent
    {
        private static readonly Vector2 _windowSize = new Vector2(200, 400);

        private List<string> fieldList;
        
        private Action<List<string>> _onOrderChanged;
        
        public static void Show( List<string> fieldList,Action<List<string>> onOrderChanged, Rect rect)
        {
            var popup = new DataSheetFieldOrderPopup()
            {
                _onOrderChanged = onOrderChanged,
                fieldList = fieldList
            };
            UnityEditor.PopupWindow.Show(rect, popup);            
        }

        public override Vector2 GetWindowSize()
        {
            return _windowSize;
        }   

        // ウィンドウが開いたときの初期化        
        public override void OnOpen()
        {
            var container = editorWindow.rootVisualElement;

            var listView = new ListView();
            
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.reorderable = true;
            listView.makeItem = () => new Label();
            listView.bindItem = (element, i) => (element as Label).text = fieldList[i];
            
            listView.itemsSource = fieldList;
            
            container.Add(listView);

            var button = new Button()
            {
                text = "Decide",
            };
            button.clicked += () =>
            {
                _onOrderChanged?.Invoke(listView.itemsSource.OfType<string>().ToList());
                editorWindow.Close();
            };
            container.Add(button);
        }
        
        // ウィンドウが閉じたときの後処理
        public override void OnClose()
        {
        }        
    }
}