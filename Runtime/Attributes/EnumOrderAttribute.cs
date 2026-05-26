using System;

namespace TinyDataTable
{
    /// <summary>
    /// Enumの並び順を指定するためのアトリビュート
    /// Enumには固有のIDをつけているがEnumは値でソートされるため
    /// このアトリビュートでオーダーを指定する
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumOrderAttribute : Attribute
    {
        private int order;

        public int Order => order;
        
        // コンストラクタ
        public EnumOrderAttribute( int order ) => this.order = order;
    }
}