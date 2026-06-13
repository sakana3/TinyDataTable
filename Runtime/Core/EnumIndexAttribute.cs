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
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false)]
    public class EnumIndexAttribute : Attribute
    {
        private readonly int _order;

        public int Order => _order;
        
        // コンストラクタ
        public EnumIndexAttribute( int order ) => this._order = order;
    }
}