/// Table of
/// Identifier
/// Notional
/// Yield


using System;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace TinyDataTable
{
    /// <summary> データテーブルレコードの基底クラス </summary>
    public abstract class DataTableRecordBase : ScriptableObject
    {
        [SerializeField] private bool _initializeOnLoad;
        public bool InitializeOnLoadEditor => _initializeOnLoadEditor;
        
        [SerializeField] private bool _initializeOnLoadEditor;
        public bool InitializeOnLoad => _initializeOnLoad;

        [SerializeField] private bool _isObsolete;
        public bool IsObsolete => _isObsolete;

        /// <summary> レコードデータヘッダ </summary>
        [Serializable]
        public struct HeaderData
        {
            public string name;
            public string description;
            public int id;
            public bool obsolete;
        }
        
        [SerializeField]
        protected HeaderData[] _headers;
        public HeaderData[] Headers => _headers;

#if UNITY_EDITOR        
        public Type RecordType => this.GetType().GetCustomAttribute<RecordAttribute>().RecordType;
        public Type IdentifierType => this.GetType().GetCustomAttribute<RecordAttribute>().IdentifierType;
        public string BaseName => this.GetType().GetCustomAttribute<RecordAttribute>().BaseName;
#endif
    }

    /// <summary> Represents the base class for data table records. </summary>
    public abstract class DataTableRecordBase<TRecord> :
        DataTableRecordBase 
        where TRecord : struct
    {
        [SerializeField]
        private TRecord[] _records;
        public TRecord[] Records => _records;

        public TRecord this[int index] => Records[index];

        public TRecord this[string key]
        {
            get
            {
                var idx = Array.FindIndex(Headers, h => h.name == key);
                return idx >= 0 ? Records[idx] : default;
            }
        }
        
        protected static DataTableRecordBase<TRecord> _instance;
        public static DataTableRecordBase<TRecord> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance;
        }

        protected virtual void OnEnable()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }
        protected virtual void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        private void Reset()
        {
            _headers = new[]
            {
                new HeaderData()
                {
                    id = 0,
                    name = "Invalid",
                    description = string.Empty,
                    obsolete = false
                }
            };
            _records = new TRecord[1];
        }
    }
}