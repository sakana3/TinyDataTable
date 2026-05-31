using System;
using UnityEngine;

namespace TinyDataTable
{
    [Serializable]
    public struct DataTableRecord<TClass,TRecord> : IRecord
        where TClass : struct
        where TRecord : struct
    {
        public Type RecordType => typeof(TRecord);
        public Type ClassType => typeof(TClass);

        [SerializeField] public RecordDataHeader[] headers;
        [SerializeField] public TRecord[] data;

        public RecordDataHeader[] Headers => headers;
        public object Data => data;

        public void Initialize()
        {
            headers = new RecordDataHeader[]
            {
                new ()
                {
                    id = 0,
                    name = "Invalid",
                    description = string.Empty,
                    obsolete = false,
                }
            };
            data = new TRecord[1]
            {
                new TRecord()
            };
        }
    }
}
