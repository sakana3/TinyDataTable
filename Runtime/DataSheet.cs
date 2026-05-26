using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDataTable
{
    [Serializable]
    public class DataSheet
    {
        [SerializeReference] public IVariableStruct record;

        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            record = new VariableStruct()
            {
                Header = new RecordHeader()
                {
                    fieldInfos = Array.Empty<RecordFieldInfo>(),
                }
            };
            record.Iniaialize(new RecordDataHeader()
            {
                name = "Invalid",
                id = 0,
                index = 0,
                description = string.Empty
            });            
        }
    }
}