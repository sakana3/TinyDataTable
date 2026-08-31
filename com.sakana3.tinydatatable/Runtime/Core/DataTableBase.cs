/// Table of
/// Identifier
/// Notional
/// Yield


using System;
using System.Collections.Generic;
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
//            Embedded = 0x0001 << 1,
            Obsolete = 0x0001 << 4,
            IncludeAssetPath = 0x0001 << 8 ,
            IncludeGUID = 0x0001 << 9 ,
            
//            InitializeOnLoad = 0x0001 << 12,
//            InitializeOnLoadEditor = 0x0001 << 13,
//            EditorOnly = 0x0001 << 16,
            IncludeEditorPath = 0x0001 << 17 ,
            InitializeOnLoadEditor = 0x0001 << 18,
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

        /// <summary> Get Record form Index </summary>        
        public TSchema this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Records[index]; }
        }

        /// <summary> Get Record form Name </summary>
        public TSchema this[string name]  => Records[ToIndex(name)];

        /// <summary> Get Schema form Name </summary>        
        private static WeakReference<DataTableBase<TSchema>> _instanceRef = new WeakReference<DataTableBase<TSchema>>(null);

        /// <summary> Singleton Instance </summary>
        public static DataTableBase<TSchema> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if( _instanceRef.TryGetTarget( out var target ) )
                {
                    Debug.Assert(target != null , $"The resource hasn't been loaded.");
                    return target;
                }
                return null;
            }
        }

        /// <summary> Singleton Instance Records </summary>        
        public static TSchema[] InstanceRecords
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if( _instanceRef.TryGetTarget( out var target ) )
                {
                    Debug.Assert(target != null , $"The resource hasn't been loaded.");
                    return target._records;
                }
                return null;
            }
        }

        /// <summary> Singleton Instance Headers </summary>
        public static bool isSet => _instanceRef.TryGetTarget( out var _ );
        
        ~DataTableBase()
        {
            Debug.Log($">>>{this.GetType().Name}.~DataTableBase.");
        }
        
        /// <summary> OnEnable </summary>        
        private void OnEnable()
        {
            Debug.Log($">>>{this.GetType().Name}.OnEnable.");
            // NOTE : メモ
            // AddressablesからUnloadされた時にはOnDisable/OnDestroyは呼ばれない
            // TryGetTargetがfalseになるのでそれで判断
            // Debug.Assert( Instance is null, "Can't create multiple instances.");
            _instanceRef.SetTarget(this);
        }

        /// <summary> OnDisable </summary>        
        private void OnDisable()
        {
            Debug.Log($">>>{this.GetType().Name}.OnDisable.");
            _instanceRef.SetTarget(null);
        }

        
        
#if  UNITY_EDITOR
        /// <summary> Reset </summary>                
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
#endif        
    }
}