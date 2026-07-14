#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace TinyDataTable.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(IIdentifier), true)]
    internal class IDPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumType = fieldInfo.FieldType
                .GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)
                .FieldType;

            var prop = property.FindPropertyRelative("_value");
            //AdvancedDropdownがエラーを吐くのでIMGUIを使う
            var container = new IMGUIContainer();
            container.onGUIHandler = () =>
            {
                bool isObsolete = false;
                bool isRetired = false;
                string enumName = string.Empty;
                try
                {
                    enumName = GetEnumName(prop);
                }
                catch (Exception )
                {
                    enumName = "Retired";
                    isRetired = true;
                }
                System.Reflection.FieldInfo field = enumType.GetField(enumName);
                if (field != null)
                {
                    isObsolete = field.GetCustomAttribute<ObsoleteAttribute>() is not null;
                }
                Color originalColor = GUI.backgroundColor;
                var color = (isObsolete|isRetired)?Color.red:originalColor;
                var text = (isObsolete ) ? $"{enumName}(Obsolete)" :enumName;

                GUI.backgroundColor = color;
                GUIContent guiContext = new GUIContent(text);

                Rect rect = EditorGUILayout.GetControlRect();
                if (EditorGUI.DropdownButton(rect,guiContext, FocusType.Keyboard))
                {
                    PopupEnumList(enumType,rect, prop, property.displayName);
                }

                GUI.backgroundColor = originalColor;
            };
            
                
            return UIToolkitEditorUtility.CreateLabeledVisualElement(preferredLabel, container).container;
        }

        private void PopupEnumList( Type enumType , Rect rect,SerializedProperty propValue , string displayName)
        {
            var dropdown = new EnumAdvancedDropdown(
                new AdvancedDropdownState(),
                enumType,
                displayName,
                propValue.enumValueIndex,
                (index, name) =>
                {
                    // 値を更新
                    propValue.enumValueIndex = index;
                    propValue.serializedObject.ApplyModifiedProperties();
                }
            );
            dropdown.Show(rect);
        }
        
        public static string GetEnumName( SerializedProperty property) => property.enumNames[property.enumValueIndex];
     
        /// <summary>
        /// Enumを表示するためのカスタムドロップダウンメニュー
        /// </summary>
        private class EnumAdvancedDropdown : AdvancedDropdown
        {
            private Action<int,string> _onSelected;
            private string _title;
            private int _index;
            private bool[] _obsoletes;
            private Type _enumType;

            private class EnumAdvancedDropdownItem : AdvancedDropdownItem
            {
                public int index;
                
                public EnumAdvancedDropdownItem(string name) : base( name )
                {
                }
            }

            public EnumAdvancedDropdown(
                AdvancedDropdownState state,
                Type enumType,
                string title,
                int index,
                Action<int, string> onSelected) : base(state)
            {
                _title = title;
                _index = index;
                _enumType = enumType;
                _onSelected = onSelected;
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem(_title);

                var values = Enum.GetValues(_enumType)
                    .Cast<object>();
                
                var attrs = values
                    .Select(t => (t.GetType(), t.ToString()))
                    .Select(t => t.Item1.GetField(t.Item2))
                    .Select(t => (
                        obsoletes:t.GetCustomAttributes<ObsoleteAttribute>(),
                        order:t.GetCustomAttributes<EnumIndexAttribute>()));

                var _items = values
                    .Select( (t,i) => (index:i,name:UnityEditor.ObjectNames.NicifyVariableName(Enum.GetName(_enumType, t))))
                    .Zip(attrs, (info, attr) => (info, attr))
                    .OrderBy(t => t.attr.order.Any() ? t.attr.order.First().Order : 0);
                
                foreach (var (item,index) in _items.Select((t,i)=>(t,i)))
                {
                    bool isObsolete = item.attr.obsoletes.Any();
                    if (isObsolete && (_index != item.info.index))
                    {
                        continue;
                    }

                    var name = isObsolete ? $"{item.info.name}(Obsolete)" : item.info.name;
                    var dropdownItem = new EnumAdvancedDropdownItem(name)
                    {
                        index = item.info.index,
                        name = name,
                        icon = (_index != item.info.index) ? null : EditorGUIUtility.IconContent("d_FilterSelectedOnly").image as Texture2D,
                        enabled = isObsolete is false,
                    };

                    root.AddChild(dropdownItem);
                }
                return root;
            }
            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is EnumAdvancedDropdownItem myItem)
                {
                    _onSelected?.Invoke(myItem.index, myItem.name);
                }
            }
        }    
    }    
}
#endif

