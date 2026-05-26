using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    public class DataTableManagerTreeView : VisualElement
    {
        private static Texture FolderIcon = EditorGUIUtility.IconContent("d_Folder Icon").image;
        private static Texture FolderEmptyIcon = EditorGUIUtility.IconContent( "d_FolderEmpty Icon").image;
        private static Texture FolderOpenIcon = EditorGUIUtility.IconContent("d_FolderOpened Icon").image;
        public static Texture ItemIcon = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;
        
        private DataTableManager Manager = null;
        private bool IsStructureMode = false;

        public Func<DataTableAsset,bool> OnSelectDataTableAsset;
        
        public DataTableManagerTreeView(DataTableManager manager,bool isStructureMode)
        {
            this.Manager = manager;
            this.IsStructureMode = isStructureMode;
            CreateGUI();
        }
        
        private void CreateGUI()
        {
            var so = new SerializedObject(Manager);

            var treeView = new SerializableTreeView<DataTableAsset>(Manager.Tree,IsStructureMode);
            treeView.hierarchyChanged += tree =>
            {
                Undo.RecordObject(Manager, "Update DataTableManager HierarchyChanged");
                Manager.Tree.FromTree(tree);
                EditorUtility.SetDirty(Manager);
            };
            treeView.OnSelectItem = asset => OnSelectDataTableAsset.Invoke(asset);
            treeView.OnRemoveItem = RemoveDataTableAsset;
            treeView.style.flexGrow = 1;
            treeView.Bind(so);
            treeView.TrackSerializedObjectValue(so, a => treeView.BuildTree(Manager.Tree) );
            treeView.makeItem = (id,node,isFold,hasChildren) =>
            {
                var root = new VisualElement();
                root.style.flexDirection = FlexDirection.Row;

                var icon = new Image();
                icon.style.width = 16;
                icon.style.height = 16;
                root.Add(icon);

                if (node.IsFolder)
                {
                    icon.image = isFold ? (hasChildren ? FolderIcon:FolderEmptyIcon) : FolderOpenIcon;
                    if (IsStructureMode)
                    {
                        var textField = new TextField();
                        var inputElement = textField.Q("unity-text-input");
                        if (inputElement != null)
                        {
                            inputElement.style.borderTopWidth = 0;
                            inputElement.style.borderBottomWidth = 0;
                            inputElement.style.borderLeftWidth = 0;
                            inputElement.style.borderRightWidth = 0;

                            // 背景も透明にしたい場合
                            inputElement.style.backgroundColor = Color.clear;
                        }

                        textField.value = node.Name;
                        textField.RegisterCallback<FocusOutEvent>(evt =>
                        {
                            if (node.Name != textField.value)
                            {
                                treeView.TreeNameChange(id, textField.value);
                            }
                        });
                        root.Add(textField);
                    }
                    else
                    {
                        var label = new Label(node.Name);
                        root.Add(label);
                    }
                }
                else
                {
                   var image = AssetPreview.GetMiniThumbnail(node.Item);
                    icon.image = image;
                    var label = new Label();
                    label.text = node.Name;
                    root.Add(label);
                }

                return root;
            };
            treeView.onCreateItem = (Position, func) =>
            {
                var popup = new DataTableCreateTablePopup(Manager.DefaultNamespace)
                {
                    clickCreateButton = className =>
                    {
                        var tableAsset = CreateDataTableAsset(className);
                        func(className,tableAsset);
                    }
                };
                UnityEditor.PopupWindow.Show(Position, popup);                    
            };
            
            Add(treeView);

            if (IsStructureMode)
            {
                var button = new Button();
                button.text = "Add Table";
                button.RegisterCallback<ClickEvent>(evt =>
                {
                    var popup = new DataTableCreateTablePopup(Manager.DefaultNamespace)
                    {
                        clickCreateButton = className =>
                        {
                            var tableAsset = CreateDataTableAsset(className);
                            treeView.InsertNewTree(-1,className,tableAsset);
                        }
                    };
                    // 1. ボタンの左上を (0, 0) とした相対座標
                    Vector2 localPos = evt.position;
                    UnityEditor.PopupWindow.Show( new Rect( localPos , new Vector2() ), popup);
                });
                Add(button);
            }
        }

        DataTableAsset CreateDataTableAsset(string name)
        {
            var dataTableAsset = ScriptableObject.CreateInstance<DataTableAsset>();
            
            if (!System.IO.Directory.Exists(Manager.TablesPath))
            {
                System.IO.Directory.CreateDirectory(Manager.TablesPath);
            
                // Unity側にフォルダが作成されたことを認識させる
                AssetDatabase.Refresh();
            }
            
            AssetDatabase.CreateAsset(dataTableAsset, $"{Manager.TablesPath}\\{name}.asset");
            EditorGUIUtility.SetIconForObject(dataTableAsset, DataTableManagerTreeView.ItemIcon as Texture2D);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            SaveDataTable.CheckNeedEnsureAddressable(dataTableAsset,true);

            SaveDataTable.SaveScript(
                dataTableAsset,
                dataTableAsset.name,
                Manager.DefaultNamespace,
                Manager.ScriptsPath);
            
            return dataTableAsset;
        }

        void RemoveDataTableAsset( IEnumerable<DataTableAsset> assets)
        {
            if (assets == null) return;

            foreach (var asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var classScript = asset.classScript;
                    AssetDatabase.DeleteAsset(assetPath);
                    if (asset.classScript != null)
                    {
                        string scriptPath = AssetDatabase.GetAssetPath(classScript);
                        AssetDatabase.DeleteAsset(scriptPath);
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}