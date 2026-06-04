using System;

namespace TinyDataTable
{
    /// <summary>
    /// レコードフィールド情報
    /// </summary>
    [Serializable]
    public struct RecordFieldInfo
    {
        public string name;
        public string description;
        public int id;
        public bool obsolete;
        public Type type;
    }
}