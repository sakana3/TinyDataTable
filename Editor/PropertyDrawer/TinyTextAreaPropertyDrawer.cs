using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif


namespace TinyDataTable.Editor
{
    [CustomPropertyDrawer(typeof(TinyTextAreaAttribute))]
    public class TinyTextAreaDrawer : PropertyDrawer
    {
        private SerializedProperty property;
        private VisualElement root;
        private TextField textField;
        private float fontSize = 0;
        private bool noWrap  = false;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.property = property;

            var attr = (TinyTextAreaAttribute)attribute;
            
            fontSize = attr.FontSize;
            noWrap = attr.NoWrap;
            
            root = new VisualElement();
            root.style.flexGrow = 1.0f;
            root.style.flexDirection = FlexDirection.Row;

//            var fold = SetupFoldToggle(property);
//            root.Add(fold);

            textField = SetupTextField();
            root.Add(textField);                
            
            return root;
        }

        private void ExpandChanged(bool newValue)
        {
            property.isExpanded = newValue;

        }

        private TextField SetupTextField()
        {
           var text = new TextField()
            {
                multiline = true,
                bindingPath = property.propertyPath
            };
            
            text.style.flexDirection = FlexDirection.Row;
            text.style.alignItems = Align.FlexStart; // 上寄せ
            text.style.flexGrow = 1.0f;
            text.style.marginLeft = 0.0f;
            text.style.marginRight = 0.0f;

            root.Add(text);

//            SetMaxLines(maxLines, fontSize, noWrap);
/*
            text.RegisterCallbackOnce<AttachToPanelEvent>(evt =>
            {
                var inputElement = text.Q(className: "unity-text-field__input");
                if (inputElement != null)
                {
                    if (property.isExpanded)
                    {
                        inputElement.style.maxHeight = StyleKeyword.None;
                        inputElement.style.whiteSpace = WhiteSpace.Normal;
                    }
                    else
                    {
                        inputElement.style.height = 18f;
                        inputElement.style.whiteSpace = WhiteSpace.NoWrap;
                    }
                }
            });
*/            
            return text;
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
            toggle.style.width = 12;
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
                    checkmark.style.backgroundImage = new StyleBackground(isOn ? EditorResources.FoldOnIcon : EditorResources.FoldOffIcon);
                }

                // 初期表示の更新
                UpdateIcon(toggle.value);

                // 値変更時のコールバック登録
                toggle.RegisterValueChangedCallback(evt =>
                {
                    // プロパティの更新
                    arrayProp.isExpanded = evt.newValue;
                    // アイコンの更新
                    UpdateIcon(evt.newValue);
                    ExpandChanged(evt.newValue);
                });
            }

            return toggle;
        }  
    }
}