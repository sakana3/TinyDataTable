using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary> </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field , AllowMultiple = false)]
    public class TINYAttribute : Attribute
    {
    }
}