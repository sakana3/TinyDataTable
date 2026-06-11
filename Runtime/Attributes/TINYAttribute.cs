using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary>
    /// TinyDataTableによって生成されたFieldだと認識させる為のアトリビュート
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field , AllowMultiple = false)]
    public class TINYAttribute : Attribute
    {
    }
}