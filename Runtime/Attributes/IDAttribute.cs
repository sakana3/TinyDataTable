using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary>
    /// TinyDataTableによって生成されたFieldだと認識させる為のアトリビュート
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Struct , AllowMultiple = false)]
    public class IDAttribute : Attribute
    {
        public Type RecordType { private set; get; }
        public IDAttribute(Type recordType)
        {
            RecordType = recordType;
        }
    }
}