using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal class DataTableManagerTreeView : VisualElement
    {
        private DataTableManager Manager = null;
        private bool IsStructureMode = false;
        private bool isDirtySelf;

        public Func<string,DataTableTree.Item,bool,bool> OnSelectDataTableAsset;
        
        public DataTableManagerTreeView(DataTableManager manager,bool isStructureMode)
        {
            this.Manager = manager;
            this.IsStructureMode = isStructureMode;
            CreateGUI();
        }
        
        private void CreateGUI()
        {
            var so = new SerializedObject(Manager);

            var treeView = new SerializableTreeView<DataTableTree.Item>(Manager.Tree,IsStructureMode);
            treeView.hierarchyChanged += tree =>
            {
                isDirtySelf = true;
                Undo.RecordObject(Manager, "Update DataTableManager HierarchyChanged");
                Manager.Tree.FromTree(tree);
                EditorUtility.SetDirty(Manager);
                AssetDatabase.SaveAssetIfDirty( Manager );
            };
            treeView.OnSelectItem = (str,asset,isFolder) => OnSelectDataTableAsset.Invoke(str,asset,isFolder);
            treeView.OnRemoveItem = RemoveDataTableAsset;
            treeView.style.flexGrow = 1;
            treeView.Bind(so);
            treeView.TrackSerializedObjectValue(so, a =>
            {
                if (!isDirtySelf)
                {
                    treeView.BuildTree(Manager.Tree);
                }
                isDirtySelf = false;
            });
            treeView.onMakeItem = (id,node,isFold,hasChildren) =>
            {
                var root = new VisualElement();
                root.style.flexDirection = FlexDirection.Row;

                var icon = new Image();
                icon.style.width = 16;
                icon.style.height = 16;
                root.Add(icon);
                if (node.IsFolder)
                {
                    icon.image = EditorResources.FolderIcon(isFold,!hasChildren);
                    
                    if (IsStructureMode)
                    {
                        if (treeView.HotCreateId == id)
                        {
//                             Debug.Log("HotCreateId");
                        }
                        
                        var textField = new TextField();
                        var inputElement = textField.Q("unity-text-input");
                        if (inputElement != null)
                        {
                            inputElement.style.borderTopWidth = 0;
                            inputElement.style.borderBottomWidth = 0;
                            inputElement.style.borderLeftWidth = 0;
                            inputElement.style.borderRightWidth = 0;

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

                   Texture image = node.Item != null ? node.Item.Icon : null;
                   icon.image = image ?? EditorResources.RefreshIcon;
                    var label = new Label();
                    label.text = node.Name;
                    root.Add(label);
                }

                return root;
            };
            treeView.onCreateItem = (Position, func) =>
            {
                var popup = new DataTableCreateTablePopup(Manager)
                {
                    clickCreateButton = className =>
                    {
                        var newItem = new DataTableTree.Item();
                        Debug.Log(newItem);
                        func(className,newItem);
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
                    var popup = new DataTableCreateTablePopup(Manager)
                    {
                        clickCreateButton = className =>
                        {
                            treeView.InsertNewTree(-1,className,null,false);
                        }
                    };
                    // 1. ボタンの左上を (0, 0) とした相対座標
                    Vector2 localPos = evt.position;
                    UnityEditor.PopupWindow.Show( new Rect( localPos , new Vector2() ), popup);
                });
                Add(button);
            }
        }


        void RemoveDataTableAsset( IEnumerable<DataTableTree.Item> assets)
        {
            if (assets == null) return;

            bool isREemoveAsset = false;
            foreach (var asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset.tableAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var classScript = MonoScript.FromScriptableObject(asset.tableAsset);
                    AssetDatabase.DeleteAsset(assetPath);
                    if (classScript != null)
                    {
                        isREemoveAsset = true;
                        string scriptPath = AssetDatabase.GetAssetPath(classScript);
                        AssetDatabase.DeleteAsset(scriptPath);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            if (isREemoveAsset)
            {
                Undo.ClearAll();
            }
        }
    }
}