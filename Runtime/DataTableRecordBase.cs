using System;
using UnityEngine;

namespace TinyDataTable
{
    /// <summary> データテーブルレコードの基底クラス </summary>
    public abstract class DataTableRecordBase : ScriptableObject
    {
        [SerializeField] private bool _initializeOnLoad;
        [SerializeField] private bool _initializeOnLoadEditor;
        [SerializeField] private bool _isObsolete;

        /// <summary> レコードデータヘッダ </summary>
        [Serializable]
        public struct HeaderData
        {
            public string name;
            public string description;
            public int id;
            public int index;
            public bool obsolete;
        }
        
        [SerializeField]
        protected HeaderData[] _headers;
        public HeaderData[] Headers => _headers;

        public virtual Type RecordType => null;
        public virtual Type IdentifierType => null;
        public virtual string BaseName => string.Empty;
    }

    /// <summary> Represents the base class for data table records. </summary>
    public abstract class DataTableRecordBase<TRecord> :
        DataTableRecordBase 
        where TRecord : struct
    {

        [SerializeField]
        private TRecord[] _records;
        public TRecord[] Records => _records;
        
        private void Reset()
        {
            _headers = new[]
            {
                new HeaderData()
                {
                    id = 0,
                    name = "Invalid",
                    description = string.Empty,
                    index = 0,
                    obsolete = false
                }
            };
            _records = new TRecord[1];
        }        
    }
}