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
    public class UIToolkitEditorUtility
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

            // Unity標準のフィールドと同じクラスを付与してレイアウトを合わせる
            container.AddToClassList("unity-base-field");
            container.AddToClassList("unity-base-field__aligned"); // ラベル幅を揃える

            // 2. ラベル
            var label = new Label(labelText);
            label.AddToClassList("unity-base-field__label");

            container.Add(label);

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


        
        /// <summary>
        /// 指定された型がUnityでシリアライズ可能かどうかを判定する
        /// </summary>
        public static bool CheckUnitySerializable(Type type)
        {
            if (type == null) return false;

            //コンパイラが自動生成したものは除外
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return false;
            }
            
            // 1. プリミティブ型と文字列
            if (type.IsPrimitive || type == typeof(string)) return true;

            // 2. Enum
            if (type.IsEnum) return true;

            // 3. Unity Object (参照として保存可能)
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;

            // 4. 配列とリスト
            if (type.IsArray)
            {
                // 多次元配列は不可
                if (type.GetArrayRank() > 1) return false;
                return CheckUnitySerializable(type.GetElementType());
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return CheckUnitySerializable(type.GetGenericArguments()[0]);
            }

            // 5. Unityの特定の組み込み構造体 (代表的なもの)
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Quaternion) || type == typeof(Matrix4x4) ||
                type == typeof(Color) || type == typeof(Color32) ||
                type == typeof(Rect) || type == typeof(Bounds) ||
                type == typeof(LayerMask) || type == typeof(AnimationCurve) || type == typeof(Gradient) ||
                type == typeof(RectOffset) || type == typeof(GUIStyle) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(RectInt) || type == typeof(BoundsInt))
            {
                return true;
            }

            // 6. [Serializable] 属性を持つクラス・構造体
            if (type.IsSerializable) // System.SerializableAttribute が付いているか
            {
                // ジェネリック定義そのもの (List<>など) は不可
                if (type.IsGenericTypeDefinition) return false;
                
                // decimal, DateTime, Dictionary など、.NETではSerializableだがUnityでは非対応なものを除外
                if (type == typeof(decimal) || type == typeof(DateTime) || type == typeof(TimeSpan) || 
                    type == typeof(Guid) || type == typeof(Uri))
                {
                    return false;
                }
                
                // ジェネリック型の場合、型引数もシリアライズ可能である必要がある
                if (type.IsGenericType)
                {
                    foreach (var arg in type.GetGenericArguments())
                    {
                        if (!CheckUnitySerializable(arg)) return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
