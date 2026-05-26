using System;
using UnityEngine;

namespace TinyDataTable
{
    public interface IRecord
    {
        Type RecordType
        {
            get;
        }
        Type ClassType
        {
            get;
        }
    }

    [Serializable]
    public struct DataTableRecord<TClass,TRecord> : IRecord
        where TClass : struct
        where TRecord : struct
    {
        public Type RecordType => typeof(TRecord);
        public Type ClassType => typeof(TClass);

        [SerializeField] TRecord[] table;
    }
}
