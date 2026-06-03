using System;
using System.Diagnostics;

namespace TinyDataTable
{
    /// <summary>
    /// Enumの並び順を指定するためのアトリビュート
    /// Enumには固有のIDをつけているがEnumは値でソートされるため
    /// このアトリビュートでオーダーを指定する
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = false)]
    public class RecordAttribute : Attribute
    {
        public Type IdentifierType { private set; get; }
        public Type RecordType { private set; get; }
        public string BaseName { private set; get; }
        
        // コンストラクタ
        public RecordAttribute( Type recordType , Type identifierType , string baseName )
        {
            IdentifierType = identifierType;
            RecordType = recordType;
            BaseName = baseName;
        }
    }
}