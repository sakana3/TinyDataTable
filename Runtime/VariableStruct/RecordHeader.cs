using System;

namespace TinyDataTable
{
    /// <summary>
    /// レコードのヘッダ
    /// </summary>
    [Serializable]
    public struct RecordHeader
    {
        public RecordFieldInfo[] fieldInfos;
    }

    /// <summary>
    /// レコードフィールド情報
    /// </summary>
    [Serializable]
    public struct RecordFieldInfo
    {
        public string name;
        public string description;
        public int id;
        public int index;
        public bool obsolete;
    }

    /// <summary>
    /// レコードデータヘッダ
    /// </summary>
    [Serializable]
    public struct RecordDataHeader
    {
        public string name;
        public string description;
        public int id;
        public int index;
        public bool obsolete;
    }
}