using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.IMGUI.Controls;

namespace TinyDataTable.Editor
{
    // ポップアップウィンドウのコンテンツ
    internal class DataTableCreateFieldPopup : PopupWindowContent
    {
        private readonly Action<FieldInfo> _onAdd;
        private Vector2 _windowSize = new Vector2(300, 200);

        public string PropertyName { set; get; } = "";
        public Type PropertyType { set; get; } = typeof(int);
        public string Description { set; get; } = "";
        public bool IsArray { set; get; } = false;

        private UnityEngine.UIElements.TextField _textField;
        private HelpBox _notifyLabel;
        private Button _decideButton;
        private string[] _assemblys;
        private string _baseClassName;
        private List<string> _customAttributes = new List<string> { };
        private IReadOnlyCollection<string> propNames  {set; get; } = new List<string>();
        private IReadOnlyCollection<string> idNames {set; get; }= new List<string>();
        private IReadOnlyCollection<string> reservNames {set; get; }= new List<string>();
        private VisualElement attributeRoot;
        private FieldInfo FieldInfo { set; get; }
        private List<AttributeAdapterBase> attributeOptions = new ();

        public DataTableCreateFieldPopup(string[] assemblys,Action<FieldInfo> onAdd)
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
//            var root = editorWindow.rootVisualElement;
            if (FieldInfo != null)
            {
                PropertyType = FieldInfo.Type.IsArray ? FieldInfo.Type.GetElementType() : FieldInfo.Type;
                PropertyName = FieldInfo.Name;
                Description = FieldInfo.Description;
                IsArray = FieldInfo.Type.IsArray;
            }
            
            var root = new ScrollView(ScrollViewMode.Vertical);
            root.style.flexGrow = 1; 
            root.style.height = Length.Percent(100);            
            root.style.width = Length.Percent(100);            
            
            var container = root.contentContainer;
            container.style.height = StyleKeyword.Auto;
            container.style.width = StyleKeyword.Auto;            

            editorWindow.rootVisualElement.Add(root);           
            
            root.contentContainer.RegisterCallback<GeometryChangedEvent>( 
                evt =>
                {
                    var size = evt.newRect.size;
                    size.x = 300;
                    _windowSize = size;
                });
            root.Add(new Label(FieldInfo == null ? "Add Field" :  "Refactor Field"));

            root.Add(new ToolbarSpacer());
            
            // クラス名入力
            var textField = new UnityEngine.UIElements.TextField(PropertyName)
            {
                label = "Name",
                value = PropertyName,
            };
            textField.RegisterValueChangedCallback(evt => OnClassNameChangeCallback(textField,evt));
            textField.textEdition.placeholder = "Input name...";
            root.Add( textField );

            _notifyLabel = new HelpBox( "Input name.", HelpBoxMessageType.Warning);
            var element = UIToolkitEditorUtility.CreateLabeledVisualElement("", _notifyLabel);
            root.Add( element.container );
            // 少し遅延させてフォーカス
            if (FieldInfo == null)
            {
                textField.schedule.Execute(() => { textField.Focus(); }).StartingIn(50); // 50ms後くらい            
            }

            //型選択ボタン
            var typeSelectButton = MakeTypeSelectorPopup();
            root.Add( typeSelectButton );
    
            //Is Array
            var boolField = new UnityEngine.UIElements.Toggle("Is Array")
            {
                value = IsArray,
            };
            boolField.RegisterValueChangedCallback(evt => IsArray = evt.newValue);
            boolField.value = IsArray;
            root.Add( boolField );
            
            attributeRoot = new VisualElement();
            OnTypeSelectChangeCallback(PropertyType);
            root.Add(attributeRoot);

            var descriptionField = new TextField("Description");
            descriptionField.RegisterValueChangedCallback(evt => Description = evt.newValue);
            descriptionField.textEdition.placeholder = "Input Description.If you need.";
            descriptionField.value = Description;
            root.Add( descriptionField );
            
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            root.Add( spacer );
            
            _decideButton = new Button(InvokeOnAdd) { text = FieldInfo == null ? "Add" : "Refactor" };
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
            string baseClassName,
            IReadOnlyCollection<string> propNames,
            IReadOnlyCollection<string> idNames,
            IReadOnlyCollection<string> reservNames,
            string[] assermblys,
            Action<FieldInfo> onAdd ,
            FieldInfo fieldInfo = null
            )
        {
            var popup = new DataTableCreateFieldPopup(assermblys,onAdd)
            {
                _baseClassName = baseClassName,
                propNames = propNames,
                idNames = idNames,
                reservNames = reservNames,
                FieldInfo = fieldInfo
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
                    GUILayout.Label("Type", GUILayout.Width(120));
                    Rect rect = EditorGUILayout.GetControlRect();
                    rect.width -= 32;
                    if (EditorGUI.DropdownButton(rect, new GUIContent(PropertyType.GetCSharpAlias()), FocusType.Keyboard))
                    {
                        var state = new AdvancedDropdownState();                        
                        var dropdown = new TypeSelectorDropdown(state, _assemblys, (selectedType) => 
                        {
                            PropertyType = selectedType;
                            OnTypeSelectChangeCallback(selectedType);
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
            var field = UIToolkitEditorUtility.CreateLabeledVisualElement("Type",popup.button);

            popup.button.clicked += () =>
            {
                var state = new AdvancedDropdownState();
                var types = new[] { typeof(int), typeof(string), typeof(Vector3) /* ... */ };

                var dropdown = new TypeSelectorDropdown(state, _assemblys, (selectedType) => 
                {
                    PropertyType = selectedType;
                    popup.buttonText.text = PropertyType.Name;
                    OnTypeSelectChangeCallback(selectedType);
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

        
        private void OnTypeSelectChangeCallback( Type type)
        {
            attributeRoot.Clear();
            attributeOptions = AttributeAdapterBase.FindAttributeOptions(type , (FieldInfo==null) ? null : FieldInfo.CustomAttributes.Select( t => t.Type ).ToArray());
            foreach (var option in attributeOptions)
            {
                option.FormFiledInfo(FieldInfo);
            }
            
            ListView listView = new ListView()
            {
                name = "Attributes",
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            };
            listView.makeItem = () => new VisualElement();
            listView.bindItem = (element, i) =>
            {
                element.Clear();
                var option = attributeOptions[i];
                var optionUI = option.MakeUI();
                if (optionUI != null)
                {
                    element.Add(optionUI);
                }
            };
            listView.itemsSource = attributeOptions;
            attributeRoot.Add(listView);
        }

        private void CheckClassName()
        {
            string text = string.Empty;
            var messageType = HelpBoxMessageType.Info;

            if (FieldInfo != null && FieldInfo.Name == PropertyName)
            {
            }
            else if (FieldInfo != null && FieldInfo.Obsolete is false)
            {
                text = "Renaming this will introduce a breaking change. Please mark it as Obsolete and resolve the warnings before making the change.";
                messageType = HelpBoxMessageType.Warning;
            }
            else if (string.IsNullOrEmpty(PropertyName))
            {
                text = "Input name.";
                messageType = HelpBoxMessageType.Info;
            }
            else if (SerializableUtility.CheckCSharpSafeName(PropertyName) is false)
            {
                text = "Invalid name.";
                messageType = HelpBoxMessageType.Error;
            }
            else if( _baseClassName == PropertyName )
            {
                text = "It has the same name as the class.";
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
            FieldInfo info = new()
            {
                Type = !IsArray ? PropertyType : PropertyType.MakeArrayType(),
                Description = Description,
                Name = PropertyName,
            };
          
            info.CustomAttributes = attributeOptions
                .Where(t => t.IsEnable)
                .Select(t => t.AttributeValue )
                .Where(t =>t.type != null )
                .ToArray();
     
            _onAdd(info);
            editorWindow.Close();
        }
    }
}
