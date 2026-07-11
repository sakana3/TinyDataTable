using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEditor;

namespace TinyDataTable.Editor
{
    internal class EnumInfo
    {
        public string Name { private set; get; }
        public int Value { private set; get; }
        public bool IsObsolate { private set; get; }

        public static List<EnumInfo> FormEnumType(Type type)
        {
            List<EnumInfo> enumInfos = new List<EnumInfo>();
            if (type != null)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    string typePath = field.DeclaringType.FullName; //
                    string ns = field.DeclaringType.Namespace;      //

                    if (!string.IsNullOrEmpty(ns) && typePath.StartsWith(ns + "."))
                    {
                        typePath = typePath.Substring(ns.Length + 1); //
                    }


                    if (!string.IsNullOrEmpty(typePath))
                    {
                        typePath = typePath.Replace('+', ','); //
                    }


                    var fullPathName = $"{typePath}.{field.Name}";
                    
                    var info = new EnumInfo()
                    {
                        Name = field.Name,
                        Value = Convert.ToInt32(field.GetValue(null)),
                        IsObsolate = field.IsDefined(typeof(ObsoleteAttribute)),
                    };

                    enumInfos.Add(info);
                }
            }

            return enumInfos;
        }
        
  
    }
}