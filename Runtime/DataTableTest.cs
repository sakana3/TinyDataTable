using System;
using TinyDataTable;
using UnityEngine;

namespace TinyDataTable.Record
{
    public partial class DataTableTest : DataTableRecordBase
    {
        [Serializable]
        public struct Record
        {
            public float x;
            public float y;
            public float z;
        }

        [SerializeField]
        private Record[] _records;
        public Record[] Records => _records;

        public static readonly Type IdentifierType = typeof(ID.DataTableTest);
    }
}

namespace ID
{
    public struct DataTableTest : IIdentifier
    {
        public static readonly Type RecordType = typeof(TinyDataTable.Record.DataTableTest.Record);
        
        [Serializable]
        public enum Enum
        {
            [EnumOrder(0)] Invalid     = 0x00000000,
            [EnumOrder(1)] record_0000 = 0x27F8912E,
            [EnumOrder(2)] record_0001 = 0x3DCC255B,
            [EnumOrder(3)] record_0002 = 0x526B1638,
            [EnumOrder(4)] record_0003 = 0x40E462D6,
            [EnumOrder(5)] record_0004 = 0x7EA92678,
            [EnumOrder(6)] record_0005 = 0x09C1C926,
            [EnumOrder(7)] record_0006 = 0x7A68ED17,
            [EnumOrder(8)] record_0007 = 0x6D98D04A,
            [EnumOrder(9)] record_0008 = 0x0B1BB0D7,
        }        
        
        public bool IsValid => false;
    }
}