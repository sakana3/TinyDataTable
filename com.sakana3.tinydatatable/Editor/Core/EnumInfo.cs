using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;


namespace TinyDataTable.Editor
{
    internal class EnumInfo
    {
        public string Name { set; get; }
        public int Value { set; get; }
        public bool IsObsolate { set; get; }

        public static List<EnumInfo> FormEnumType(Type type)
        {
            List<EnumInfo> enumInfos = new List<EnumInfo>();
            if (type != null)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
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