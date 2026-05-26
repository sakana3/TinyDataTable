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
    // ポップアップウィンドウのコンテンツ
    public class DataTableAddPropertyPopup : PopupWindowContent
    {
        private readonly Action<Type, string, bool,string> _onAdd;
        private Vector2 _windowSize = new Vector2(300, 200);

        public string PropertyName { set; get; } = "";
        public Type PropertyType { set; get; } = typeof(int);
        public string Description { set; get; } = "";
        public bool IsArray { set; get; } = false;

        private UnityEngine.UIElements.TextField _textField;
        private HelpBox _notifyLabel;
        private Button _decideButton;
        private string[] _assemblys;
        public List<string> propNames  {set; get; } = new List<string>();
        public List<string> idNames {set; get; }= new List<string>();
        public List<string> reservNames {set; get; }= new List<string>();
        
        private static Type[] types = new []
        {
            typeof(int),
            typeof(float),
            typeof(bool),
            typeof(string),
            typeof(long),
            typeof(double),
            typeof(Color),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Rect),
            typeof(Bounds),
            typeof(AnimationCurve),
            typeof(Gradient),
            typeof(Sprite),
        };


        
        public DataTableAddPropertyPopup(string[] assemblys,Action<Type, string, bool,string> onAdd)
        {
            _assemblys = assemblys;
            _onAdd = onAdd;
        }

        public override Vector2 GetWindowSize()
        {
            return _windowSize;
        }

        // ウィンドウが開いたときの初期化        
        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;
            root.Add(new Label("Add Field"));

            root.Add(new ToolbarSpacer());
            
            // クラス名入力
            var textField = new UnityEngine.UIElements.TextField(PropertyName)
            {
                label = "Field Name",
                value = PropertyName,
                
            };
            textField.RegisterValueChangedCallback(evt => OnClassNameChangeCallback(textField,evt));
            textField.textEdition.placeholder = "Input field name...";　
            root.Add( textField );

            _notifyLabel = new HelpBox( "Input field name.", HelpBoxMessageType.Warning);
            var element = UIToolkitEditorUtility.CreateLabeledVisualElement("", _notifyLabel);
            root.Add( element.container );
            // 少し遅延させてフォーカス
            textField.schedule.Execute(() => 
            {
                textField.Focus();
            }).StartingIn(50); // 50ms後くらい            

            //型選択ボタン
            var typeSelectButton = MakeTypeSelectorPopup();
            root.Add( typeSelectButton );
    
            var boolField = new UnityEngine.UIElements.Toggle("Is Array")
            {
                value = IsArray,
            };
            boolField.RegisterValueChangedCallback(evt => IsArray = evt.newValue);
            root.Add( boolField );

            var DescriptionField = new TextField("Description")
            {

            };
            DescriptionField.RegisterValueChangedCallback(evt => Description = evt.newValue);
            DescriptionField.textEdition.placeholder = "Input Description.If you need.";　
            root.Add( DescriptionField );
            
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            root.Add( spacer );
            
            _decideButton = new Button(InvokeOnAdd) { text = "Add" };
            root.Add(_decideButton);

            CheckClassName();
        }

        // ウィンドウが閉じたときの後処理
        public override void OnClose()
        {
        }
        
        // 呼び出し用の静的ヘルパーメソッド
        public static void Show(
            Rect activatorRect,
            List<string> propNames,
            List<string> idNames,
            List<string> reservNames,
            string[] assermblys,
            Action<Type, string, bool,string> onAdd)
        {
            var popup = new DataTableAddPropertyPopup(assermblys,onAdd)
            {
                propNames = propNames,
                idNames = idNames,
                reservNames = reservNames
            };
            UnityEditor.PopupWindow.Show(activatorRect, popup);
        }

        private VisualElement MakeTypeSelectorPopup()
        {
            var gui = new IMGUIContainer();
            gui.onGUIHandler += () =>
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Field type", GUILayout.Width(120));
                    Rect rect = EditorGUILayout.GetControlRect();
                    rect.width -= 32;
                    if (EditorGUI.DropdownButton(rect, new GUIContent(PropertyType.Name), FocusType.Keyboard))
                    {
                        var state = new AdvancedDropdownState();                        
                        var dropdown = new TypeSelectorDropdown(state, _assemblys, (selectedType) => 
                        {
                            PropertyType = selectedType;
                        });
                        dropdown.Show(rect);                        
                    }
                }
            };
            return gui;
        }

        private VisualElement MakeTypeSelectorPopup_()
        {
            var popup = UIToolkitEditorUtility.CreatePopupButton(PropertyType.Name);
            var field = UIToolkitEditorUtility.CreateLabeledVisualElement("Field Type",popup.button);

            popup.button.clicked += () =>
            {
                var state = new AdvancedDropdownState();
                var types = new[] { typeof(int), typeof(string), typeof(Vector3) /* ... */ };

                var dropdown = new TypeSelectorDropdown(state, _assemblys, (selectedType) => 
                {
                    PropertyType = selectedType;
                    popup.buttonText.text = PropertyType.Name;
                });

                dropdown.Show(popup.button.worldBound);
            };

            return field.container;
        }        
        
        private void OnClassNameChangeCallback(TextField textField , ChangeEvent<string> evt )
        {
            PropertyName = evt.newValue;
            CheckClassName();
        }

        private void CheckClassName()
        {
            string text = string.Empty;
            var messageType = HelpBoxMessageType.Info;
            
            if (string.IsNullOrEmpty(PropertyName))
            {
                text = "Input field name.";
                messageType = HelpBoxMessageType.Info;
            }
            else if (DataSheetPropertyUtility.CheckCSharpSafeName(PropertyName) is false)
            {
                text = "Invalid field name.";
                messageType = HelpBoxMessageType.Error;
            }
            else if (propNames.Any( t => t == PropertyName))
            {
                text = "Name already exists.";
                messageType = HelpBoxMessageType.Error;
            }
            else if (idNames.Any( t => t == PropertyName))
            {
                text = "Name already exists.";
                messageType = HelpBoxMessageType.Error;
            }
            else if (reservNames.Any( t => t == PropertyName))
            {
                text = "Reserved word.";
                messageType = HelpBoxMessageType.Error;
            }

            if ( string.IsNullOrEmpty(text) is false)
            {
                _decideButton.tooltip = text;
                _decideButton.displayTooltipWhenElided = true;
                _decideButton.SetEnabled(false);
                _notifyLabel.style.display = DisplayStyle.Flex;
                _notifyLabel.style.fontSize = 10.0f;
                _notifyLabel.messageType = messageType;
                _notifyLabel.text = text;
            }
            else
            {
                _notifyLabel.style.display = DisplayStyle.None;                
                _decideButton.SetEnabled(true);
            }
        }
        
        private void InvokeOnAdd()
        {
            _onAdd(PropertyType, PropertyName, IsArray,Description);
            editorWindow.Close();
        }
    }
}
