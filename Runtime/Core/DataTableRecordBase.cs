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
            public int id;
            public string description;
            public bool obsolete;
        }
        
        [SerializeField]
        protected HeaderData[] _headers;
        public HeaderData[] Headers => _headers;

#if UNITY_EDITOR
        public Type RecordType => this.GetType().GetCustomAttribute<RecordAttribute>().SchemaType;
        public Type IdentifierType => this.GetType().GetCustomAttribute<RecordAttribute>().IdentifierType;
        public string BaseName => this.GetType().GetCustomAttribute<RecordAttribute>().BaseName;
#endif
    }

    /// <summary> Represents the base class for data table records. </summary>
    public abstract class DataTableRecordBase<TSchema, TEnum> :
        DataTableRecordBase
        where TSchema : struct
        where TEnum : Enum
    {
        [SerializeField] private TSchema[] _records;
        public TSchema[] Records
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _records; }
        }

        public int ToIndex(TEnum enumValue)
        {
            var index =  Array.FindIndex(Headers, h => h.id == Unsafe.As<TEnum, int>(ref enumValue));
            return  index >= 0 ? index : 0;
        }

        public int ToIndex(string key)
        {
            var index = Array.FindIndex(Headers, h => h.name == key);
            return  index >= 0 ? index : 0;
        }

        public TSchema this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Records[index]; }
        }

        public TSchema this[string key]  => Records[ToIndex(key)];
        
        public TSchema this[TEnum enumValue] => Records[ToIndex(enumValue)];
        
        protected static DataTableRecordBase<TSchema,TEnum> _instance;
        public static DataTableRecordBase<TSchema,TEnum> Instance
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
            _records = new TSchema[1];
        }
    }
}