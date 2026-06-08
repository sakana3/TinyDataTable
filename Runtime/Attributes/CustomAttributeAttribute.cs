using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary> </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field , AllowMultiple = false)]
    public class CustomAttributeAttribute : Attribute
    {
        public string[] Attributes { private set; get; }
        
        // コンストラクタ
        public CustomAttributeAttribute( params string[] attributes )
        {
            Attributes = attributes;
        }
        
        public CustomAttributeAttribute( params (Type type,string args)[] attributes )
        {
//            Attributes = attributes;
        }
    }
}