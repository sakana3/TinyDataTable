using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal static class UIElementExtensions
    {

        public static void SetPadding(this UnityEngine.UIElements.IStyle style,
            float top, float bottom, float left, float right)
        {
            style.paddingBottom = bottom;
            style.paddingTop = top;
            style.paddingLeft = left;
            style.paddingRight = right;
        }

        public static void SetPadding(this UnityEngine.UIElements.IStyle style, float value) =>
            style.SetPadding(value, value, value, value);

        public static void SetBorderRadius( this UnityEngine.UIElements.IStyle style ,
            float topLeft, float topRight, float bottomLeft, float bottomRight)
        {
            style.borderTopLeftRadius = topLeft;
            style.borderTopRightRadius = topRight;
            style.borderBottomLeftRadius = bottomLeft;
            style.borderBottomRightRadius = bottomRight;
        }

        public static void SetBorderRadius(this UnityEngine.UIElements.IStyle style, float value) =>
            style.SetBorderRadius(value, value, value, value);
        
        public static void SetBorderWidth( this UnityEngine.UIElements.IStyle style ,
            float top, float bottom, float left, float right)
        {
            style.borderTopWidth = top;
            style.borderBottomWidth = bottom;
            style.borderLeftWidth = left;
            style.borderRightWidth = right;
        }
        public static void SetBorderWidth(this UnityEngine.UIElements.IStyle style, float value) =>
            style.SetBorderWidth(value, value, value, value);

        public static void SetMargine( this UnityEngine.UIElements.IStyle style ,
            float top, float bottom, float left, float right)
        {
            style.marginTop = top;
            style.marginBottom = bottom;
            style.marginLeft = left;
            style.marginRight = right;
        }        
        public static void SetMargine(this UnityEngine.UIElements.IStyle style, float value) =>
            style.SetMargine(value, value, value, value);
    }
}