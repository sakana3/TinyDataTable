using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary> </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = false)]
    public class RecordAttribute : Attribute
    {
        public Type IdentifierType { private set; get; }
        public Type SchemaType { private set; get; }
        public Type EnumType { private set; get; }
        public string BaseName { private set; get; }
        
        // コンストラクタ
        public RecordAttribute( Type schemaType,Type enumType , Type identifierType , string baseName )
        {
            SchemaType = schemaType;
            EnumType = enumType;
            IdentifierType = identifierType;
            BaseName = baseName;
        }
    }
}