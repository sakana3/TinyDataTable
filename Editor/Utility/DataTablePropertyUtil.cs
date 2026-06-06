using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TinyDataTable.Editor
{
    public class RecordPropertyUtil
    {
        public DataTableRecordBase TargeTableAsset { get; private set; }

        public SerializedProperty HeaderProperty => _serializedObject.FindProperty("_headers");
        public SerializedProperty RecordProperty => _serializedObject.FindProperty("_records");
        
        private SerializedObject _serializedObject;
        public SerializedObject SerializedObject => _serializedObject;
        
        public List<DataTableRecordBase.HeaderData> RowHeaders { private set; get; }
        public List<RecordFieldInfo> FieldInfos { private set; get; }

        public RecordPropertyUtil( DataTableRecordBase targeTableAsset )
        {
            this.TargeTableAsset = targeTableAsset;
            _serializedObject = new SerializedObject(TargeTableAsset);
                
            ReloadInfo();
        }
        
        public bool IsChanged => false;

        public void ReloadInfo()
        {
            FieldInfos = RecordFieldInfo.FieldsFromType(TargeTableAsset.RecordType);
            
            RowHeaders = GetRowProperties()
                .Select(p => new DataTableRecordBase.HeaderData()
                {
                    name = p.FindPropertyRelative( nameof(DataTableRecordBase.HeaderData.name)).stringValue ,
                    id = p.FindPropertyRelative( nameof(DataTableRecordBase.HeaderData.id)).intValue,
                    description = p.FindPropertyRelative( nameof(DataTableRecordBase.HeaderData.description)).stringValue,
                    obsolete = p.FindPropertyRelative( nameof(DataTableRecordBase.HeaderData.obsolete)).boolValue,
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
                .Select(p => p.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.id)).intValue)
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
            var dataProp = RecordProperty;
            var headerProp = HeaderProperty;
            
            var idx = index >= 0 ? index : headerProp.arraySize;

            dataProp.InsertArrayElementAtIndex(idx);
            headerProp.InsertArrayElementAtIndex(idx);

            var tmpName = $"record_{idx:0000}";
            while (CheckName(tmpName) == false)
            {
                tmpName += "_";
            }
            
            var newHeaderProp = headerProp.GetArrayElementAtIndex(idx);
            newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.id)).intValue = MakeNewID();
            newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.name)).stringValue = tmpName;
            newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.obsolete)).boolValue = false;

            _serializedObject.ApplyModifiedProperties();

            ReloadInfo();
        }        
        

        public void RemoveRow( int index = -1)
        {
            var dataProp = RecordProperty;
            var headerProp = HeaderProperty;
            
            var idx = index >= 0 ? index : headerProp.arraySize;
            
            dataProp.DeleteArrayElementAtIndex(index);
            headerProp.DeleteArrayElementAtIndex(index);
            _serializedObject.ApplyModifiedProperties();
            
            ReloadInfo();
        }        

        public void RemoveRows( IEnumerable<int> indexs )
        {
            var dataProp = RecordProperty;
            var headerProp = HeaderProperty;                
            foreach (var index in indexs.OrderByDescending(i => i))
            {
                dataProp.DeleteArrayElementAtIndex(index);
                headerProp.DeleteArrayElementAtIndex(index);
            }
            _serializedObject.ApplyModifiedProperties();
            
            ReloadInfo();
        }
        

        public void ResizeRow(int size)
        {
            var recordProp = RecordProperty;
            var headerProp = HeaderProperty;                      
            if (headerProp.arraySize == size && recordProp.arraySize == size)
            {
                return;
            }

            while (headerProp.arraySize != size )
            {
                if (headerProp.arraySize < size)
                {
                    var index = headerProp.arraySize;                        
                    headerProp.InsertArrayElementAtIndex(index);
                    var newHeaderProp = headerProp.GetArrayElementAtIndex(index);

                    var tmpName = $"record_{index - 1:0000}";

                    while (CheckName(tmpName) == false)
                    {
                        tmpName += "_";
                    }
                    newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.id)).intValue = MakeNewID();
                    newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.name)).stringValue = tmpName;
                    newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.obsolete)).boolValue = false;
                    newHeaderProp.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.description)).stringValue = "";
                }
                else
                {
                    headerProp.DeleteArrayElementAtIndex(headerProp.arraySize - 1);                        
                }
            }

            while (recordProp.arraySize != size)
            {
                if (recordProp.arraySize < size)
                {
                    var index = recordProp.arraySize;                        
                    recordProp.InsertArrayElementAtIndex(index);
                    recordProp.GetArrayElementAtIndex(index);                    
                }
                else
                {
                    recordProp.DeleteArrayElementAtIndex(recordProp.arraySize - 1);                             
                }
            }

            _serializedObject.ApplyModifiedProperties();         
            ReloadInfo();
        }
        
        public void MoveRow( int from, int to)
        {
            RecordProperty.MoveArrayElement(from, to);
            HeaderProperty.MoveArrayElement(from, to);
            SerializedObject.ApplyModifiedProperties();        
            ReloadInfo();
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
            if (RecordProperty == null )
            {
                Debug.LogError("DataProperty is null");
                yield break;
            }
            
            var property = RecordProperty.GetArrayElementAtIndex(iRow);

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
            header.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.obsolete)).boolValue = isObsolete;
            var t = RowHeaders[iRow];
            t.obsolete = isObsolete;
            RowHeaders[iRow] = t;
            _serializedObject.ApplyModifiedProperties();
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
                    var idProp = header.FindPropertyRelative( nameof(DataTableRecordBase.HeaderData.id));
                    var nameProp = header.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.name));
                    var isObs = header.FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.obsolete));
                    idList.Add((idProp.intValue,nameProp.stringValue,isObs.boolValue));
                }
            }
            return idList;
        }
        
        public SerializedProperty GetRecordNameProperty(int iRow)
        {
            var nameProp = HeaderProperty
                .GetArrayElementAtIndex(iRow)
                .FindPropertyRelative(nameof(DataTableRecordBase.HeaderData.name));
            return nameProp;
        }        

        public bool CheckName(string name)
        {
            //クラスと同じ名前は付けられない
            if (TargeTableAsset.BaseName == name)
            {
                return false;
            }

            if (TargeTableAsset.Headers.Any(f => f.name == name))
            {
                return false;
            }

            if (FieldInfos.Any(f => f.Type.Name == name))
            {
                return false;
            }

            if (ReservWords.Contains(name))
            {
                return false;
            }
            
            return true;
        }

        public static List<string> ReservWords = new List<string>()
        {
            "ToString", "GetHashCode", "GetType","Equals",
            "Invalid","IsValid","Terminate","EnumToIndex","Index",
            "ValidEnumList","ValidIDList",
            "_dataTable","_assetPath","_value","_index","_validEnums","_validIDs",
//            "Size"
        };
    }
}