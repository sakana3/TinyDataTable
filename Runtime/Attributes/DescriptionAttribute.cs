using System;
using System.Diagnostics;
    
namespace TinyDataTable.Description
{
    /// <summary>
    /// リリース時に外されるDescription
    /// System.ComponentModel.DescriptionAttributeはIDEがコメントとして認識するがリリースに含まれてしまう為
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.All , AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        private readonly string _description;

        public string Description => _description;

        // コンストラクタ
        public DescriptionAttribute(string description) => this._description = description;
    }
}