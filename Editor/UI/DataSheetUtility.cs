using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDataTable.Editor
{
    internal static class DataSheetUtility
    {
        private static IVariableStruct ChangeRecordFieldType( this DataSheet sheet,IVariableStruct valiableStruct , params Type[] typeArgument )
        {
            var json = UnityEditor.EditorJsonUtility.ToJson(valiableStruct);
            valiableStruct = VariableStructBuilder.Instantiate(typeArgument);
            UnityEditor.EditorJsonUtility.FromJsonOverwrite(json, valiableStruct);
            return valiableStruct;
        }
        
        public static void AddField( this DataSheet sheet,Type type , string fieldName ,bool isArray = false )
        {
            var header = sheet.record.Header;
            var newField = new RecordFieldInfo()
            {
                name = fieldName,
                id = sheet.MakeNewID(),
                index = header.fieldInfos.Length
            };
            header.fieldInfos = header.fieldInfos.Append(newField).ToArray();
            sheet.record.Header = header;

            if (isArray)
            {
                type = type.MakeArrayType();
            }
            
            var newFiledTypes = sheet.record.GetFieldTypes().Append(type).ToArray();
            sheet.record = sheet.ChangeRecordFieldType(sheet.record, newFiledTypes);
        }

        public static bool RemoveField(this DataSheet sheet,int index)
        {
             if (index >= 0)
             {
                 var tmpHeader = sheet.record.Header;
                 tmpHeader.fieldInfos = tmpHeader.fieldInfos
                     .Where((t, i) => i != index)
                     .ToArray();
                 sheet.record.Header = tmpHeader;
                 var oldTypes = sheet.record.GetFieldTypes();
                 var newTypes = oldTypes
                     .Where((_, i) => i != index )
                     .Where((_, i) => i < tmpHeader.fieldInfos.Length)
                     .ToArray();
                 
                 var json = UnityEditor.EditorJsonUtility.ToJson(sheet.record,true);
                 //Jsonの中身を新しいフィールドに書き換える
                 //TODO : 単なる置き換えなので後でちゃんとJSONをパースするようにする。
                 for (int i = index; i < oldTypes.Length; i++)
                 {
                     json = json.Replace($"\"Field{i}\":", $"\"___Field{i}\":");
                 }
                 for ( int i = index ; i < oldTypes.Length ; i++)
                 {
                     var t = i -1 >= 0 ? $"Field{i -1}" : "FieldX";
                     json = json.Replace($"\"___Field{i}\":", $"\"{t}\":");
                 }
                 sheet.record = VariableStructBuilder.Instantiate(newTypes);
                 UnityEditor.EditorJsonUtility.FromJsonOverwrite(json, sheet.record);                 

                 return true;
             }
             return false;
        }

        /// <summary>
        /// ヘッダーと定義に差があった場合いい感じにそろえる
        /// </summary>
        public static void FitField(this DataSheet sheet)
        {
            var filedCount = sheet.record.Header.fieldInfos.Length;
            if (filedCount < sheet.record.GetFieldTypes().Length)
            {
                var newTypes = sheet.record.GetFieldTypes()
                    .Where((_, i) => i < filedCount)
                    .ToArray();
                sheet.record = VariableStructBuilder.Instantiate(newTypes);
            }
        }

        /// <summary>
        /// 新規idを作成
        /// </summary>
        public static int MakeNewID(this DataSheet sheet)
        {
            var idCandidates = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
            while ( sheet.record.Header.fieldInfos.Any(t => t.id == idCandidates) ||
                    sheet.record.Records.Any( t => t.Header.id == idCandidates )
                  )
            {
                idCandidates = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
            }

            return idCandidates;
        }
    }
}