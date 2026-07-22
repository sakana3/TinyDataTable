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
    [Icon("Packages/com.sakana3.tinydatatable//Editor/Assets/TinyDataTableIcon.png")]
    public abstract class DataTableBase : ScriptableObject
    {
#if UNITY_EDITOR        
        [Flags]
        public enum Flags
        {
            IncludeAssetPath = 0x0001 ,
            Embedded = 0x0001 << 1,
            Obsolete = 0x0001 << 2,
            InitializeOnLoad = 0x0001 << 3,
            InitializeOnLoadEditor = 0x0001 << 4,
            EditorOnly = 0x0001 << 5,
        }
        

        [SerializeField] public Flags EditorFlags = Flags.IncludeAssetPath;
#endif
        /// <summary> header struct </summary>
        [Serializable]
        public struct HeaderData
        {
            public string name;
            public int id;
#if UNITY_EDITOR
            public string description;
            public bool obsolete;
#endif
        }

        [SerializeField] protected HeaderData[] _headers;
        /// <summary> Header data </summary>
        public HeaderData[] Headers => _headers;

        [SerializeField] protected DataTableBase[] _relations;
        /// <summary> Relations </summary>
        public DataTableBase[] Relations => _relations;
    }

    /// <summary> Represents the base class for data table records. </summary>
    public abstract class DataTableBase<TSchema> :
        DataTableBase
        where TSchema : struct
    {
        /// <summary> Records </summary>        
        [SerializeField] private TSchema[] _records;
        public TSchema[] Records
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _records; }
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
        
        protected static DataTableBase<TSchema> _instance;
        /// <summary> Singleton Instance </summary>        
        public static DataTableBase<TSchema> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance;
        }

        protected virtual void OnEnable()
        {
//            if (_instance == null)
            {
                _instance = this;
            }
        }
        
        protected virtual void OnDisable()
        {
//            if (_instance == this)
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