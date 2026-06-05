using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    public partial class DataSheetField
    {
        private ContextualMenuManipulator MakeColumHeaderManipulator(
            VisualElement element,
            int index)
        {
            var manipulator = new ContextualMenuManipulator((evt) =>
            {
                // メニュー項目を追加
/*
                    evt.menu.AppendAction(
                        "Add Field",
                        (action) =>
                        {
                            OpenAddFieldPopup(property,index, action.eventInfo.mousePosition);
                        });
*/
                /*
                    evt.menu.AppendAction(
                        
                        "Obsolete Field",
                        (action) =>
                        {
                            var obsolete = DataSheetPropertyUtility.ColumObsolete(property, index);
                            obsolete.boolValue = !obsolete.boolValue;
                            property.serializedObject.ApplyModifiedProperties();
                            element.style.backgroundColor =  obsolete.boolValue?_obsoleteColor:new StyleColor();
                            _multiColumnListView.RefreshItems();
                        },
                        (action) =>
                        {
                            var obsolete = DataSheetPropertyUtility.ColumObsolete(property, index);
                            return obsolete.boolValue ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                        });]
*/                        
/*                
                    evt.menu.AppendAction(
                        "Remove Field",
                        (action) =>
                        {
                            DataSheetPropertyUtility.RemoveColum(property, index);
//                            fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(_dataSheetProperty);   
                            
                            //消したはずのセルのコールバックが走ってしまうので一旦nullにする
                            foreach (var column in _multiColumnListView.columns)
                            {
                                column.bindCell = null;
                                column.makeCell = null;
                                column.makeHeader = null;
                            }
                            
                            SetupColumns(property, _multiColumnListView);
                            _multiColumnListView.RefreshItems();
                            _multiColumnListView.Rebuild();
                        },
                        (action) =>
                        {
                            var obsolete = DataSheetPropertyUtility.ColumObsolete(property, index);
                            return obsolete.boolValue ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                        });
                    evt.menu.AppendSeparator();
*/                    
            });  
            return manipulator;
        }

        
        private ContextualMenuManipulator MakeRowIndexManipulator(
            VisualElement element,
            int index)
        {
            var manipulator = new ContextualMenuManipulator((evt) =>
            {
                if (IsStructureMode)
                {

                    // メニュー項目を追加
                    evt.menu.AppendAction(
                        "Add Record",
                        (action) =>
                        {
                            _recordPropertyUtil.AddRow(index + 1);
                            SetupRows( _multiColumnListView);
                            _multiColumnListView.RefreshItems();
                        });
                    if (index > 0)
                    {
                   
                        evt.menu.AppendAction(
                            "Obsolete Record",
                            (action) =>
                            {
                                if (_multiColumnListView.selectedIndices.Contains(index))
                                {
                                    var isObsolete = _recordPropertyUtil.RowHeaders[index].obsolete;
                                    foreach (var idx in _multiColumnListView.selectedIndices.Where(i => i > 0))
                                    {
                                        _recordPropertyUtil.SetRowObsolete(idx ,!isObsolete);
                                    }
                                    _multiColumnListView.RefreshItems();
                                }
                            },
                            (action) =>
                            {
                                return _recordPropertyUtil.RowHeaders[index].obsolete
                                    ? DropdownMenuAction.Status.Checked
                                    : DropdownMenuAction.Status.Normal;
                            });

                        evt.menu.AppendAction(
                            "Remove Record",
                            (action) =>
                            {
                                if (_multiColumnListView.selectedIndices.Contains(index))
                                {
                                    _recordPropertyUtil.RemoveRows(_multiColumnListView.selectedIndices);
                                    SetupRows(_multiColumnListView);
                                    _multiColumnListView.itemsSource = rowIDList;
                                    _multiColumnListView.ClearSelection();
                                    _multiColumnListView.Rebuild();                                    
                                }
                            },
                            (action) =>
                            {
                                return _recordPropertyUtil.RowHeaders[index].obsolete
                                    ? DropdownMenuAction.Status.Normal
                                    : DropdownMenuAction.Status.Disabled;
                            });
                    
                    }
                    evt.menu.AppendSeparator();                
                }
            });
            return manipulator;
        }

        private ContextualMenuManipulator MakeAddFieldManipulator(VisualElement element)
        {
            var manipulator = new ContextualMenuManipulator((evt) =>
            {
                evt.menu.AppendAction("Chage Field Order", (action) =>
                {
                    var rect = element.worldBound;

                    var nameList = _recordPropertyUtil.FieldInfos.Select(f=>f.name).ToList();
                    
                    DataSheetFieldOrderPopup.Show(nameList,OrderChange,rect);
                });
                evt.menu.AppendSeparator();
            });
            return manipulator;                
        }

        private void OrderChange(List<string> newOrder)
        {
            var newList = newOrder.Select(n => _recordPropertyUtil.FieldInfos.FirstOrDefault(f => f.name == n)).ToList();
            SaveDataTable.SaveScript(targetAsset, newList);
        }

        
        private void OpenAddFieldPopup( int index ,Rect activatorRect)
        {
            DataTableAddPropertyPopup.Show(
                activatorRect,
                _recordPropertyUtil.FieldInfos.Select(f=>f.name).ToList(),
                _recordPropertyUtil.RowHeaders.Select(s=>s.name).ToList(), 
                DataTablePropertyUtil.ReservWords,
                Manager?.Assemblies,
                (type, fieldName, isArray,description) =>
                {
                    if (string.IsNullOrEmpty(fieldName) is false)
                    {
                        var fields = DataTableRecordUtility.GetSerializableFields(targetAsset.RecordType);                        
                        var field = new RecordFieldInfo()
                        {
                            name = fieldName,
                            description = description,
                            id = 0,
                            obsolete = false,
                            type = type
                        };
                        fields.Add(field);
                        
                        SaveDataTable.SaveScript(targetAsset, fields);

#if false
                        var sheet = DataSheetPropertyUtility.GetValue(property) as DataSheet;
                        if (sheet != null)
                        {
                            sheet.AddField(type, fieldName, isArray);
                            property.serializedObject.Update();
                            property.serializedObject.ApplyModifiedProperties();
                            
//                            fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(_dataSheetProperty);   
                            
                            var newIndex = sheet.record.Header.fieldInfos.Length;
                            var newColumn = MakePropertyColumn(property, sheet.record.Header.fieldInfos.Length-1);
                            _multiColumnListView.columns.Insert( _multiColumnListView.columns.Count - 1, newColumn );
                            _multiColumnListView.RefreshItems();
                        }
#endif
                    }
                });
        }
    }
}