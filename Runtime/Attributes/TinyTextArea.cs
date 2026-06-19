using UnityEngine;
using System;
using System.Diagnostics;

namespace TinyDataTable
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class TinyTextAreaAttribute : PropertyAttribute
    {
        public int MinLines { get; }
        public int MaxLines { get; }
        public float FontSize { get; }

        // デフォルトで「1行スタート、最大3行まで自動伸縮」にする
        public TinyTextAreaAttribute(int minLines = 1, int maxLines = 0, float fontSize = 12f)
        {
            MinLines = minLines;
            MaxLines = maxLines;
            FontSize = fontSize;
        }
    }
}