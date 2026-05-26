using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TinyDataTable.Editor
{
    public static class DataTablePropertyUtil
    {
        public static List<string> ReservWords = new List<string>()
        {
            "ToString", "GetHashCode", "GetType","Equals",
            "Invalid","IsValid","Prepare","Terminate","EnumToIndex","Enum","Index",
            "ValidEnumList","ValidIDList",
            "_dataTable","_assetPath","_value","_index","_validEnums","_validIDs",
//            "Size"
        };        
    }
}