#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Rendering;

namespace TinyDataTable.Editor
{
    public class IDPropertyDrawerBase<T> : PropertyDrawer where T : Enum 
    {
        private static Texture2D dropdownTexture2D = EditorGUIUtility.IconContent("d_dropdown@2x").image as Texture2D;
        GUIContent guiContext = new GUIContent();
      
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var prop = property.FindPropertyRelative("_value");
            //AdvancedDropdownがエラーを吐くのでIMGUIを使う
            var container = new IMGUIContainer();
            container.onGUIHandler = () =>
            {
                if (string.IsNullOrEmpty(preferredLabel))
                {
                    Rect rect = EditorGUILayout.GetControlRect();
                    bool isObsolete = false;
                    bool isRetired = false;
                    string enumName = string.Empty;
                    try
                    {
                        enumName = prop.GetEnumName<T>();
                    }
                    catch (Exception )
                    {
                        enumName = "Retired";
                        isRetired = true;
                    }
                    FieldInfo field = typeof(T).GetField(enumName);

                    if (field != null)
                    {
                        isObsolete = field.GetCustomAttribute<ObsoleteAttribute>() is not null;
                    }
                    guiContext.text = (isObsolete ) ? $"{enumName}(Obsolete)" :enumName;       
                    Color originalColor = GUI.backgroundColor;
                    guiContext.text = (isObsolete ) ? $"{enumName}(Obsolete)" :enumName;       
                    GUI.backgroundColor = (isObsolete|isRetired)?Color.red:originalColor;
                    if (EditorGUI.DropdownButton(rect,guiContext, FocusType.Keyboard))
                    {
                        PopupEnumList(rect, prop, property.displayName);
                    }

                    GUI.backgroundColor = originalColor;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(preferredLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                    Rect rect = EditorGUILayout.GetControlRect();
                    guiContext.text = prop.GetEnumName<T>();
                    if (EditorGUI.DropdownButton(rect,guiContext, FocusType.Keyboard))
                    {
                        PopupEnumList(rect, prop, property.displayName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            };

            return container;
        }

        private void PopupEnumList( Rect rect,SerializedProperty propValue , string displayName)
        {
            var dropdown = new EnumAdvancedDropdown(
                new AdvancedDropdownState(),
                typeof(T),
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
        
#if false        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // コンテナを作成
            var container = new VisualElement();
            // プロパティを取得
            var propValue = property.FindPropertyRelative("_value");
            
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginRight = -2;
            if (string.IsNullOrEmpty(preferredLabel) is false)
            {
                var label = new Label(property.displayName);
                label.text = this.preferredLabel;
                label.style.flexGrow = 0.0f;
                label.style.minWidth = 120.0f;
                label.style.fontSize = 12.0f;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.marginLeft = 3.0f;
                label.AddToClassList("unity-base-field__label");
                container.Add(label);
            }

            var button = new Button();
            button.style.flexGrow = 1.0f;
            button.style.flexDirection = FlexDirection.Row;
            button.style.justifyContent = Justify.FlexStart;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.marginLeft = 0.0f;
            button.style.marginRight = 0.0f;
            button.style.paddingLeft = 3.333f;
            button.style.paddingRight = 3.333f;
            
            var text = new Label();
            text.style.justifyContent = Justify.FlexStart;
            text.style.unityTextAlign = TextAnchor.MiddleLeft;
            text.style.flexGrow = 1.0f;
            text.style.overflow =Overflow.Hidden;
            text.style.whiteSpace = WhiteSpace.NoWrap;
            text.style.paddingLeft = 0;
            text.style.paddingRight = 0;
            button.Add(text);

            void SetSelectText()
            {
                text.text = propValue.enumDisplayNames[propValue.enumValueIndex];
                var enumValue = Enum.GetValues(typeof(T)).GetValue(propValue.enumValueIndex);
                var field = enumValue.GetType().GetField(enumValue.ToString());
                var attr = field.GetCustomAttributes(typeof(ObsoleteAttribute), false);
                text.style.color = (attr.Length > 0)?Color.deepPink:StyleKeyword.Null;
                text.text = (attr.Length == 0) ? 
                    propValue.enumDisplayNames[propValue.enumValueIndex] :
                    propValue.enumDisplayNames[propValue.enumValueIndex] + "(Obsolete)";
            }
            SetSelectText();
            
            var arrow = new Image();
            arrow.image = dropdownTexture2D;
            button.Add(arrow);        
    //        button.text = propValue.enumDisplayNames[propValue.enumValueIndex];
            // 3. クリックイベントを登録
            button.clicked += () =>
            {
                EditorApplication.delayCall += () =>
                {                
                    propValue.serializedObject.Update();
                    var dropdown = new EnumAdvancedDropdown(
                        new AdvancedDropdownState(),
                        typeof(T) ,
                        property.displayName,
                        propValue.enumValueIndex,
                        (index,name) =>
                        {
                            // 値を更新
                            propValue.enumValueIndex = index;
                            propValue.serializedObject.ApplyModifiedProperties();
                        
                            // ボタンの表示も更新
                            SetSelectText();
                        }
                    );
                    var rect = button.worldBound;
                    dropdown.Show(rect);
                };
            };
            
            button.TrackPropertyValue(propValue, (p) =>
            {
                SetSelectText();
            });

            container.Add(button);
            
            return container;
        }
#endif
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
                        order:t.GetCustomAttributes<EnumOrderAttribute>()));

                var _items = values
                    .Select( (t,i) => (index:i,name:UnityEditor.ObjectNames.NicifyVariableName(Enum.GetName(_enumType, t))))
                    .Zip(attrs, (info, attr) => (info, attr))
                    .OrderBy(t => t.attr.order.Any() ? t.attr.order.First().Order : 0);
                
                foreach (var (item,index) in _items.Select((t,i)=>(t,i)))
                {
                    bool isObsolete = item.attr.obsoletes.Any();
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

