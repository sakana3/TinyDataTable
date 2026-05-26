using System;
using System.Linq;
using System.Collections.Generic;

namespace TinyDataTable
{
    public interface IVariableStruct
    {
        public RecordHeader Header { get; set; }
        public Type[] GetFieldTypes();
        public IRecordData GetRecord(int rowIndex);
        public IEnumerable<IRecordData> Records { get; }
        public void Iniaialize(RecordDataHeader newHeader) { }

        public string GetTypeString()
        {
            if (GetFieldTypes().Length == 0) return "VariableStruct";
            return $"VariableStruct<{string.Join(",",GetFieldTypes().Select(t => t.FullName))}>";
        }
    }
    
    public interface IRecordData
    {
        public RecordDataHeader Header {set; get;}        
    }
}