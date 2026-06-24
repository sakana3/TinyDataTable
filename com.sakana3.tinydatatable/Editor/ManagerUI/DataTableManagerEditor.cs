using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal class DataTableManagerEditor : VisualElement
    {
        public static Texture FolderIcon = EditorGUIUtility.IconContent("d_Folder Icon").image;
        public static Texture FolderEmptyIcon = EditorGUIUtility.IconContent( "d_FolderEmpty Icon").image;
        public static Texture FolderOpenIcon = EditorGUIUtility.IconContent("d_FolderOpened Icon").image;

        public enum Mode
        {
            DesignMode ,
            BuildMode ,
            Preference,
            Addressable,
        }

        private string[] ModeStr = new[]
        {
            "Design Mode","Build Mode","Preference","Addressable"
        };
        
        public static Texture ItemIcon = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;
        
        private DataTableManager manager = null;

        public Mode mode
        {
            private set => EditorPrefs.SetInt("DataTableManagerEditorMode", (int)value);
            get => (Mode)EditorPrefs.GetInt("DataTableManagerEditorMode", (int)Mode.DesignMode);
        }

        public DataTableManagerEditor(DataTableManager manager)
        {
            this.manager = manager;
            CreateGUI();
        }

        private TwoPaneSplitView splitView;
        private VisualElement treeViewRoot;
        private VisualElement tableViewRoot;
        private VisualElement Root;

        private Toolbar toolbar;
        private DataTableManagerTreeView treeView;
        private bool isStructureMode => mode == Mode.BuildMode;
        private DataTableManagerTableOperator tableOperator;

        private void CreateGUI()
        {
            var so = new SerializedObject(manager);

            toolbar = new Toolbar();
            Add(toolbar);

            var modeMenu = new ToolbarMenu()
            {
                text = ModeStr[(int)mode],
                tooltip = "Change Mode",
            };
            modeMenu.style.width = 120;
            modeMenu.menu.AppendAction(ModeStr[0],
                action =>
                {
                    modeMenu.text = action.name;
                    mode = Mode.DesignMode;
                    CreateTreeView();
                },
                a => mode == Mode.DesignMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );            
            modeMenu.menu.AppendAction(ModeStr[1],
                action =>
                {
                    modeMenu.text = action.name;
                    mode = Mode.BuildMode;
                    CreateTreeView();
                },
                a => mode == Mode.BuildMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
            toolbar.Add(modeMenu);
            modeMenu.menu.AppendAction(ModeStr[2],
                action =>
                {
                    modeMenu.text = action.name;
                    mode = Mode.Preference;
                    CreateTreeView();
                },
                a => mode == Mode.Preference ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
            toolbar.Add(modeMenu);

            this.style.flexGrow = 1;

            Root = new VisualElement();
            Root.style.flexGrow = 1;
            Add(Root);
            
            CreateTreeView();
        }

        
        private void CreateTreeView()
        {
            Root.Clear();
            if (mode == Mode.Preference)
            {
                var preference = new DataTableManagerPreference(manager);
                Root.Add(preference);
            }
            else
            {
                splitView = new TwoPaneSplitView(
                    fixedPaneIndex: 0,
                    fixedPaneStartDimension: 200,
                    TwoPaneSplitViewOrientation.Horizontal
                );
                splitView.style.flexGrow = 1;
                Root.Add(splitView);
                treeViewRoot = new VisualElement();
                tableViewRoot = new VisualElement();

                splitView.Add(treeViewRoot);
                splitView.Add(tableViewRoot);
                treeView = new DataTableManagerTreeView(manager, isStructureMode)
                {
                    OnSelectDataTableAsset = OnSelectDataTableAsset,
                };
                treeView.style.flexGrow = 1;
                treeViewRoot.Add(treeView);
            }
        }
        
        private bool OnSelectDataTableAsset(DataTableRecordBase asset)
        {
            if ( tableOperator == null || tableOperator.OnChange(asset))
            {
                tableViewRoot.Clear();
                if (asset != null)
                {
                    if (isStructureMode)
                    {
                        DataTableManager.InjectRelation( asset);
                        tableOperator = new DataTableManagerTableOperator(manager, asset);
                        tableViewRoot.Add(tableOperator);
                    }
                    var tableView = new DataTableManagerTableView(manager, asset, isStructureMode);
                    tableView.style.flexGrow = 1;
                    tableViewRoot.Add(tableView);
                }
                else
                {
                    tableOperator = null;
                }
                return true;
            }
            return false;
        }
    }
}