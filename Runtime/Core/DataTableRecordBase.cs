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
        /// <summary> initializeOnLoad </summary>
        public bool InitializeOnLoadEditor => _initializeOnLoadEditor;
        
        [SerializeField] private bool _initializeOnLoadEditor;
        /// <summary> initializeOnLoadEditor </summary>
        public bool InitializeOnLoad => _initializeOnLoad;

        [SerializeField] private bool _isObsolete;
        /// <summary> isObsolete </summary>
        public bool IsObsolete => _isObsolete;

        /// <summary> header struct </summary>
        [Serializable]
        public struct HeaderData
        {
            public string name;
            public int id;
            public string description;
            public bool obsolete;
        }
        
        [SerializeField] protected HeaderData[] _headers;
        /// <summary> Header data </summary>
        public HeaderData[] Headers => _headers;

        [SerializeField] protected DataTableRecordBase[] _relations;
        /// <summary> Relations </summary>
        public DataTableRecordBase[] Relations => _relations;

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
        /// <summary> Records </summary>        
        [SerializeField] private TSchema[] _records;
        public TSchema[] Records
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _records; }
        }

        /// <summary> Enum To Index </summary>        
        public int ToIndex(TEnum enumValue)
        {
            var index =  Array.FindIndex(Headers, h => h.id == Unsafe.As<TEnum, int>(ref enumValue));
            return  index >= 0 ? index : 0;
        }

        /// <summary> Name To Index </summary>        
        public int ToIndex(string name)
        {
            var index = Array.FindIndex(Headers, h => h.name == name);
            return  index >= 0 ? index : 0;
        }

        /// <summary> Get Schema form Index </summary>        
        public TSchema this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Records[index]; }
        }

        /// <summary> Get Schema form Name </summary>        
        public TSchema this[string name]  => Records[ToIndex(name)];
        
        /// <summary> Get Schema form Enum </summary>        
        public TSchema this[TEnum enumValue] => Records[ToIndex(enumValue)];
        
        protected static DataTableRecordBase<TSchema,TEnum> _instance;
        /// <summary> Singleton Instance </summary>        
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