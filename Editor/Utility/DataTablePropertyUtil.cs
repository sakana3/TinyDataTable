using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TinyDataTable.Editor
{
    public class RecordProperty
    {
        public DataTableAsset TargeTableAsset { get; private set; }
        private SerializedProperty _recordProperty;

        public SerializedProperty Property => _recordProperty;

        public SerializedProperty HeaderProperty => _recordProperty.FindPropertyRelative("headers");
        public SerializedProperty DataProperty => _recordProperty.FindPropertyRelative("data");
        
        public SerializedObject SerializedObject => _recordProperty.serializedObject;
        
        public List<RecordDataHeader> RowHeaders { private set; get; }
        public List<RecordFieldInfo> FieldInfos { private set; get; }

        public RecordProperty( DataTableAsset targeTableAsset )
        {
            this.TargeTableAsset = targeTableAsset;
            this._recordProperty = new SerializedObject(TargeTableAsset)
                .FindProperty(DataTableAsset.nameOfRecord);
                
            ReloadInfo();
        }
        
        public bool IsChanged => false;

        public void ReloadInfo()
        {
            FieldInfos = DataTableRecordUtility.GetSerializableFields(TargeTableAsset.RecordType);
            
            RowHeaders = GetRowProperties()
                .Select(p => new RecordDataHeader()
                {
                    name = p.FindPropertyRelative( nameof(RecordDataHeader.name)).stringValue ,
                    id = p.FindPropertyRelative( nameof(RecordDataHeader.id)).intValue,
                    index = p.FindPropertyRelative( nameof(RecordDataHeader.index)).intValue,
                    description = p.FindPropertyRelative( nameof(RecordDataHeader.description)).stringValue,
                    obsolete = p.FindPropertyRelative( nameof(RecordDataHeader.obsolete)).boolValue,
                } )
                .ToList();
        }


        
        /// <summary>
        /// 新規IDを作る
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private int MakeNewID()
        {
            var rowProp = HeaderProperty;

            var ids = Enumerable.Range(0, rowProp.arraySize)
                .Select(i => rowProp.GetArrayElementAtIndex(i))
                .Select(p => p.FindPropertyRelative(nameof(RecordDataHeader.id)).intValue)
                .ToArray();
            
            var idCandidates = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
            while (ids.Contains(idCandidates))
            {
                idCandidates = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
            }
            return idCandidates;
        }        

        /// <summary> Add Row </summary>
        public void AddRow( int index = -1)
        {
            var dataProp = DataProperty;
            var headerProp = HeaderProperty;
            
            var idx = index >= 0 ? index : headerProp.   arraySize;

            dataProp.InsertArrayElementAtIndex(idx);
            headerProp.InsertArrayElementAtIndex(idx);

            var newHeaderProp = headerProp.GetArrayElementAtIndex(idx);
            
            newHeaderProp.FindPropertyRelative(nameof(RecordDataHeader.id)).intValue = MakeNewID();
            newHeaderProp.FindPropertyRelative(nameof(RecordDataHeader.name)).stringValue = $"record_{idx - 1:0000}";
            newHeaderProp.FindPropertyRelative(nameof(RecordDataHeader.index)).intValue = headerProp.arraySize;

            _recordProperty.serializedObject.ApplyModifiedProperties();
        }        
        

        public void RemoveRow( int index = -1)
        {
            var dataProp = DataProperty;
            var headerProp = HeaderProperty;
            
            var idx = index >= 0 ? index : headerProp.arraySize;
            
            dataProp.DeleteArrayElementAtIndex(index);
            headerProp.DeleteArrayElementAtIndex(index);
            _recordProperty.serializedObject.ApplyModifiedProperties();
        }        

        public void RemoveRows( IEnumerable<int> indexs )
        {
            var dataProp = DataProperty;
            var headerProp = HeaderProperty;                
            foreach (var index in indexs.OrderByDescending(i => i))
            {
                dataProp.DeleteArrayElementAtIndex(index);
                headerProp.DeleteArrayElementAtIndex(index);
            }
            _recordProperty.serializedObject.ApplyModifiedProperties();                
        }
        

        public void ResizeRow(uint size)
        {
            var dataProp = DataProperty;
            var headerProp = HeaderProperty;                      
            if (headerProp.arraySize == size && dataProp.arraySize == size)
            {
                return;
            }

            while (headerProp.arraySize != size)
            {
                if (headerProp.arraySize < size)
                {
                    var index = headerProp.arraySize;                        
                    headerProp.InsertArrayElementAtIndex(index);
                    var newHeaderProp = headerProp.GetArrayElementAtIndex(index);

                    newHeaderProp.FindPropertyRelative(nameof(RecordDataHeader.id)).intValue = MakeNewID();
                    newHeaderProp.FindPropertyRelative(nameof(RecordDataHeader.name)).stringValue = $"record_{index - 1:0000}";
                    newHeaderProp.FindPropertyRelative(nameof(RecordDataHeader.index)).intValue = headerProp.arraySize;

                    //TODO: 名前のチェックちゃんとやる
/*
                    while (CheckName(property, nameProp) == false)
                    {
                        nameProp.stringValue += "_";
                    }
*/                   
                }
                else
                {
                    headerProp.DeleteArrayElementAtIndex(headerProp.arraySize - 1);                        
                }
            }
            _recordProperty.serializedObject.ApplyModifiedProperties();                
        }
        
        public void MoveRow( int from, int to)
        {
            DataProperty.MoveArrayElement(from, to);
            HeaderProperty.MoveArrayElement(from, to);
            SerializedObject.ApplyModifiedProperties();            
        }

        public IEnumerable<SerializedProperty> GetRowProperties()
        {
            var headerProp = HeaderProperty;
            for (int i = 0; i < headerProp.arraySize; i++)
            {
                yield return headerProp.GetArrayElementAtIndex(i);
            }
        }
        
        
        /// <summary> GetCellProperties </summary>
        public IEnumerable<SerializedProperty> GetCellProperties( int iRow = 0 )
        {
            if (DataProperty == null )
            {
                Debug.LogError("DataProperty is null");
                yield break;
            }
            
            var property = DataProperty.GetArrayElementAtIndex(iRow);

            // 探索の終了地点（このプロパティの次の兄弟要素）を取得
            SerializedProperty endProperty = property.GetEndProperty();
        
            // 複製を作らないと、元のプロパティのポインタが動いてしまうためCopyを使用
            SerializedProperty iterator = property.Copy();

            // 最初の子要素に移動 (enterChildren: true)
            if (iterator.NextVisible(true))
            {
                while (true)
                {
                    // 終了地点に達するか、別のプロパティの領域に入ったらブレイク
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;

                    yield return iterator;

                    // 次の要素へ移動 (ここでは親に戻らないよう enterChildren: false にするのが一般的)
                    if (!iterator.NextVisible(false))
                        break;
                }
            }
        }
        
        public void SetRowObsolete(int iRow, bool isObsolete)
        {
            var headerProp = HeaderProperty;
            var header = headerProp.GetArrayElementAtIndex(iRow);
            header.FindPropertyRelative(nameof(RecordDataHeader.obsolete)).boolValue = isObsolete;
            var t = RowHeaders[iRow];
            t.obsolete = isObsolete;
            RowHeaders[iRow] = t;
            _recordProperty.serializedObject.ApplyModifiedProperties();           
        }
        
        public List<(int id,string name,bool isObsolete)> MakeRecordHeaderList()
        {
            List<(int id,string name,bool isObsolete)> idList = new ();
            var headers = HeaderProperty;
            if (headers != null)
            {
                for (int i = 0; i < headers.arraySize; i++)
                {
                    var header = headers.GetArrayElementAtIndex(i);
                    var idProp = header.FindPropertyRelative( nameof(RecordDataHeader.id));
                    var nameProp = header.FindPropertyRelative(nameof(RecordDataHeader.name));
                    var isObs = header.FindPropertyRelative(nameof(RecordDataHeader.obsolete));
                    idList.Add((idProp.intValue,nameProp.stringValue,isObs.boolValue));
                }
            }            
            return idList;
        }
        
        public SerializedProperty GetRecordNameProperty(int iRow)
        {
            var nameProp = HeaderProperty
                .GetArrayElementAtIndex(iRow)
                .FindPropertyRelative(nameof(RecordDataHeader.name));
            return nameProp;
        }        
    }
    
    
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