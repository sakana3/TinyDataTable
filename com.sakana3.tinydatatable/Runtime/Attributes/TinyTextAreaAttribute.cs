using UnityEngine;
using System;
using System.Diagnostics;

namespace TinyDataTable
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class TinyTextAreaAttribute : PropertyAttribute
    {
        public float FontSize { get; }
        public bool NoWrap { get; }

        // デフォルトで「1行スタート、最大3行まで自動伸縮」にする
        public TinyTextAreaAttribute( float fontSize = 12f , bool noWarp = false)
        {
            FontSize = fontSize;
            NoWrap = noWarp;
        }
    }
}