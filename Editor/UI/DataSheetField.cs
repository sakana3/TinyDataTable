using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    public partial class DataSheetField : VisualElement
    {
        public struct Item
        {
            public int id;
            public int index;
            public bool isObsolete;
        }
        
        private SerializedProperty _property = null;
        private MultiColumnListView _multiColumnListView;
        private static Color _obsoleteColor = new Color( Color.darkViolet.r,Color.darkViolet.g,Color.darkViolet.b , 0.25f );
        private List<TextField> idTextFieldList = new List<TextField>();
        private List<Item> rowIDList = new List<Item>();
        private List<int> columnIDList = new List<int>();
        private ( List<string> fieldNames, List<string> recordNames ) _names = (null,null);
        private List<int> fieldOrderList = new List<int>();
        private bool IsStructureMode;
        private DataTableManager Manager;
        
        public DataSheetField( DataTableManager manager, SerializedProperty property,bool IsStructureMode)
        {
            _property = property;
            Manager = manager;
            this.IsStructureMode = IsStructureMode;
            
            // 拡張子 (.uss) を含めて指定します
            var styleSheet = EditorGUIUtility.Load("TinyDataTableMultiColumListViewStyle.uss") as StyleSheet;
            if (styleSheet != null)
            {
                this.styleSheets.Add(styleSheet);
            }
  
            _multiColumnListView = CreateListView(property);
            Add(_multiColumnListView);
        }

        public MultiColumnListView CreateListView(SerializedProperty property)
        {
            idTextFieldList.Clear();

            var listView = new MultiColumnListView()
            {
                name = property.displayName,
                reorderable = IsStructureMode,
                reorderMode = ListViewReorderMode.Simple, //AnimatedにするとdragAndDropUpdateが来ない

                showAddRemoveFooter = IsStructureMode,
                sortingMode = ColumnSortingMode.None,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBoundCollectionSize = false,
//                showFoldoutHeader = true,
                selectionType = IsStructureMode ? SelectionType.Multiple : SelectionType.None
            };
            _multiColumnListView = listView;
            listView.columns.reorderable = false;
            listView.columns.resizePreview = true;
            listView.columns.resizable = true;
            listView.style.overflow = Overflow.Visible; // 通常はHiddenにしてスクロールバーに任せる

            listView.itemIndexChanged += (form, to) => DataSheetPropertyUtility.MoveRow(property, form, to);
            //Invalidのドラッグ＆ドロップを禁止する
            listView.canStartDrag += args => args.id is not 0;
            //Invalidの上には移動できないようにする
            listView.dragAndDropUpdate += (args) =>
                (args.insertAtIndex is 0) ? DragVisualMode.Rejected : DragVisualMode.Move;
            listView.columnSortingChanged += () => { Debug.Log("columnSortingChanged"); };

            if (IsStructureMode)
            {
                listView.makeFooter = () => { return MakeFooter(property); };
            }

            this.TrackSerializedObjectValue(property.serializedObject, (prop) =>
            {
                var columnChange = DataSheetPropertyUtility.CheckColums(property, columnIDList,!IsStructureMode);
                if (columnChange is false)
                {
                    SetupColumns(property, listView);
                }
                
                var rowList = DataSheetPropertyUtility.MakeRowIDList(property)
                    .Where(i=> IsStructureMode || (i.id != 0 && i.isObsolete is false));
                var rowChange = rowIDList
                    .Select(i=>(i.id,i.isObsolete))
                    .SequenceEqual(rowList);
                if ((columnChange && rowChange) is false)
                {
                    SetupRows(property, listView);                    
                    _multiColumnListView.Rebuild();
                }
            });
            
            SetupColumns(property, listView);

            SetupRows(property, listView);
            
            return listView;
        }


        /// <summary>
        /// 行をセットアップする
        /// </summary>
        /// <param name="property"></param>
        /// <param name="listView"></param>
        private void SetupRows(SerializedProperty property, MultiColumnListView listView)
        {
            var list = DataSheetPropertyUtility.MakeRowIDList(property);

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
                    .Where( i => i.id != 0 && i.isObsolete is false)
                    .ToList();
            }

            listView.itemsSource = rowIDList;                    
        }        
        
        /// <summary>
        /// 列をセットアップする
        /// </summary>
        /// <param name="property"></param>
        /// <param name="listView"></param>
        private void SetupColumns(SerializedProperty property, MultiColumnListView listView)
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

            fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(property);
            
            listView.columns.Clear();

            var indexColumn = MakeIndexColumn(property);
            listView.columns.Add(indexColumn);
            
            var recordNameColumn = MakeIDNameColumn(property);
            listView.columns.Add(recordNameColumn);
            
            var columnsCount = DataSheetPropertyUtility.GetColumnCount(property);
            for (int i = 0; i < columnsCount; i++)
            {
                if (DataSheetPropertyUtility.ColumObsolete(property, fieldOrderList[i]).boolValue is false)
                {
                    var columProp = MakePropertyColumn(property, i);
                    listView.columns.Add(columProp);
                }
            }

            if (IsStructureMode)
            {
                var lastColumn = MakeLastColumn(property);
                listView.columns.Add(lastColumn);
            }
        }        
     
     
        private Column MakeIDNameColumn(SerializedProperty property)
        {
            _names = (null,null);
            var colum = new Column()
            {
                name = "ID",                
                makeHeader = () =>
                {
                    var header = MakeColumHeader(property, "ID",  false, "ID");
//                    var manipulator = MakeMenuManipulator(property,header ,-1);
//                    header.AddManipulator( manipulator);
                    return header;
                },
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
                    e.Add(textField);

                    return e;
                },
                bindCell = (e,idx) =>
                {
                    var iRow = rowIDList[idx].index;
                    var textField = e.Q<TextField>();
                    if( textField != null )
                    {
                        var nameProperty = DataSheetPropertyUtility.GetRowNameProperty(property,iRow);
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
                    var isObsolete = DataSheetPropertyUtility.RowObsolete(property, iRow).boolValue;
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
    
            return colum;                        
        }
      
        
        private Column MakeIndexColumn(SerializedProperty property)
        {
            var colum = new Column()
            {
                name = "Index",
                makeHeader = () => MakeColumHeader(property,"Index",false,"Index"),
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
                        label.text =$"{iRow - 1}";
                        label.style.unityTextAlign = TextAnchor.MiddleCenter;
                        label.AddManipulator( MakeRowIndexManipulator(property,label,iRow) );
                        var isObsolete = DataSheetPropertyUtility.RowObsolete(property, iRow).boolValue;
                        e.style.backgroundColor = isObsolete?_obsoleteColor:new StyleColor();                        
                        e.Clear();
                        e.Add(label);
                    }
                    else
                    {
                        e.AddManipulator( MakeRowIndexManipulator(property,e,iRow) );
                    }
                    e.parent.style.justifyContent = Justify.Center;
                },
                stretchable = false,
                resizable = false,
                width = 40    ,
                maxWidth = 40,
            };
            return colum;            
        }

        private Column MakePropertyColumn(SerializedProperty property, int iColum )
        {
            Column colum = new Column()
            {
                makeHeader = () =>
                {
                    var iField = fieldOrderList[iColum];
                    var (title,id,description,isObsolete) = DataSheetPropertyUtility.GetColumn(property,iField);
                    var header = MakeColumHeader(property, title, isObsolete,description) as Label;
                    if (IsStructureMode)
                    {
                        var manipulator = MakeColumHeaderManipulator(property, header, iField);
                        header.AddManipulator(manipulator);
                    }
                    columnIDList.Add( id);
                    return header;
                },
                makeCell = () => new VisualElement() { },
                bindCell = (e,idx) =>
                {
                    var iRow = rowIDList[idx].index;                            
                    var iField = fieldOrderList[iColum];
                    
                    var isObsoleteCol = DataSheetPropertyUtility.ColumObsolete(property,iField).boolValue;
                    var isObsoleteRow = DataSheetPropertyUtility.RowObsolete(property,iRow).boolValue;
                    e.style.flexGrow = 1.0f;
                    e.style.backgroundColor = (isObsoleteCol|isObsoleteRow)?_obsoleteColor:new StyleColor();

                    e.Clear();
                    var prop = DataSheetPropertyUtility.GetCellProperty(property,iField,iRow);
                    var propertyField = new PropertyField(prop, string.Empty);
                    propertyField.BindProperty(prop);
                    propertyField.AddToClassList("no-frame-field");
//                    propertyField.AddToClassList("right-align-field");
                    e.Add(propertyField);
                },
                unbindCell = (e,i) =>
                {
                    e.Clear();                        
                },                
//                stretchable = true,
                resizable = true,
                width = 80,
            };
            return colum;
        }

        private static Texture2D plusTex = (Texture2D)EditorGUIUtility.IconContent("Toolbar Plus").image;        
        private static Texture2D minusTex = (Texture2D)EditorGUIUtility.IconContent("Toolbar Minus").image;        
        private Column MakeLastColumn(SerializedProperty property)
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
                            OpenAddFieldPopup(property, -1, button.worldBound);
                        }
                    });
                    button.tooltip = "Add Field";
                    var manipulator = MakeAddFieldManipulator(_property,button);
                    button.AddManipulator( manipulator);
                    return button;
                },
                makeCell = () => new VisualElement() { },
                optional = true,
            };
            return colum;
        }

        private VisualElement MakeColumHeader(
            SerializedProperty property ,
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
            return label;
        }

        /// <summary>
        /// IDのテキストを更新する
        /// </summary>
        private void ReloadIDText()
        {
            _names = DataSheetPropertyUtility.MakeNameList(_property);
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
            if (_names.fieldNames == null || _names.recordNames == null)
            {
                _names = DataSheetPropertyUtility.MakeNameList(_property);
            }

            var value = textField.value;
            
            var input = textField.Q(className: "unity-text-field__input");
            if (input != null)
            {
                if ( string.IsNullOrEmpty(textField.value) )
                {
                    input.style.color = StyleKeyword.Null;
                    textField.tooltip = "Input ID name";                    
                }
                else if (DataSheetPropertyUtility.CheckCSharpSafeName(textField.value) is false)
                {
                    input.style.color = Color.red;
                    textField.tooltip = "Invalid C# identifier.";
                }
                else if (_names.recordNames.Count( t => t == value ) >= 2 ||
                         _names.fieldNames.Contains(  value ) )
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

        private VisualElement MakeFooter(SerializedProperty property)
        {
            var root = new VisualElement();
            root.AddToClassList("unity-list-view__footer");
            
            var TableSizeField = new UnsignedIntegerField()
            {
                value = (uint)DataSheetPropertyUtility.GetRowCount(property) -1
            };
            TableSizeField.SendToBack();
            TableSizeField.style.marginRight = 4.0f;
            TableSizeField.TrackPropertyValue(DataSheetPropertyUtility.GetRowArrayProp(property),
                (t) =>
                {
                    TableSizeField.SetValueWithoutNotify((uint)DataSheetPropertyUtility.GetRowCount(property)-1);
                });
            root.Add(TableSizeField);

            // 編集終了（Enterキー or フォーカス外れ）
            TableSizeField.RegisterCallback<FocusOutEvent>(evt =>
            {
                DataSheetPropertyUtility.ResizeRow(property, TableSizeField.value+1);
            });

            //追加ボタン
            var addButton = new VisualElement();
            addButton.style.backgroundImage = plusTex;
            addButton.AddToClassList("unity-button");
            addButton.RegisterCallback<MouseDownEvent>((t) =>
            {
                if (t.button == 0)
                {
                    DataSheetPropertyUtility.AddRow(property);
                    SetupRows(property, _multiColumnListView);
                    _multiColumnListView.RefreshItems();                               
                }
            });
            root.Add( addButton);
            
            //削除ボタン
            var removeButton = new VisualElement();
            removeButton.style.backgroundImage = minusTex;
            removeButton.SetEnabled( DataSheetPropertyUtility.GetRowCount(property) > 1 );
            removeButton.AddToClassList("unity-button");
            removeButton.RegisterCallback<MouseDownEvent>((t) =>
            {
                if (t.button == 0)
                {
                    var removeIndexList = _multiColumnListView.selectedIndices.Any()
                        ? _multiColumnListView.selectedIndices.ToArray()
                        : new int[] { rowIDList.Count - 1 };
                    RemoveRow(property,removeIndexList);
                }
            });
            removeButton.TrackPropertyValue(DataSheetPropertyUtility.GetRowArrayProp(property),
                (t) =>
                {
                    removeButton.SetEnabled( DataSheetPropertyUtility.GetRowCount(property) > 1 );
                });            
            root.Add( removeButton);
            
            return root;
        }
        
        private void RemoveRow( SerializedProperty property ,params int[] indexs)
        {
            var removes = indexs
                .Where( i => i > 0 )
                .Where(i => DataSheetPropertyUtility.RowObsolete(property, i).boolValue)
                .OrderByDescending(i => i)
                .ToArray();
            if (removes.Length > 0)
            {
                DataSheetPropertyUtility.RemoveRows(property, removes);

                foreach (var i in removes)
                {
                    rowIDList.RemoveAt(i);
                }

                //これをやらないと変更が通知されないことがある？
                _multiColumnListView.itemsSource = rowIDList;
                _multiColumnListView.ClearSelection();
                _multiColumnListView.RefreshItems();
//                            _multiColumnListView.Rebuild();
            }
            else
            {
                foreach (var index in indexs.Where( i => i > 0))
                {
                    DataSheetPropertyUtility.RowObsolete(property, index).boolValue = true;
                    property.serializedObject.ApplyModifiedProperties();
                    _multiColumnListView.RefreshItems();
                }
            }
        }
    }
}