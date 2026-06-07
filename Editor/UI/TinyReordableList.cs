using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    public class TinyReordableListField : VisualElement
    {
        private SerializedProperty property;
        
        /// <summary> </summary>
        private ListView listView;
        /// <summary> </summary>
        private Toggle expendToggle;
        /// <summary> </summary>
        public IntegerField arraySizeField;
        /// <summary> </summary>
        private Label foldoutText;
        /// <summary> </summary>
        private VisualElement foldoutElement;

        private static Texture2D iconOn = EditorGUIUtility.IconContent("d_IN_foldout_on@2x").image as Texture2D;
        private static Texture2D iconOff = EditorGUIUtility.IconContent("d_IN_foldout@2x").image as Texture2D;
        private static Texture2D iconAdd = EditorGUIUtility.IconContent("d_Toolbar Plus").image as Texture2D;
        private static Texture2D iconDec = EditorGUIUtility.IconContent("d_Toolbar Minus").image as Texture2D;

        private bool _isExpanded;

        /// <summary>
        /// このプロパティはリストが展開されているか(展開状態)を判定します。
        /// trueの場合、リスト要素が表示され、falseの場合、リスト要素は非表示になります。
        /// リストの表示/非表示制御に使用されます。
        /// </summary>
        private bool isExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                foldoutElement.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
                listView.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// 指定されたSerializedPropertyを基に再配置可能なリストフィールドを作成します。
        /// </summary>
        public TinyReordableListField(SerializedProperty arrayProp)
        {
            arrayProp.serializedObject.Update();
            property = arrayProp;
            
            this.style.flexDirection = FlexDirection.Row;

            // Toggleの初期化
            expendToggle = SetupFoldToggle(arrayProp);
            this.Add(expendToggle);
            
            // リストの初期化
            listView = SetupListView(arrayProp);
            this.Add(listView);

            // Foldトグルの初期化
            SetupFoldedElements(arrayProp);

            Refresh();
            
            this.TrackPropertyValue( property , prop => Refresh());

        }

        /// <summary>
        /// 指定されたSerializedPropertyを基に折りたたみ要素を初期化し、設定します。
        /// </summary>
        /// <param name="arrayProp">折りたたみ要素を設定する元となるSerializedProperty。</param>
        /// <returns>初期化されたVisualElementの折りたたみ要素を返します。</returns>
        public void SetupFoldedElements(SerializedProperty arrayProp)
        {
            foldoutElement = new VisualElement();
            foldoutElement.style.flexDirection = FlexDirection.Row;
            foldoutElement.style.flexGrow = 1.0f;
            foldoutElement.AddToClassList("unity-list-view__empty-label");
            foldoutText = new Label(arrayProp.arraySize == 0 ? "List is empty" : $"Array size {arrayProp.arraySize}");
            foldoutElement.Add( foldoutText );            

            Add( foldoutElement );
        }


        /// <summary>
        /// 指定されたSerializedPropertyを基にリスト表示を作成します。
        /// </summary>
        /// <param name="arrayProp">リストを構築する元となるSerializedProperty。</param>
        /// <returns>リストのUI要素であるListViewを返します。</returns>
        private ListView SetupListView(SerializedProperty arrayProp)
        {
            var listView = new UnityEngine.UIElements.ListView()
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showBoundCollectionSize = false,
                showAddRemoveFooter = true,          
                showFoldoutHeader = false,
                makeItem = () =>new PropertyField(),
                bindItem = (element, index) =>
                {
                    var field = element as PropertyField;
                    var prop = arrayProp.GetArrayElementAtIndex(index);
                    field.BindProperty( prop );

                    var labelElement = field.Q(className: "unity-base-field__label") as Label;
                    if (labelElement != null)
                    {
                        labelElement.text = $"{index}";
                        labelElement.style.minWidth = 20; // 最小幅を小さく設定
                        labelElement.style.width = 20;    // 幅を固定（必要に応じて）
                        // labelElement.style.flexBasis = 30; // Flexレイアウトでの基準幅
                    }                
                },
            };

            //Binding
            listView.bindingPath = arrayProp.propertyPath;
            listView.BindProperty( arrayProp.serializedObject);
            
            // リストの初期表示状態を設定
            listView.style.display = arrayProp.isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            listView.style.flexGrow = 1.0f;
            listView.style.marginRight = 0.0f;
            listView.style.marginLeft = 0.0f;
            listView.style.marginLeft = 0.0f;
            listView.style.marginTop = 0.0f;
            listView.style.marginBottom = 0.0f;
            
            var footer = listView.Q<VisualElement>("unity-list-view__footer");
            footer.style.display = DisplayStyle.None;            

            //SetupArraySizeField
            arraySizeField = new IntegerField();
            footer.Add( arraySizeField );
            arraySizeField.SendToBack();
            arraySizeField.value = arrayProp.arraySize;
            arraySizeField.isDelayed = true;
            arraySizeField.focusable = true;
            arraySizeField.Bind( arrayProp.serializedObject);
            arraySizeField.RegisterValueChangedCallback(OnArraySizeFieldChanged);
            arraySizeField.labelElement.style.minWidth = 45;
            listView.itemsAdded += indices =>Refresh();
            listView.itemsRemoved += indices =>Refresh();
            listView.RegisterCallback<FocusInEvent>(evt =>
            {
                footer.style.display = DisplayStyle.Flex;
            });       
            listView.RegisterCallback<FocusOutEvent>(evt =>
            {
                footer.style.display = DisplayStyle.None;
                listView.ClearSelection();
            });              
     
            return listView;
        }

        /// <summary>
        /// 指定されたSerializedPropertyを基にトグルボタンを作成します。
        /// </summary>
        private Toggle SetupFoldToggle(SerializedProperty arrayProp)
        {
            // Toggleの初期化
            var toggle = new Toggle()
            {
                value = arrayProp.isExpanded, // 初期値をプロパティから設定
            };

            toggle.style.marginRight = 0.0f;
            toggle.style.marginLeft = 0.0f;
            toggle.style.width = StyleKeyword.Auto;
            toggle.style.alignSelf = Align.FlexStart;
            toggle.style.flexDirection = FlexDirection.Row;

            // チェックマーク部分のスタイルをカスタマイズしてアイコン化
            var checkmark = toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                // 枠線や背景を消してアイコンだけに見えるようにする
                checkmark.style.backgroundColor = Color.clear;
//                checkmark.style.borderWidth = 0;

                // 状態に合わせて画像を切り替えるローカル関数
                void UpdateIcon(bool isOn)
                {
                    checkmark.style.backgroundImage = new StyleBackground(isOn ? iconOn : iconOff);
                }

                // 初期表示の更新
                UpdateIcon(toggle.value);

                // 値変更時のコールバック登録
                toggle.RegisterValueChangedCallback(evt =>
                {
                    // プロパティの更新
                    arrayProp.isExpanded = evt.newValue;
                    // SerializedObjectへの変更適用は通常自動で行われないため、必要に応じて
                    // arrayProp.serializedObject.ApplyModifiedProperties(); 
                    // を呼びますが、isExpandedはエディタ専用の状態なので、
                    // 即座に反映されることが多いです。
                    
                    // アイコンの更新
                    UpdateIcon(evt.newValue);
                    
                    isExpanded = arrayProp.isExpanded;
                });
            }

            return toggle;
        }

        /// <summary>
        /// 現在のSerializedPropertyの状態に基づいてリストビューと関連UI要素を更新します。
        /// </summary>
        private void Refresh()
        {
            foldoutText.text = $"Array size {property.arraySize}";
            arraySizeField.SetValueWithoutNotify(property.arraySize);
            isExpanded = property.isExpanded;
            if (property.arraySize == 0)
            {
                expendToggle.style.display = DisplayStyle.None;
                isExpanded = true;
            }
            else
            {
                expendToggle.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// IntegerFieldの値が変更された際にリストのサイズを更新します。
        /// </summary>
        /// <param name="evt">フィールドの値の変更イベント。</param>
        void OnArraySizeFieldChanged(ChangeEvent<int> evt)
        {
            if (evt.previousValue == evt.newValue)
            {
                return;
            }
            else if (evt.newValue < 0)
            {
                arraySizeField.SetValueWithoutNotify(evt.previousValue);
            }
            else
            {
                int itemsCount = listView.viewController.GetItemsCount();
                if (evt.newValue > itemsCount)
                {
                    listView.viewController.AddItems(evt.newValue - itemsCount);
                }
                else if (evt.newValue < itemsCount)
                {
                    var items = Enumerable.Range(evt.newValue, itemsCount - evt.newValue);
                    listView.viewController.RemoveItems(items.ToList());
                }
                else if (evt.newValue == 0)
                {
                    listView.viewController.ClearItems();
                }
            }
        }
    }
}
