using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace TinyDataTable.Editor
{

    
    
    [Serializable]
    internal class DataTableTree : SerializableTree<DataTableTree.Item>
    {
        [Serializable]
        internal class Item
        {
            [SerializeField]
            public LazyLoadReference<DataTableBase> lazeyTable;
            public DataTableBase tableAsset
            {
                get
                {
                    return lazeyTable.asset;
                }
                set
                {
                    lazeyTable.asset = value;
                }
            }

            private static readonly System.Reflection.FieldInfo InstanceIdField = 
                typeof(LazyLoadReference<DataTableBase>).GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);

            /// <summary>
            /// Chashを汚さないようにinstanceIdからIconを取得する
            /// </summary>
            public Texture Icon
            {
                get
                {
                    if (!lazeyTable.isSet) return null;

                    object fieldValue = InstanceIdField.GetValue(lazeyTable);
                    if (fieldValue == null) return null;

                    int instanceId = (int)fieldValue;
                    if (instanceId == 0) return null;

                    string path = AssetDatabase.GetAssetPath((EntityId)instanceId);
                    if (string.IsNullOrEmpty(path)) return null;

                    System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (assetType == null) return null;

                    GUIContent content = EditorGUIUtility.ObjectContent(null, assetType);
                    return content.image;
                }
            }
        }
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

        public bool CheckDirty(DataTableBase asset)
        {
            var dirdy = SaveDataTable.CheckScriptModified(asset);
            return dirdy;
        }

        public static void OnCreateAsset(DataTableBase asset)
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
                        manager.Tree.Nodes[i].Item.tableAsset = asset;
                        EditorUtility.SetDirty(manager);
                        AssetDatabase.SaveAssetIfDirty(manager);
                        AssetDatabase.Refresh();
                        break;
                    }
                }
            }
        }

    }
}