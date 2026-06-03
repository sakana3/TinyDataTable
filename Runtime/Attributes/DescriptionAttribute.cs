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
    public class DescriptionAttribute : Attribute
    {
        private readonly string _description;

        public string Description => _description;
        
        // コンストラクタ
        public DescriptionAttribute( string description ) => this._description = description;
    }
}