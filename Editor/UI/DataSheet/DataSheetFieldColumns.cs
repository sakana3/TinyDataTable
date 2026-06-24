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

        
        /// <summary>
        /// 列をセットアップする
        /// </summary>
        /// <param name="property"></param>
        /// <param name="listView"></param>
        private void SetupColumns()
        {
            //Make Columns
            columnIDList.Clear();
            //Clearを呼ぶと何故かコールバックが呼ばれるので潰してから呼ぶ。どう考えてもバグ
            foreach (var column in _multiColumnListView.columns)
            {
                column.bindCell = null;
                column.makeCell = null;
            }
            _multiColumnListView.columns.Clear();       

            var indexColumn = MakeIndexColumn();
            _multiColumnListView.columns.Add(indexColumn);
            
            var recordNameColumn = MakeIDNameColumn();
            recordNameColumn.stretchable = true;
            _multiColumnListView.columns.Add(recordNameColumn);

            for (int i = 0; i < _recordPropertyUtil.FieldInfos.Count; i++)
            {
                if ( IsStructureMode || _recordPropertyUtil.FieldInfos[i].Obsolete is false)
                {
                    var columProp = MakePropertyColumn(i);
                    _multiColumnListView.columns.Add(columProp);
                }
            }

            if (IsStructureMode)
            {
                var lastColumn = MakeLastColumn();
                _multiColumnListView.columns.Add(lastColumn);
            }
        }        

        
        /// <summary>
        /// Make Index Row
        /// </summary>
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
                    e.Clear();
 
                    var iRow = rowIDList[idx].index;                    
                    var label = new Label();
                    label.text =$"{iRow }";
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    label.AddManipulator( MakeRowIndexManipulator(label,iRow) );
                    label.style.flexGrow = 1.0f ;
                    e.style.backgroundColor = rowIDList[idx].isObsolete?_obsoleteColor:new StyleColor();                        

                    e.Add(label);
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
     
        /// <summary>
        /// Make Name Row
        /// </summary>        
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
            
//            LoadColumnWidths(colum , 120.0f);
            return colum;                        
        }

        /// <summary>
        /// Make Property Row
        /// </summary>
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
        
    }
}