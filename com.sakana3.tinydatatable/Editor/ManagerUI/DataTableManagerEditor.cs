using System;
using System.Linq;
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
            EditMode ,
            BuildMode ,
            Preference,
            Addressable,
        }

        private string[] ModeStr = new[]
        {
            "Edit Mode","Build Mode","Preference","Addressable"
        };
        private Color[] ModeColor = new[]
        {
            Color.clear , Color.darkRed , Color.darkMagenta, Color.darkSlateBlue
        };
        
        public static Texture ItemIcon = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;
        
        private DataTableManager manager = null;

        public Mode mode
        {
            private set => EditorPrefs.SetInt("DataTableManagerEditorMode", (int)value);
            get => (Mode)EditorPrefs.GetInt("DataTableManagerEditorMode", (int)Mode.EditMode);
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
//        private ToolbarButton toolbarBuildButton;
        private DataTableManagerTreeView treeView;
        private DataTableManagerTableView tableView;
        private DataTableManagerTableOperator tableOperator;

        private bool IsBuildMode => mode == Mode.BuildMode;

        private void CreateGUI()
        {
            var so = new SerializedObject(manager);

            toolbar = new Toolbar();
            Add(toolbar);

            //Mode Select
            var modeMenu = new ToolbarMenu()
            {
                text = ModeStr[(int)mode],
                tooltip = "Mode Select",
            };
            modeMenu.style.backgroundColor = ModeColor[(int)mode];
            modeMenu.style.width = 100;
            modeMenu.menu.AppendAction(ModeStr[0],
                action =>
                {
                    modeMenu.style.backgroundColor = ModeColor[0];
                    modeMenu.text = action.name;
                    ModeChange(Mode.EditMode);
                },
                a => mode == Mode.EditMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );            
            modeMenu.menu.AppendAction(ModeStr[1],
                action =>
                {
                    modeMenu.style.backgroundColor = ModeColor[1];
                    modeMenu.text = action.name;
                    ModeChange(Mode.BuildMode);
                },
                a => mode == Mode.BuildMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
            toolbar.Add(modeMenu);
            modeMenu.menu.AppendAction(ModeStr[2],
                action =>
                {
                    modeMenu.style.backgroundColor = ModeColor[2];
                    modeMenu.text = action.name;
                    ModeChange(Mode.Preference);
                },
                a => mode == Mode.Preference ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
            toolbar.Add(modeMenu);
            //Tool
            var actionMenu = new ToolbarMenu()
            {
                text = "Utility",
            };
            actionMenu.menu.AppendAction( "Build All",
                action =>
                {
                    BuildAll();
                },
                a => DropdownMenuAction.Status.Normal
            );
            toolbar.Add(modeMenu);            
            toolbar.Add(actionMenu);

/*            
            var spacer = new ToolbarSpacer()
            {
                flex = true
            };
            toolbar.Add(spacer);
            
            toolbarBuildButton = new ToolbarButton()
            {
                text = "Build All",
            };
            toolbarBuildButton.clicked += BuildAll;
            toolbar.Add(toolbarBuildButton);
*/
            this.style.flexGrow = 1;

            Root = new VisualElement();
            Root.style.flexGrow = 1;
            Add(Root);
            
            ModeChange(mode);
        }

        private void ModeChange( Mode mode )
        {
            this.mode = mode;
            MakeTreeView();
        }

        private void BuildAll()
        {
            var tables = manager.Tree.Nodes
                .Where(t => t.IsFolder is false && t.Item != null)
                .Select(t => t.Item);

            foreach (var table in tables)
            {
                SaveDataTable.SaveScript(table.tableAsset);
            }
        }
        
        
        private void MakeTreeView()
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
                treeView = new DataTableManagerTreeView(manager, IsBuildMode)
                {
                    OnSelectDataTableAsset = OnSelectDataTableAsset,
                };
                treeView.style.flexGrow = 1;
                treeViewRoot.Add(treeView);
            }
        }

        private bool OnSelectDataTableAsset( string treeName, DataTableTree.Item item , bool isFolder)
        {
            if ( tableOperator == null || tableOperator.OnChange(item))
            {
                if (tableView != null)
                {
                    if (tableView.item != null )
                    {
                        if (tableView.item.lazeyTable.isSet )
                        {
//                          Resources.UnloadAsset(tableView.item.tableAsset);
                        }
                    }
                    tableView = null;
                }
                tableViewRoot.Clear();
                
                if( isFolder )
                {
                    tableOperator = null;
                }                
                else if (item?.tableAsset != null)
                {
                    if (IsBuildMode)
                    {
                        item.tableAsset.InjectRelation();
                        tableOperator = new DataTableManagerTableOperator(manager, item);
                        tableViewRoot.Add(tableOperator);
                    }
                    tableView = new DataTableManagerTableView(manager, item, IsBuildMode);
                    tableView.style.flexGrow = 1;
                    tableViewRoot.Add(tableView);
                }
                else
                {
                    if (IsBuildMode)
                    {
                        var constructTableView = new DataTableConstructTableView(manager,treeName);
                        constructTableView.style.flexGrow = 1;
                        tableViewRoot.Add(constructTableView);
                    }
                }
                return true;
            }
            return false;
        }
    }
}