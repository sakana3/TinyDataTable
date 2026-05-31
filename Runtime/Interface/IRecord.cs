using System;

namespace TinyDataTable
{
    public interface IRecord
    {
        void Initialize();

        Type RecordType { get; }

        Type ClassType { get; }
        
        RecordDataHeader[] Headers { get; }

        object Data { get; }
    }
}