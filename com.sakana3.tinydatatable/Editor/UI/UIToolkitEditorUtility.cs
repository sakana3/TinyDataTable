using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace TinyDataTable.Editor
{
    internal static class UIToolkitEditorUtility
    {
        public static (VisualElement container, Label label) CreateLabeledVisualElement(VisualElement element)
        {
            return CreateLabeledVisualElement(element.name, element);
        }

        /// <summary>
        /// Field風のレイアウトに配置する
        /// </summary>
        public static (VisualElement container, Label label) CreateLabeledVisualElement(string labelText,
            VisualElement element)
        {
            // 1. コンテナ (行)
            var container = new VisualElement();
            Label label = null;

            // Unity標準のフィールドと同じクラスを付与してレイアウトを合わせる
            container.AddToClassList("unity-base-field");
            container.AddToClassList("unity-base-field__aligned"); // ラベル幅を揃える

            // 2. ラベル
            if (string.IsNullOrEmpty(labelText) is false)
            {
                label = new Label(labelText);
                label.AddToClassList("unity-base-field__label");

                container.Add(label);
            }

            element.style.flexGrow = 1; // 右側いっぱいに広げる
            element.style.unityTextAlign = TextAnchor.MiddleLeft; // 左寄せ

            container.Add(element);

            return (container, label);
        }

        /// <summary>
        /// ポップアップ風のボタンを作成する
        /// </summary>
        public static (Button button, Label buttonText) CreatePopupButton(string labelText)
        {
            // ボタン (入力部分)
            var button = new Button();
            button.AddToClassList("unity-base-field__input");
            button.AddToClassList("unity-base-popup-field__input");

            // スタイル調整
            button.style.flexDirection = FlexDirection.Row;
            button.style.justifyContent = Justify.SpaceBetween;

            // ボタン内のテキスト
            var textLabel = new Label(labelText);
            textLabel.style.marginLeft = 2;
            textLabel.style.alignSelf = Align.Center;
            button.Add(textLabel);

            // 矢印アイコン
            var arrow = new VisualElement();
            arrow.AddToClassList("unity-base-popup-field__arrow");
            button.Add(arrow);

            return (button, textLabel);
        }
    }
}
