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
            SerializedProperty property,
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
                        });
                    evt.menu.AppendAction(
                        "Remove Field",
                        (action) =>
                        {
                            DataSheetPropertyUtility.RemoveColum(property, index);
                            fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(_property);   
                            
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
            });  
            return manipulator;
        }

        
        private ContextualMenuManipulator MakeRowIndexManipulator(
            SerializedProperty property,
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
                            DataSheetPropertyUtility.AddRow(property, index + 1);
                            SetupRows(property, _multiColumnListView);
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
                                    var isObsolete = !DataSheetPropertyUtility.RowObsolete(property, index).boolValue;
                                    foreach (var idx in _multiColumnListView.selectedIndices.Where(i => i > 0))
                                    {
                                        var obsolete = DataSheetPropertyUtility.RowObsolete(property, idx);
                                        obsolete.boolValue = isObsolete;
                                    }

                                    property.serializedObject.ApplyModifiedProperties();
                                    _multiColumnListView.RefreshItems();
                                }
                            },
                            (action) =>
                            {
                                var obsolete = DataSheetPropertyUtility.RowObsolete(property, index);
                                return obsolete.boolValue
                                    ? DropdownMenuAction.Status.Checked
                                    : DropdownMenuAction.Status.Normal;
                            });
                        evt.menu.AppendAction(
                            "Remove Record",
                            (action) =>
                            {
                                if (_multiColumnListView.selectedIndices.Contains(index))
                                {
                                    RemoveRow(property, _multiColumnListView.selectedIndices.ToArray());
                                    _multiColumnListView.ClearSelection();
                                }
                            },
                            (action) =>
                            {
                                var obsolete = DataSheetPropertyUtility.RowObsolete(property, index);
                                return obsolete.boolValue
                                    ? DropdownMenuAction.Status.Normal
                                    : DropdownMenuAction.Status.Disabled;
                            });
                    }
                    evt.menu.AppendSeparator();                
                }
            });
            return manipulator;
        }

        private ContextualMenuManipulator MakeAddFieldManipulator(SerializedProperty property,VisualElement element)
        {
            var manipulator = new ContextualMenuManipulator((evt) =>
            {
                evt.menu.AppendAction("Chage Field Order", (action) =>
                {
                    var rect = element.worldBound;

                    var nameList = DataSheetPropertyUtility.MakeNameList(property);
                    var orderList = DataSheetPropertyUtility.MakeFieldOrderList(property);

                    var fieldNames = orderList
                        .Select(i => nameList.fieldNames[i])
                        .ToList();
                    
                    DataSheetFieldOrderPopup.Show(fieldNames,OrderChange,rect);
                });
                evt.menu.AppendSeparator();
            });
            return manipulator;                
        }

        private void OrderChange(List<string> newOrder)
        {
            DataSheetPropertyUtility.ChangeFieldOrderList(_property, newOrder);
            fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(_property);        
            _multiColumnListView.Rebuild();
        }

        private void OpenAddFieldPopup(SerializedProperty property, int index, Vector2 mousePos)
        {
            OpenAddFieldPopup(property, index, new Rect(mousePos.x, mousePos.y, 0, 0));
        }
        
        private void OpenAddFieldPopup( SerializedProperty property, int index ,Rect activatorRect)
        {
            var names = DataSheetPropertyUtility.MakeNameList(property);
            DataTableAddPropertyPopup.Show(
                activatorRect,
                names.fieldNames,
                names.recordNames, 
                DataTablePropertyUtil.ReservWords,
                Manager?.Assemblies,
                (type, fieldName, isArray,description) =>
                {
                    if (string.IsNullOrEmpty(fieldName) is false)
                    {
                        var sheet = DataSheetPropertyUtility.GetValue(property) as DataSheet;
                        if (sheet != null)
                        {
                            sheet.AddField(type, fieldName, isArray);
                            property.serializedObject.Update();
                            property.serializedObject.ApplyModifiedProperties();
                            
                            fieldOrderList = DataSheetPropertyUtility.MakeFieldOrderList(_property);   
                            
                            var newIndex = sheet.record.Header.fieldInfos.Length;
                            var newColumn = MakePropertyColumn(property, sheet.record.Header.fieldInfos.Length-1);
                            _multiColumnListView.columns.Insert( _multiColumnListView.columns.Count - 1, newColumn );
                            _multiColumnListView.RefreshItems();
                        }
                    }
                });
        }
    }
}