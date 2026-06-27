using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEngine.TextCore.Text;

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
        private VisualElement attributeArea;
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
            root.style.width = Length.Percent(98);
            root.style.marginBottom = 4;
            root.style.marginTop = 4;
            root.style.marginLeft = 4;
            root.style.marginRight = 4;

            var container = root.contentContainer;
            container.style.height = StyleKeyword.Auto;
            container.style.width = StyleKeyword.Auto;

            editorWindow.rootVisualElement.Add(root);

            root.contentContainer.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var size = evt.newRect.size;
                size.x = 300;
                size.y += 10;
                _windowSize = size;
            });
            root.Add(new Label(FieldInfo == null ? "Add Field" : "Refactor Field"));

            root.Add(new ToolbarSpacer());

            // クラス名入力
            {
                var block = MakeBlock(Color.blue,"NAME");
                root.Add(block.root);
                var textField = new UnityEngine.UIElements.TextField(PropertyName)
                {
                    label = "Name",
                    value = PropertyName,
                };
                textField.RegisterValueChangedCallback(evt => OnClassNameChangeCallback(textField, evt));
                textField.textEdition.placeholder = "Input name...";
                block.area.Add(textField);

                _notifyLabel = new HelpBox("Input name.", HelpBoxMessageType.Warning);
                var element = UIToolkitEditorUtility.CreateLabeledVisualElement("", _notifyLabel);
                block.area.Add(element.container);
                // 少し遅延させてフォーカス
                if (FieldInfo == null)
                {
                    textField.schedule.Execute(() => { textField.Focus(); }).StartingIn(50); // 50ms後くらい            
                }
            }

            //型選択ボタン
            {
                var block = MakeBlock(Color.teal,"TYPE");
                root.Add(block.root);
                {
                    var typeSelectButton = MakeTypeSelectorPopup();
                    block.area.Add(typeSelectButton);

                    //Is Array
                    var boolField = new UnityEngine.UIElements.Toggle("Is Array")
                    {
                        value = IsArray,
                    };
                    boolField.RegisterValueChangedCallback(evt => IsArray = evt.newValue);
                    boolField.value = IsArray;
                    block.area.Add(boolField);
                }
            }
            
            {
                attributeRoot = new VisualElement();
                CreateAttributeSelector(PropertyType);
                root.Add(attributeRoot);
            }

            {
                var block = MakeBlock(Color.maroon,"Desc");

                root.Add(block.root);
                var descriptionlabel = new Label("Description");
                block.area.Add(descriptionlabel);

                var descriptionField = new TextField();
                descriptionField.RegisterValueChangedCallback(evt => Description = evt.newValue);
                descriptionField.textEdition.placeholder = "Input Description.If you need.";
                descriptionField.value = Description;
                block.area.Add(descriptionField);
            }

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

        private void OnTypeSelectChangeCallback(Type type)
        {
            CreateAttributeSelector(type);
        }
        
        private void CreateAttributeSelector( Type type)
        {
            attributeRoot.Clear();
            attributeOptions = AttributeAdapterBase.FindAttributeOptions(type , (FieldInfo==null) ? null : FieldInfo.CustomAttributes.Select( t => t.Type ).ToArray());

            if (attributeOptions.Any())
            {
                
                var mainBlock = MakeBlock(Color.darkGoldenRod,"ATTR");                
                attributeRoot.Add(mainBlock.root);                
                
                foreach (var option in attributeOptions)
                {
                    option.InitializeFormFiledInfo(FieldInfo);
                }
                
                var label = new Label("Attributes");
                mainBlock.area.Add(label);


                var mainAttributes = attributeOptions
                    .Where(a => a.AttributeType == AttributeType.Drawer);
                var activeAttributes = mainAttributes
                    .FirstOrDefault(t => t.IsEnable);
                var index = attributeOptions.IndexOf(activeAttributes);
                
                var attrBlock = MakeBlock(Color.coral, null, 4);
                mainBlock.area.Add(attrBlock.root);
                var popup = new UnityEngine.UIElements.PopupField<string>()
                {
                    choices = new string[]{"None"}.Concat( mainAttributes.Select(t=>t.Title)).ToList(),
                    index = index + 1,
                };
                attrBlock.area.Add(popup);
                
                attributeArea = new VisualElement();
                attrBlock.area.Add(attributeArea);
                
                if (activeAttributes != null)
                {
                    attributeArea.Add(activeAttributes.CreateRootUI(false));
                }
                popup.RegisterValueChangedCallback(evt =>
                {
                    var active = mainAttributes
                        .FirstOrDefault(t => t.Title == evt.newValue);

                    attributeArea.Clear();
                    foreach (var attr in mainAttributes)
                    {
                        attr.IsEnable = false;
                    }
                    if (active != null)
                    {
                        active.IsEnable = true;
                        attributeArea.Add(active.CreateRootUI(false));
                    }
                });

                var additonalAttributes = attributeOptions
                    .Where(a => a.AttributeType == AttributeType.Additional)
                    .ToList();
                
                if (additonalAttributes.Any())
                {
                    ListView listView = new ListView()
                    {
                        name = "Attributes",
//                        reorderable = true,
                        reorderMode = ListViewReorderMode.Animated,
                        virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                    };
                    listView.makeItem = () => new VisualElement();
                    listView.bindItem = (element, i) =>
                    {
                        element.Clear();
                        var block = MakeBlock(Color.coral, null, 4);
                        element.Add(block.root);
                        var option = additonalAttributes[i];
                        var optionUI = option.CreateRootUI(true);
                        if (optionUI != null)
                        {
                            block.area.Add(optionUI);
                        }
                    };
                    listView.itemsSource = additonalAttributes;
                    mainBlock.area.Add(listView);
                }
            }
        }

        private (VisualElement root ,VisualElement header, VisualElement area) MakeBlock( Color headeColor , string title ,float width = 12)
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var header = new VisualElement();
            {
                header.style.width = width;
                header.style.marginBottom = 2;
                header.style.marginTop = 2;
                header.style.marginLeft = 2;
                header.style.marginRight = 2;
                header.style.backgroundColor = headeColor;
                header.style.justifyContent = Justify.Center; // 主軸方向（デフォルトなら縦）の中央揃え
                header.style.alignItems = Align.Center;
                root.Add(header);

                if (string.IsNullOrEmpty(title) is false)
                {
                    var label = new Label(title);
                    label.style.rotate = new Rotate(270);
                    label.style.fontSize = 9;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    header.Add(label);
                }
            }
            var area = new VisualElement();
            {
                area.style.marginBottom = 4;
                area.style.marginTop = 4;
                area.style.marginLeft = 2;
                area.style.marginRight = 2;                
                area.style.flexGrow = 1;
                root.Add(area);
            }
            return (root,header,area);
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
