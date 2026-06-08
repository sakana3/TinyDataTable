using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary> </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field , AllowMultiple = false)]
    public class AttributeMetaDataAttribute : Attribute
    {
        public (Type Type,string[] Args) Attribute { private set; get; }
        
        public AttributeMetaDataAttribute( Type type, params string[] args )
        {
            Attribute = (type,args);
        }
    }
}