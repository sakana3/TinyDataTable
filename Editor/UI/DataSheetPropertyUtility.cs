using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Text.RegularExpressions;

namespace TinyDataTable.Editor
{
    internal static class DataSheetPropertyUtility
    {
        //コラムが変化したかチェック
        public static bool CheckColums(SerializedProperty property,IReadOnlyList<int> columIDs,bool checkObsolete)
        {
            var header = property.FindPropertyRelative("record.header.fieldInfos");
            var headers = Enumerable.Range(0, header.arraySize)
                .Select(i => header.GetArrayElementAtIndex(i));
            
            if (checkObsolete is false)
            {
                if (header.arraySize != columIDs.Count)
                {
                    return false;
                }
            }
            else
            {
                headers = headers
                    .Where(p => p.FindPropertyRelative("obsolete").boolValue is false);
                if (headers.Count() != columIDs.Count)
                {
                    return false;
                }
            }

            var seq = headers
                .OrderBy(p => p.FindPropertyRelative("index").intValue)
                .Select(p => p.FindPropertyRelative("id").intValue);

            if (columIDs.SequenceEqual( seq ) is false)
            {
                return false;
            }
            return true;
        }

        //Rowが変化したかチェック
        public static bool CheckRows(SerializedProperty property,IReadOnlyList<int> rowIDs)
        {
            var recordHeader = property.FindPropertyRelative("record.recordData");
            if (recordHeader.arraySize != rowIDs.Count)
            {
                return false;
            }
            foreach ( var (id,idx) in rowIDs.Select( (name,idx) => (name,idx) ))
            {
                var recordInfo = recordHeader.GetArrayElementAtIndex(idx);
                if (recordInfo.FindPropertyRelative("header.id").intValue != id)
                {
                    return false;
                };
            }
            return true;
        }
        
        public static (string name,int id, string description, bool obsolete) GetColumn(SerializedProperty property, int index)
        {
            var info = property.FindPropertyRelative($"record.header.fieldInfos.Array.data[{index}]");
            var name = info.FindPropertyRelative("name");
            var id = info.FindPropertyRelative("id");
            var description = info.FindPropertyRelative("description");
            var obsolete = info.FindPropertyRelative("obsolete");

            return (name.stringValue, id.intValue, description.stringValue, obsolete.boolValue);
        }

        
        public static SerializedProperty ColumObsolete(SerializedProperty property , int iColum)
        {
            var infos = property.FindPropertyRelative("record.header.fieldInfos");
            var info = infos.GetArrayElementAtIndex(iColum);
            var obsoleteColum = info.FindPropertyRelative("obsolete");

            return obsoleteColum;
        }        
        
        public static SerializedProperty RowObsolete(SerializedProperty property,int iRow)
        {
            var obsoleteRow = property.FindPropertyRelative($"record.recordData.Array.data[{iRow}].header.obsolete");
            return obsoleteRow;
        }

        public static SerializedProperty GetRowNameProperty(SerializedProperty property, int iRow)
        {
            var nameProp = property.FindPropertyRelative($"record.recordData.Array.data[{iRow}].header.name");            
            return nameProp;
        }
        
        /// <summary>
        /// 行の個数を取得
        /// </summary>
        /// <returns></returns>
        public static int GetColumnCount(SerializedProperty property)
        {
            var infos = property.FindPropertyRelative("record.header.fieldInfos");
            return infos == null ? 0 :infos.arraySize;
        }
        
        /// <summary>
        /// 列の個数を取得
        /// </summary>
        /// <returns></returns>
        public static int GetRowCount(SerializedProperty property)
        {
            var infos = property.FindPropertyRelative("record.recordData");
            return infos == null ? 0 : infos.arraySize;
        }

        public static SerializedProperty GetRowArrayProp(SerializedProperty property)
        {
            return property.FindPropertyRelative("record.recordData");
        }
        
        public static SerializedProperty GetCellProperty(SerializedProperty property, int iColum, int iRow)
        {
            var recordHeader = property.FindPropertyRelative("record.recordData");
            var recordInfo = recordHeader.GetArrayElementAtIndex(iRow);
            var cellProp = recordInfo.FindPropertyRelative($"Field{iColum}");
             
            return cellProp;
        }


        public static List<(int id,bool isObsolete)> MakeRowIDList(SerializedProperty property)
        {
            List<(int id,bool isObsolete)> idList = new ();
            var records = property.FindPropertyRelative("record.recordData");
            if (records != null)
            {
                for (int i = 0; i < records.arraySize; i++)
                {
                    var recordInfo = records.GetArrayElementAtIndex(i);
                    var idProp = recordInfo.FindPropertyRelative("header.id");
                    var isObs = recordInfo.FindPropertyRelative("header.obsolete");
                    idList.Add((idProp.intValue,isObs.boolValue));
                }
            }            
            return idList;
        }
        
        public static List<int> MakeColumIDList(SerializedProperty property)
        {
            List<int> idList = new ();
            var infos = property.FindPropertyRelative("record.header.fieldInfos");
            if (infos != null)
            {
                for (int i = 0; i < infos.arraySize; i++)
                {
                    var info = infos.GetArrayElementAtIndex(i);
                    var idProp = info.FindPropertyRelative("id");
                    idList.Add(idProp.intValue);                    
                }
            }            
            return idList;
        }        
        
        public static (List<string> fieldNames, List<string> recordNames ) MakeNameList(
            SerializedProperty property)
        {
            List<string> fieldNames = new ();

            var infos = property.FindPropertyRelative("record.header.fieldInfos");
            if (infos != null)
            {
                for (int i = 0; i < infos.arraySize; i++)
                {
                    var info = infos.GetArrayElementAtIndex(i);
                    var nameProp = info.FindPropertyRelative("name");
                    fieldNames.Add(nameProp.stringValue);
                }
            }

            List<string> recordNames = new ();            
            var records = property.FindPropertyRelative("record.recordData");
            if (records != null)
            {
                for (int i = 0; i < records.arraySize; i++)
                {
                    var recordInfo = records.GetArrayElementAtIndex(i);
                    var nameProp = recordInfo.FindPropertyRelative("header.name");                    
                    recordNames.Add(nameProp.stringValue);                    
                }
            }

            return (fieldNames, recordNames);
        }

        public static List<int> MakeFieldOrderList(SerializedProperty property)
        {
            List<int> fieldOrderList = new ();
            var infos = property.FindPropertyRelative("record.header.fieldInfos");
            if (infos != null)
            {
                for (int i = 0; i < infos.arraySize; i++)
                {
                    var info = infos.GetArrayElementAtIndex(i);
                    var indexProp = info.FindPropertyRelative("index");
                    fieldOrderList.Add(indexProp.intValue);
                }
            }
            return fieldOrderList
                .Select( (index,i) => (index,i) )
                .OrderBy( f => f.index )
                .Select( f => f.i )
                .ToList();
        }

        public static void ChangeFieldOrderList(SerializedProperty property,List<string> newFields )
        {
            var infos = property.FindPropertyRelative("record.header.fieldInfos");
            if (infos != null)
            {
                for (int i = 0; i < infos.arraySize; i++)
                {
                    var info = infos.GetArrayElementAtIndex(i);                    
                    var nameProp = info.FindPropertyRelative("name");
                    
                    var indexProp = info.FindPropertyRelative("index");
                    indexProp.intValue = newFields.IndexOf(nameProp.stringValue);        
                }
            }

            property.serializedObject.ApplyModifiedProperties();            
        }

        public static void AddRow( SerializedProperty property ,int index = -1)
        {
            var recordProp = property.FindPropertyRelative("record.recordData");
            index = index >= 0 ? index : recordProp.arraySize;
            recordProp.InsertArrayElementAtIndex(index);

            var newProp = recordProp.GetArrayElementAtIndex(index);
            
            newProp.FindPropertyRelative("header.id").intValue = MakeNewID(property);
            var nameProp = newProp.FindPropertyRelative("header.name");
            nameProp.stringValue = $"record_{index-1:0000}";
            var indexProp = newProp.FindPropertyRelative("header.index");
            indexProp.intValue = recordProp.arraySize;

            while (CheckName(property, nameProp) == false)
            {
                nameProp.stringValue += "_";
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        public static void RemoveRow( SerializedProperty property, int index = -1)
        {
            var recordProp = property.FindPropertyRelative("record.recordData");
            index = index >= 0 ? index : recordProp.arraySize -1;
            recordProp.DeleteArrayElementAtIndex(index);
            property.serializedObject.ApplyModifiedProperties();            
        }
        
        public static void RemoveRows( SerializedProperty property, IEnumerable<int> indexs )
        {
            var recordProp = property.FindPropertyRelative("record.recordData");
            foreach (var index in indexs.OrderByDescending(i=>i)　)
            {
                recordProp.DeleteArrayElementAtIndex(index);
            }
            property.serializedObject.ApplyModifiedProperties();            
        }

        public static void ResizeRow(SerializedProperty property, uint size)
        {
            var recordProp = property.FindPropertyRelative("record.recordData");
            if (recordProp.arraySize == size)
            {
                return;
            }
            
            while (recordProp.arraySize != size)
            {
                if (recordProp.arraySize < size)
                {
                    var index = recordProp.arraySize;
                    recordProp.InsertArrayElementAtIndex(index);
                    var newProp = recordProp.GetArrayElementAtIndex(index);
            
                    newProp.FindPropertyRelative("header.id").intValue = MakeNewID(property);
                    var nameProp = newProp.FindPropertyRelative("header.name");
                    nameProp.stringValue = $"record_{index-1:0000}";

                    while (CheckName(property, nameProp) == false)
                    {
                        nameProp.stringValue += "_";
                    }                    
                }
                else
                {
                    recordProp.DeleteArrayElementAtIndex(recordProp.arraySize-1);
                }
            }
            property.serializedObject.ApplyModifiedProperties();            
        }

        public static void MoveRow( SerializedProperty property, int from, int to)
        {
            var recordProp = property.FindPropertyRelative("record.recordData");
            recordProp.MoveArrayElement(from, to);
            property.serializedObject.ApplyModifiedProperties();            
        }

        public static void RemoveColum(SerializedProperty property ,int index )
        {
            var sheet = DataSheetPropertyUtility.GetValue(property) as DataSheet;
            if (sheet != null)
            {
                sheet.RemoveField( index );
                property.serializedObject.Update();
//                property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        /// <summary>
        /// SerializedPropertyが参照している実際のインスタンスを取得する
        /// </summary>
        public static object GetValue(this SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data[", "["); // 配列パスを正規化
            string[] elements = path.Split('.');

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    // 配列/リスト要素の場合
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    
                    obj = GetFieldValue(obj, elementName);
                    obj = GetElementAtIndex(obj, index);
                }
                else
                {
                    // 通常フィールドの場合
                    obj = GetFieldValue(obj, element);
                }
            }
            
            return obj;
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(source);
                
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null) return p.GetValue(source, null);
                
                type = type.BaseType;
            }
            return null;
        }

        private static object GetElementAtIndex(object collection, int index)
        {
            if (collection is IList list)
            {
                return list[index];
            }
            return null;
        }
        
        /// <summary>
        /// 新規IDを作る
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static int MakeNewID(SerializedProperty property)
        {
            var rowProp = property.FindPropertyRelative("record.header.fieldInfos");
            var rowEnumrator = Enumerable.Range(0, rowProp.arraySize)
                    .Select(i => rowProp.GetArrayElementAtIndex(i))
                    .Select(prop => prop.FindPropertyRelative("id").intValue);

            var columns = property.FindPropertyRelative("record.recordData");
            var colEnumtaror = Enumerable
                .Range(0, columns.arraySize)
                .Select(i => columns.GetArrayElementAtIndex(i).FindPropertyRelative("header.id").intValue);

            var idCandidates = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
            while (colEnumtaror.Concat(rowEnumrator).Any(t => t == idCandidates))
            {
                idCandidates = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
            }    

            return idCandidates;
        }
        
        public static bool CheckName(SerializedProperty property,SerializedProperty nameProp)
        {
            var columns = property.FindPropertyRelative("record.header.fieldInfos");
            var propNames = Enumerable.Range(0, columns.arraySize)
                .Select(i => columns.GetArrayElementAtIndex(i))
                .Select(col => col.FindPropertyRelative("name"))
                .Where( t => !SerializedProperty.EqualContents(t, nameProp) )
                .Select(prop => prop.stringValue);

            if (propNames.Any(t => t == nameProp.stringValue ))
            {
                return false;
            }

            var rows = property
                .FindPropertyRelative("record.recordData");
            var idNames = Enumerable.Range(0, rows.arraySize)
                .Select(i => rows.GetArrayElementAtIndex(i))
                .Select(row => row.FindPropertyRelative("header.name"))
                .Where( t => !SerializedProperty.EqualContents(t, nameProp) )
                .Select(prop => prop.stringValue);

            if (idNames.Any(t => t == nameProp.stringValue ))
            {
                return false;
            }
            
            return true;
        }
        
        // C#の予約語リスト
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        };        
        
        public static bool CheckCSharpSafeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
        
            // 先頭: 文字 (Unicode Letter) またはアンダースコア
            // 2文字目以降: 文字、数字、またはアンダースコア
            // (日本語などの文字も許容する正規表現パターンです)
            if (!Regex.IsMatch(name, @"^[\p{L}_][\p{L}\p{N}_]*$"))
            {
                return false;
            }

            // 予約語と完全一致しないかをチェック
            return !CSharpKeywords.Contains(name);
        }      
        
        public static bool CheckExistClass(string namespaceName, string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Any(t => t.Name == className && t.Namespace == namespaceName);
        }        
    }
}