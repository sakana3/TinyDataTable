using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    public class DataTableManagerWindow : EditorWindow
    {
        // メニューアイテムを追加 (Window > My Sample Window)
        [MenuItem("Window/TinyDataTable")]
        public static void ShowWindow()
        {
            // ウィンドウを表示 (既存ならフォーカス、なければ作成)
            GetWindow<DataTableManagerWindow>("TinyDataTable");
        }

        private DataTableManager dataTableManager;

        
        /// <summary>
        /// Unityエディタでエディターウィンドウが有効化されたときに呼び出されるメソッド。
        /// 必要なリソースをロードし、シリアライズされたオブジェクトの初期化を行う。
        /// </summary>
        private void OnEnable()
        {
            dataTableManager = null;
            if (dataTableManager == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DataTableManager");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids.First());
                    dataTableManager = AssetDatabase.LoadAssetAtPath<DataTableManager>(path);
                }
            }

        }
        
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            
            // ルート要素
            var root = new VisualElement();
            root.style.flexGrow = 1;
            rootVisualElement.Add(root);
            
            if (dataTableManager == null)
            {
                var welcome = new DataTableManagerWelcome(dataTableManager);
                welcome.OnClickStart = CreateManager;
                root.Add(welcome);
            }
            else
            {
                var editor = new DataTableManagerEditor(dataTableManager);
                root.Add(editor);
            }
        }

        public void CreateManager(DataTableManager manager)
        {
            this.dataTableManager = manager;
       
            EditorGUIUtility.SetIconForObject(manager, DataTableManagerTreeView.ItemIcon as Texture2D);
            
            EditorPrefs.SetInt("DataTableManagerEditorMode", (int)DataTableManagerEditor.Mode.Structure);
            
            CreateGUI();
        }
    }
}