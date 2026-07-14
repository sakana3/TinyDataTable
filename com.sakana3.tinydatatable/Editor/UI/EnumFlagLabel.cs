using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal class EnumFlagLabel : VisualElement
    {
        private SerializedProperty _property;
        private Type _enumType;
        
        public EnumFlagLabel(SerializedProperty property)
        {
            _property = property;
            _enumType = property.GetPropertyType();
            style.flexDirection = FlexDirection.Row;

            Add(MakeLabelArea());
            var button = new Button();
            button.iconImage = EditorResources.PlusIcon;
            button.style.height = EditorGUIUtility.singleLineHeight;
            button.style.width = EditorGUIUtility.singleLineHeight;
            button.style.SetPadding(0);
            button.style.SetBorderRadius(8);
            button.clicked += () =>
            {
                UnityEditor.PopupWindow.Show(button.worldBound, new EnumFlagLabelPopup(property));
            };
            Add(button);


        }

        private VisualElement MakeLabelArea()
        {
            var area = new VisualElement();
            area.style.flexGrow = 1.0f;
            area.style.flexDirection = FlexDirection.Row;
            area.style.flexWrap = Wrap.Wrap;
            
            area.TrackPropertyValue(_property , (p) =>
            {
                area.Clear();
                Makelabels();
            });

            void Makelabels()
            {
                var ens = Enum.GetValues(_enumType).Cast<int>()
                    .Zip(_property.enumDisplayNames, (flag,name) => (name, flag));                
                foreach (var en in ens)
                {
                    if (en.flag > 0)
                    {
                        if ((_property.enumValueFlag & en.flag) != 0)
                        {
                            area.Add(MakeLabel(en.name));
                        }
                    }
                }
            }

            Makelabels();
            return area;
        }

        private VisualElement MakeLabel(string name)
        {
            var labelArea = new VisualElement();
            labelArea.style.flexDirection = FlexDirection.Row;
            labelArea.style.backgroundColor = new Color( 0.133f,0.28f,0.5f,1.0f );
            labelArea.style.SetBorderRadius(8);
            labelArea.style.height = 16;
            labelArea.style.paddingLeft = 4;
            labelArea.style.paddingRight = 4;
            labelArea.style.SetMargine( 1.33333f, 1.33333f, 3.33333f, 3.33333f );
            labelArea.style.SetBorderWidth(1.3333f);
            
            var label = new Label(name);
            label.style.fontSize = 10;
            labelArea.Add(label);
/*            
            var remove = new Button(EditorResources.CloseIcon);
            remove.style.SetBorderRadius(8);
            remove.style.SetPadding(0);
            remove.style.SetMargine( 0 );
            remove.style.backgroundColor = Color.clear;
            remove.style.SetBorderWidth(0);
            remove.style.width = 14;
            remove.style.height = 14;
            remove.clicked += () =>
            {

            };
            labelArea.Add(remove);
*/
            return labelArea;
        }
    }

    // ... (EnumFlagLabel クラスは変更なし) ...

    internal class EnumFlagLabelPopup : PopupWindowContent
    {
        private SerializedProperty _property;
        private Type _enumType;
        private int value = 0;


        public EnumFlagLabelPopup(SerializedProperty property)
        {
            _property = property;
            // SerializedPropertyからenumの型を取得する
            _enumType = property.GetPropertyType();
            value = _property.enumValueFlag;

        }

        public override void OnGUI(Rect rect)
        {
            var currentEnumValue = Enum.ToObject(_enumType, _property.intValue);

            var ens = Enum.GetValues(_enumType).Cast<int>()
                .Zip(_property.enumDisplayNames, (flag,name) => (name, flag));

            foreach (var en in ens )
            {
                if (en.flag > 0)
                {
                    var isOn = (value & en.flag) != 0;
                    if (GUILayout.Toggle(isOn, en.name ) != isOn )
                    {
                        value = value ^ en.flag;
                    }
                }
            }
        }

        public override void OnClose()
        {
            if (_property.enumValueFlag != value)
            {
                Undo.RecordObject(_property.serializedObject.targetObject, "Flag Changed");
                _property.intValue = value;
                _property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override Vector2 GetWindowSize()
        {
            // ウィンドウの高さを1行分に設定
            return new Vector2(200, EditorGUIUtility.singleLineHeight*_enumType.GetEnumValues().Length);
        }
    }

}