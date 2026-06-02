using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace TinyDataTable.Editor
{
    [Serializable]
    public class DataTableTree : SerializableTree<DataTableRecordBase>
    {
        
    }

    public class DataTableManager : ScriptableObject
    {
        public enum DataType
        {
            Resources,
            Addresable
        }

        [SerializeField] public DataType dataType;
        [SerializeField] public string RootPath;
        [SerializeField] public string DefaultNamespace;
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
            this.TablesPath = $"Assets\\{RootPath}\\Tables";
            this.ScriptsPath = $"Assets\\{RootPath}\\Scripts";
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
                    if (manager.Tree.Nodes[i].Name == asset.BaseName)
                    {
                        manager.Tree.Nodes[i].Item = asset;
                        EditorUtility.SetDirty(manager);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        break;
                    }
                }
            }
        }
    }
}