using System;
using UnityEngine;

namespace TinyDataTable
{
    public abstract class DataTableRecordBase : ScriptableObject
    {
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
        private HeaderData[] _headers;
        private HeaderData[] Headers => _headers;
    }
}