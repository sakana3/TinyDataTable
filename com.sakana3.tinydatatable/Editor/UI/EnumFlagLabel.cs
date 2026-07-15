using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal class FlagLabel : VisualElement
    {
        protected VisualElement LabelArea;
        protected Button AddButton;
        
        public FlagLabel()
        {
        }

        protected void Initialize()
        {
            style.flexDirection = FlexDirection.Row;
            LabelArea = MakeLabelArea();
            Add(LabelArea);            
            
            AddButton = new Button();
            AddButton.iconImage = EditorResources.PlusIcon;
            AddButton.style.height = EditorGUIUtility.singleLineHeight;
            AddButton.style.width = EditorGUIUtility.singleLineHeight;
            AddButton.style.SetPadding(0);
            AddButton.style.SetBorderRadius(8);
            
            AddButton.style.SetBorderWidth(0);
            Add(AddButton);            
        }
        
        protected virtual IEnumerable<string> EnumrateLables()
        {
            yield break;
        }
        
        protected virtual void Makelabels( VisualElement labelArea )
        {
            foreach (var labelName in EnumrateLables() )
            {
                labelArea.Add(MakeLabel(labelName));
            }
        }
        
        protected VisualElement MakeLabelArea()
        {
            var area = new VisualElement();
            area.style.flexGrow = 1.0f;
            area.style.flexDirection = FlexDirection.Row;
            area.style.flexWrap = Wrap.Wrap;

            Makelabels(area);
            return area;
        }     
        
        protected VisualElement MakeLabel(string name)
        {
            var area = new VisualElement();
            area.style.flexDirection = FlexDirection.Row;
            area.style.backgroundColor = new Color( 0.133f,0.28f,0.5f,1.0f );
            area.style.SetBorderRadius(8);
            area.style.height = 16;
            area.style.paddingLeft = 4;
            area.style.paddingRight = 4;
            area.style.SetMargine( 1.33333f, 1.33333f, 3.33333f, 3.33333f );
            area.style.SetBorderWidth(1.3333f);
            
            var label = new Label(name);
            label.style.fontSize = 10;
            area.Add(label);
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
            return area;
        }

        protected void ResetLables()
        {
            LabelArea.Clear();
            Makelabels(LabelArea);            
        }
    }


    internal class EnumFlagLabel : FlagLabel
    {
        private SerializedProperty _property;
        private Type _enumType;
        
        public EnumFlagLabel(SerializedProperty property) : base()
        {
            _property = property;
            _enumType = property.GetPropertyType();

            Initialize();
            
            LabelArea.TrackPropertyValue(_property , (p) =>
            {
                ResetLables();
            });            
            AddButton.clicked += () =>
            {
                UnityEditor.PopupWindow.Show(AddButton.worldBound, new EnumFlagLabelPopup(property));
            };
        }
        
        protected override IEnumerable<string> EnumrateLables()
        {
            var ens = Enum.GetValues(_enumType).Cast<int>()
                .Zip(_property.enumDisplayNames, (flag,name) => (name, flag));                    
            foreach (var en in ens)
            {
                if (en.flag > 0)
                {
                    if ((_property.enumValueFlag & en.flag) != 0)
                    {
                        yield return en.name;
                    }
                }
            }            
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