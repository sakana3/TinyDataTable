using System;
using System.Linq;
using System.Diagnostics;

namespace TinyDataTable.Editor
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class)]
    public class AttributeOptionAttribute : Attribute
    {
        public Type[] TargetTypes { get; }

        public bool HasType(Type type) => 
            TargetTypes == null || 
            (TargetTypes.Any() is false) ||
            TargetTypes.Contains(type);

        public AttributeOptionAttribute( params Type[] targetTypes)
        {
            TargetTypes = targetTypes;
        }
    }
}