using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace TinyDataTable.Editor
{
    [Serializable]
    internal class DataTableTree : SerializableTree<DataTableRecordBase>
    {
        
    }

    [Icon( "Packages/com.sakana3.tinydatatable//Editor/Assets/TinyDataTableIcon.png")]
    internal class DataTableManager : ScriptableObject
    {
        public enum DataType
        {
            Manual,
            Resources,
        }

        [SerializeField] public DataType dataType;
        [SerializeField] public string RootPath;
        [SerializeField] public string DefaultNamespace;
        [SerializeField] public int RowLimit = 1000;
        [SerializeField] public DataTableTree Tree = new();
        [SerializeField] public string TablesPath;
        [SerializeField] public string ScriptsPath;

        [SerializeField] public string[] Assemblies = new[]
        {
            "Assembly-CSharp", "UnityEngine", "UnityEngine.CoreModule"
        };

        public void Initialize(DataType dataType, string RootPath, string DefaultNamespace)
        {
            this.dataType = dataType;
            this.RootPath = RootPath;
            this.DefaultNamespace = DefaultNamespace;
            if (dataType == DataType.Manual)
            {
                this.TablesPath = $"Assets\\{RootPath}\\Tables";
                this.ScriptsPath = $"Assets\\{RootPath}\\Scripts";
            }
            else
            {
                this.TablesPath = $"Assets\\{RootPath}\\Resources\\TinyDataTables";
                this.ScriptsPath = $"Assets\\{RootPath}\\Scripts";
            }
        }

        public static void MakeDirectory(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);

                // Unity側にフォルダが作成されたことを認識させる
                AssetDatabase.Refresh();
            }
        }

        public bool CheckDirty(DataTableRecordBase asset)
        {
            var dirdy = SaveDataTable.CheckScriptModified(asset);
            return dirdy;
        }

        public static void OnCreateAsset(DataTableRecordBase asset)
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(DataTableManager)}");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var manager = AssetDatabase.LoadAssetAtPath<DataTableManager>(path);
                
                for (int i = 0; i < manager.Tree.Nodes.Length; i++)
                {
                    if (manager.Tree.Nodes[i].Name == asset.BaseName())
                    {
                        manager.Tree.Nodes[i].Item = asset;
                        EditorUtility.SetDirty(manager);
                        AssetDatabase.SaveAssetIfDirty(manager);
                        AssetDatabase.Refresh();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Schema内のリレーション先を検索しRelationに登録する
        /// </summary>
        public static void InjectRelation( DataTableRecordBase target )
        {
            var types = FieldInfo.FieldsFromType<IIdentifier>(target.RecordType())
                .Select(t => t.Type.GetCustomAttribute<IDAttribute>()?.RecordType )
                .Where(t => t != null && t != target.GetType())
                .ToArray();
            
            if ( target.Relations == null || target.Relations.Select(r=>r.GetType()).SequenceEqual(types) is false)
            {
                var newItems = types
                    .SelectMany(t=> AssetDatabase.FindAssets($"t:{t}"))
                    .Select(guid => AssetDatabase.LoadAssetAtPath<DataTableRecordBase>(AssetDatabase.GUIDToAssetPath(guid)))
                    .ToArray();

                var so = new SerializedObject(target);
                var relations = so.FindProperty("_relations");
                relations.arraySize = newItems.Length;
                for (int i = 0; i < relations.arraySize; i++)
                {
                    var relation = relations.GetArrayElementAtIndex(i);
                    relation.objectReferenceValue = newItems[i];                    
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(target);
            }
        }
    }
}