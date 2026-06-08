using System;
using System.Linq;
using System.Diagnostics;

namespace TinyDataTable.Editor
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AttributeOptionAttribute : Attribute
    {
        public Type[] TargetTypes { private set; get; }
        public Type TargetAttributeType { private set; get; }

        public bool HasType(Type type) => TargetTypes.Contains(type);

        public AttributeOptionAttribute( Type targetAttributeType,params Type[] targetTypes)
        {
            TargetAttributeType = targetAttributeType;
            TargetTypes = targetTypes;
        }
    }
}