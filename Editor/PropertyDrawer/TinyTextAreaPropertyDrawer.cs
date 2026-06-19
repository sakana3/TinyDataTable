using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

// ==========================================
// 2. 描画ロジック（PropertyDrawer）の定義
// ==========================================
namespace TinyDataTable.Editor
{
    [CustomPropertyDrawer(typeof(TinyTextAreaAttribute))]
    public class TinyTextAreaDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // 属性に設定された最小・最大行数を取得
            var attr = (TinyTextAreaAttribute)attribute;

            // 💡 複数行入力を有効にしたTextFieldを生成してデータバインド
            var textField = new TextField()
            {
                multiline = true,
                bindingPath = property.propertyPath
            };

            // 🔥 【ハック1】配列内でも強制的にラベルの「真横（Row）」に並ばせる
            // 標準のTextAreaが下に回り込んでしまう原因をこれで潰します
            textField.style.flexDirection = FlexDirection.Row;
            textField.style.alignItems = Align.FlexStart; // 上寄せ

            // 🔥 【ハック2】内部のテキスト入力エリア（input）を狙い撃ちして自動伸縮させる
            textField.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                var inputElement = textField.Q(className: "unity-text-field__input");
                if (inputElement != null)
                {
                    // 高さを固定（Fixed）ではなく、文字量連動（Auto）にする
                    inputElement.style.height = StyleKeyword.Auto;

                    // 行数に応じた最小・最大高さをピクセルで計算（1行あたり約18px）
                    float lineHeight = 18f;
                    inputElement.style.minHeight = attr.MinLines * lineHeight;
                    if (attr.MaxLines > 0)
                    {
                        inputElement.style.maxHeight = attr.MaxLines * lineHeight;
                    }
                    else
                    {
                        inputElement.style.maxHeight = StyleKeyword.Auto;
                    }
                    inputElement.style.fontSize = attr.FontSize;

                    // 文字の自動折り返しを有効化
                    inputElement.style.whiteSpace = WhiteSpace.Normal;
                }
            });

            return textField;
        }
    }
}