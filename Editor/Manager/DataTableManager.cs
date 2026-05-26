using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace TinyDataTable.Editor
{
    [Serializable]
    public class DataTableTree : SerializableTree<DataTableAsset>
    {
        
    }
    
    public class DataTableManager : ScriptableObject
    {
        public enum DataType
        {
            Resources,
            Addresable
        }
        
        [SerializeField]
        public DataType dataType;
        [SerializeField]
        public string RootPath;
        [SerializeField]
        public string DefaultNamespace;
        [SerializeField]
        public DataTableTree Tree = new ();
        [SerializeField]
        public string TablesPath;
        [SerializeField]
        public string ScriptsPath;
        [SerializeField] public string[] Assemblies = new []
        {
            "Assembly-CSharp", "UnityEngine", "UnityEngine.CoreModule"
        };

        public void Initialize(DataType dataType,string RootPath,string DefaultNamespace)
        {
            this.dataType = dataType;
            this.RootPath = RootPath;
            this.DefaultNamespace = DefaultNamespace;
            this.TablesPath = $"Assets\\{RootPath}\\Tables";
            this.ScriptsPath = $"Assets\\{RootPath}\\Scripts";
        }

        public static void MakeDirectory( string directory )
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            
                // Unity側にフォルダが作成されたことを認識させる
                AssetDatabase.Refresh();
            }
        }

        public bool CheckDirty(DataTableAsset asset)
        {
            var dirdy = SaveDataTable.CheckScriptModified(
                    asset,
                    asset.name,
                    DefaultNamespace,
                    ScriptsPath);

            return dirdy;
        }
    }
}