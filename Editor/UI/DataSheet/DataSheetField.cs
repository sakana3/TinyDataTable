using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    internal partial class DataSheetField : VisualElement
    {
        public struct Item
        {
            public int id;
            public int index;
            public bool isObsolete;
        }

        public bool IsStructureMode { private set; get; }
        private MultiColumnListView _multiColumnListView;
        private static Color _obsoleteColor = new Color( Color.darkViolet.r,Color.darkViolet.g,Color.darkViolet.b , 0.25f );
        private List<TextField> idTextFieldList = new List<TextField>();
        private List<Item> rowIDList = new List<Item>();
        private List<int> columnIDList = new List<int>();
        private DataTableManager Manager;
        private RecordPropertyUtil _recordPropertyUtil;
        private DataTableRecordBase targetAsset => _recordPropertyUtil.TargeTableAsset;

        public DataSheetField( 
            DataTableManager manager,
            DataTableRecordBase asset,
            bool IsStructureMode)
        {
            Manager = manager;
            _recordPropertyUtil = new(asset);
            this.IsStructureMode = IsStructureMode;
            
            // 拡張子 (.uss) を含めて指定します
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DataSheetField).Assembly);
            string ussPath = $"{packageInfo.assetPath}/Editor/Assets/UIElement/TinyDataTableMultiColumListViewStyle.uss";
            StyleSheet stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (stylesheet != null)
            {
                this.styleSheets.Add(stylesheet);
            }

            _multiColumnListView = CreateListView();

            Add(_multiColumnListView);
        }

        public MultiColumnListView CreateListView()
        {
            idTextFieldList.Clear();

            var listView = new MultiColumnListView()
            {
                name = _recordPropertyUtil.TargeTableAsset.name,
                reorderable = IsStructureMode,
                reorderMode = ListViewReorderMode.Simple, //AnimatedにするとdragAndDropUpdateが来ない

                showAddRemoveFooter = IsStructureMode,
                sortingMode = ColumnSortingMode.None,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBoundCollectionSize = false,
//                showFoldoutHeader = true,
                selectionType = IsStructureMode ? SelectionType.Multiple : SelectionType.Single
            };
            _multiColumnListView = listView;
            listView.columns.reorderable = false;
            listView.columns.resizePreview = true;
            listView.columns.resizable = true;
            listView.style.overflow = Overflow.Visible; // 通常はHiddenにしてスクロールバーに任せる

            listView.itemIndexChanged += (form, to) =>
            {
                _recordPropertyUtil.MoveRow(form, to);
            };
            //Invalidのドラッグ＆ドロップを禁止する
            listView.canStartDrag += args => args.id is not 0;
            //Invalidの上には移動できないようにする
            listView.dragAndDropUpdate += (args) =>
                (args.insertAtIndex is 0) ? DragVisualMode.Rejected : DragVisualMode.Move;
            listView.columnSortingChanged += () => { Debug.Log("columnSortingChanged"); };

            if (IsStructureMode)
            {
                listView.makeFooter = () => { return MakeFooter(); };
            }

            this.TrackSerializedObjectValue(_recordPropertyUtil.SerializedObject, (prop) =>
            {
                if (_recordPropertyUtil.IsChanged)
                {
                    SetupColumns(listView);
                    SetupRows();                    
                    _multiColumnListView.Rebuild();
                }
            });
            
            SetupColumns(listView);

            SetupRows();
            return listView;
        }

        /// <summary>
        /// 行をセットアップする
        /// </summary>
        /// <param name="property"></param>
        /// <param name="listView"></param>
        private void SetupRows()
        {
            var list = _recordPropertyUtil.MakeRecordHeaderList();

            if (IsStructureMode)
            {
                rowIDList = list
                    .Select((i,index) => new Item() { id = i.id ,isObsolete = i.isObsolete, index = index})
                    .ToList();
            }
            else
            {
                rowIDList = list
                    .Select((i,index) => new Item() { id = i.id ,isObsolete = i.isObsolete, index = index})
                    .Where( i => i.id != 0 && i.isObsolete is false && string.IsNullOrEmpty(list[i.index].name) is false )
                    .ToList();
            }
            
            _multiColumnListView.itemsSource = rowIDList;
        }
        
        /// <summary>
        /// 列をセットアップする
        /// </summary>
        /// <param name="property"></param>
        /// <param name="listView"></param>
        private void SetupColumns(MultiColumnListView listView)
        {
            //Make Columns
            columnIDList.Clear();
            //Clearを呼ぶと何故かコールバックが呼ばれるので潰してから呼ぶ。どう考えてもバグ
            foreach (var column in listView.columns)
            {
                column.makeHeader = null;
                column.bindCell = null;
                column.makeCell = null;
            }

            //fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(property);
            
            listView.columns.Clear();

            var indexColumn = MakeIndexColumn();
            listView.columns.Add(indexColumn);
            
            var recordNameColumn = MakeIDNameColumn();
            listView.columns.Add(recordNameColumn);
            

            for (int i = 0; i < _recordPropertyUtil.FieldInfos.Count; i++)
            {
                if ( IsStructureMode || _recordPropertyUtil.FieldInfos[i].Obsolete is false)
                {
                    var columProp = MakePropertyColumn(i);
                    listView.columns.Add(columProp);
                }
            }

            if (IsStructureMode)
            {
                var lastColumn = MakeLastColumn();
                listView.columns.Add(lastColumn);
            }
        }        

        private Column MakeIndexColumn()
        {
            var colum = new Column()
            {
                name = "Index",
                makeCell = () =>
                {
                    var e= new VisualElement() { };
                    e.style.flexGrow = 1.0f;
                    return e;
                },
                bindCell = (e,idx) =>
                {
                    var iRow = rowIDList[idx].index;                    
                    if (iRow > 0)
                    {
                        var label = new Label();
                        label.text =$"{iRow }";
                        label.style.unityTextAlign = TextAnchor.MiddleCenter;
                        label.AddManipulator( MakeRowIndexManipulator(label,iRow) );
                        label.style.flexGrow = 1.0f ;
                        e.style.backgroundColor = rowIDList[idx].isObsolete?_obsoleteColor:new StyleColor();                        
                        e.Clear();
                        e.Add(label);
                    }
                    else
                    {
                        e.AddManipulator( MakeRowIndexManipulator(e,iRow) );
                    }
                    e.parent.style.justifyContent = Justify.Center;
                },
                stretchable = false,
                resizable = false,
                width = 40    ,
                maxWidth = 40,
            };
            colum.makeHeader = () => MakeColumHeader(null, "Index", false, "Index");
            return colum;            
        }
     
        private Column MakeIDNameColumn()
        {
            var colum = new Column()
            {
                name = "ID",                

                makeCell = () =>
                {
                    var e = new VisualElement();
                    e.style.flexGrow = 1.0f;
                    var textField = new TextField() { };
                    var inputElement = textField.Q(className: "unity-text-field__input");
                    if (inputElement != null)
                    {
                        inputElement.style.backgroundColor = Color.clear;
                        inputElement.style.borderTopWidth = 0;
                        inputElement.style.borderBottomWidth = 0;
                        inputElement.style.borderLeftWidth = 0;
                        inputElement.style.borderRightWidth = 0;
                    }                    
                    textField.textEdition.placeholder = "Please input name.";
                    e.Add(textField);

                    return e;
                },
                bindCell = (e,idx) =>
                {
                    var iRow = rowIDList[idx].index;
                    var textField = e.Q<TextField>();
                    if( textField != null )
                    {
                        var nameProperty = _recordPropertyUtil.GetRecordNameProperty(iRow);
                        textField.BindProperty(nameProperty);
                        textField.RegisterValueChangedCallback(evt =>
                        {
                            ReloadIDText();
                        });
                        e.userData = nameProperty;
                        ReloadIDText(textField);
                        textField.SetEnabled(iRow > 0);
                        idTextFieldList.Add(textField);
                    }
                    var isObsolete = _recordPropertyUtil.RowHeaders[iRow].obsolete;
                    e.style.backgroundColor = isObsolete?_obsoleteColor:new StyleColor();                        
                },
                unbindCell = (e,i) =>
                {
                    if( e is TextField textField )
                    {
                        idTextFieldList.Remove(textField);
                    }
                },
                stretchable = false,
                width = 120,
            };
            colum.makeHeader = () =>
            {
                var header = MakeColumHeader(colum, "ID", false, "ID");
//                    var manipulator = MakeMenuManipulator(property,header ,-1);
//                    header.AddManipulator( manipulator);
                return header;
            };            
            LoadColumnWidths(colum , 120.0f);
            return colum;                        
        }
 
        private Column MakePropertyColumn( int iColum )
        {
            Column colum = new Column()
            {
                name = _recordPropertyUtil.FieldInfos[iColum].Name,
                makeCell = () => new VisualElement() { },
                bindCell = (e,idx) =>
                {
                    var iRow = rowIDList[idx].index;                            
                    
                    var isObsoleteCol = _recordPropertyUtil.FieldInfos[iColum].Obsolete;
                    var isObsoleteRow = rowIDList[idx].isObsolete;
                    e.style.flexGrow = 1.0f;
                    e.style.backgroundColor = (isObsoleteCol|isObsoleteRow)?_obsoleteColor:new StyleColor();

                    e.Clear();
                    if (_recordPropertyUtil.FieldInfos.Count > iColum)
                    {
                        var prop = _recordPropertyUtil.RecordProperty
                            .GetArrayElementAtIndex(iRow)
                            .FindPropertyRelative(_recordPropertyUtil.FieldInfos[iColum].Name);
                        if (prop != null)
                        {
                            if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
                            {
                                var arrayField = new TinyReordableListField( prop);                                
                                e.Add(arrayField);
                            }
                            else
                            {
                                var propertyField = new PropertyField(prop, string.Empty);
                                propertyField.BindProperty(prop);
                                propertyField.AddToClassList("no-frame-field");
//                    propertyField.AddToClassList("right-align-field");
                                e.Add(propertyField);
                            }
                        }
                    }
                },
                unbindCell = (e,i) =>
                {
                    e.Clear();                        
                },                
//                stretchable = true,
                resizable = true,
                width = 80,
            };
            colum.makeHeader = () =>
            {
                var fieldInfo = _recordPropertyUtil.FieldInfos[iColum];
                var header = MakeColumHeader(colum, fieldInfo.Name, fieldInfo.Obsolete, fieldInfo.Description) as Label;
                if (IsStructureMode)
                {
                    var manipulator = MakeColumHeaderManipulator(header, iColum);
                    header.AddManipulator(manipulator);
                }

                columnIDList.Add(fieldInfo.Name.GetHashCode());
                return header;
            };            
            LoadColumnWidths(colum,120.0f);
            return colum;
        }

        private static Texture2D plusTex = (Texture2D)EditorGUIUtility.IconContent("Toolbar Plus").image;        
        private static Texture2D minusTex = (Texture2D)EditorGUIUtility.IconContent("Toolbar Minus").image;        
        private Column MakeLastColumn()
        {
            Column colum = new Column()
            {
                stretchable = false,
                resizable = false,
                width = 32,
                maxWidth = 32,
//                minWidth = 42,
                makeHeader = () =>
                {
                    var button = new Image();
                    button.image = plusTex;
                    button.RegisterCallback<MouseDownEvent>((t) =>
                    {
                        if (t.button == 0)
                        {
                            OpenAddSchemaPopup( -1, button.worldBound);
                        }
                    });
                    button.tooltip = "Add Field";
                    var manipulator = MakeAddSchemaManipulator(button);
                    button.AddManipulator( manipulator);
                    return button;
                },
                makeCell = () => new VisualElement() { },
                optional = true,
            };
            return colum;
        }

        private VisualElement MakeColumHeader(
            Column column,
            string name ,
            bool isObsolete,
            string description)
        {
            var label = new Label(){ text = name };
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.paddingTop = 2.0f;
            label.style.paddingBottom = 2.0f;
            label.style.backgroundColor = isObsolete?_obsoleteColor:new StyleColor();
            label.tooltip = description;
            RegisterColumnResizeCallbacks(column,label);
            return label;
        }

        /// <summary>
        /// IDのテキストを更新する
        /// </summary>
        private void ReloadIDText()
        {
            _recordPropertyUtil.ReloadInfo();
            foreach (var textField in idTextFieldList)
            {
                ReloadIDText(textField);
            }
        }

        /// <summary>
        /// 指定フィールドのテキストを更新する
        /// </summary>
        private void ReloadIDText( TextField textField )
        {
            var value = textField.value;
            
            var input = textField.Q(className: "unity-text-field__input");
            if (input != null)
            {
                if ( string.IsNullOrEmpty(textField.value) )
                {
                    input.style.color = StyleKeyword.Null;
                    textField.tooltip = "Input ID name";                    
                }
                else if (SerializableUtility.CheckCSharpSafeName(textField.value) is false)
                {
                    input.style.color = Color.red;
                    textField.tooltip = "Invalid C# identifier.";
                }
                else if (_recordPropertyUtil.RowHeaders.Count( t => t.name == value ) >= 2 ||
                         _recordPropertyUtil.FieldInfos.Select(f=>f.Name).Contains(  value ) )
                {
                    input.style.color = Color.yellow;
                    textField.tooltip = "This name is conflict.";
                }
                else
                {
                    input.style.color = StyleKeyword.Null;
                    textField.tooltip = string.Empty;
                }
            }
        }

        private VisualElement MakeFooter()
        {
            var root = new VisualElement();
            root.AddToClassList("unity-list-view__footer");
            
            var TableSizeField = new UnsignedIntegerField()
            {
                value = (uint)_recordPropertyUtil.RowHeaders.Count -1
            };
            TableSizeField.SendToBack();
            TableSizeField.style.marginRight = 4.0f;
            TableSizeField.TrackPropertyValue(_recordPropertyUtil.HeaderProperty,
                (t) =>
                {
                    TableSizeField.SetValueWithoutNotify((uint)_recordPropertyUtil.RowHeaders.Count-1);
                });
            root.Add(TableSizeField);

            // 編集終了（Enterキー or フォーカス外れ）
            TableSizeField.RegisterCallback<FocusOutEvent>(evt =>
            {
                _recordPropertyUtil.ResizeRow( Math.Min((int)TableSizeField.value+1,Manager.RowLimit+1) );
                SetupRows();
                _multiColumnListView.ClearSelection();
                _multiColumnListView.Rebuild();                
            });

            //追加ボタン
            var addButton = new VisualElement();
            addButton.style.backgroundImage = plusTex;
            addButton.AddToClassList("unity-button");
            addButton.RegisterCallback<MouseDownEvent>((t) =>
            {
                if (t.button == 0)
                {
                    _recordPropertyUtil.AddRow();
                    SetupRows();
                    _multiColumnListView.RefreshItems();                               
                }
            });
            root.Add( addButton);
            
            //削除ボタン
            var removeButton = new VisualElement();
            removeButton.style.backgroundImage = minusTex;
            removeButton.SetEnabled( _recordPropertyUtil.RowHeaders.Count > 1 );
            removeButton.AddToClassList("unity-button");
            removeButton.RegisterCallback<MouseDownEvent>((t) =>
            {
                if (t.button == 0)
                {
                    var removeIndexList = _multiColumnListView.selectedIndices.Any()
                        ? _multiColumnListView.selectedIndices.ToArray()
                        : new int[] { rowIDList.Count - 1 };
                    RemoveRow(removeIndexList);
                }
            });
            removeButton.TrackPropertyValue( _recordPropertyUtil.HeaderProperty,
                (t) =>
                {
                    removeButton.SetEnabled( _recordPropertyUtil.RowHeaders.Count > 1 );
                });            
            root.Add( removeButton);
            
            return root;
        }
        
        private void RemoveRow( params int[] indexs)
        {
            var removes = indexs
                .Where( i => i > 0 )
                .Where(i => _recordPropertyUtil.RowHeaders[i].obsolete)
                .OrderByDescending(i => i)
                .ToArray();
            if (removes.Length > 0)
            {
                _recordPropertyUtil.RemoveRows(removes);

                foreach (var i in removes)
                {
                    rowIDList.RemoveAt(i);
                }

                //これをやらないと変更が通知されないことがある？
                _multiColumnListView.itemsSource = rowIDList;
                _multiColumnListView.ClearSelection();
                SetupRows();
                _multiColumnListView.RefreshItems();
                _recordPropertyUtil.ReloadInfo();
                _multiColumnListView.Rebuild();
            }
            else
            {
                foreach (var index in indexs.Where( i => i > 0))
                {
                    _recordPropertyUtil.SetRowObsolete(index, true);
                }
                _multiColumnListView.RefreshItems();
            }
        }
    }
}