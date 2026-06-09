using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    /// <summary>
    /// コンテキストメニュー周りの処理
    /// </summary>
    public partial class DataSheetField
    {
        private ContextualMenuManipulator MakeColumHeaderManipulator(
            VisualElement element,
            int index)
        {
            var manipulator = new ContextualMenuManipulator((evt) =>
            {
                // メニュー項目を追加
                    evt.menu.AppendAction(
                        "Add",
                        (action) =>
                        {
                            var rect = element.worldBound;
                            OpenAddSchemaPopup(index,rect );
                        });
                    evt.menu.AppendAction(
                        "Refactor",
                        (action) =>
                        {
                            var rect = element.worldBound;
                            OpenRefactorSchemaPopup(index,rect );
                        });

                    evt.menu.AppendAction(
                        "Obsolete",
                        (action) =>
                        {
                            var info = _recordPropertyUtil.SchemaInfos[index];
                            info.Obsolete = info.Obsolete ? false : true;
                            _recordPropertyUtil.SchemaInfos[index] = info;

                            SaveDataTable.SaveScript(targetAsset, _recordPropertyUtil.SchemaInfos);
                        },
                        (action) =>
                        {
                            var info = _recordPropertyUtil.SchemaInfos[index];
                            return info.Obsolete ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                        });
                  
                    evt.menu.AppendAction(
                        "Remove",
                        (action) =>
                        {
                            var newInfos = _recordPropertyUtil.SchemaInfos.ToList();
                            newInfos.RemoveAt(index);
                            SaveDataTable.SaveScript(targetAsset, newInfos);
                        },
                        (action) =>
                        {
                            var obsolete = _recordPropertyUtil.SchemaInfos[index].Obsolete;
                            return obsolete ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                        });
                    evt.menu.AppendSeparator();
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

        private ContextualMenuManipulator MakeAddSchemaManipulator(VisualElement element)
        {
            var manipulator = new ContextualMenuManipulator((evt) =>
            {
                evt.menu.AppendAction("Chage Order", (action) =>
                {
                    var rect = element.worldBound;

                    var nameList = _recordPropertyUtil.SchemaInfos.Select(f=>f.Name).ToList();
                    
                    DataSheetFieldOrderPopup.Show(nameList,OrderChange,rect);
                });
                evt.menu.AppendSeparator();
            });
            return manipulator;                
        }

        private void OrderChange(List<string> newOrder)
        {
            var newList = newOrder.Select(n => _recordPropertyUtil.SchemaInfos.FirstOrDefault(f => f.Name == n)).ToList();
            SaveDataTable.SaveScript(targetAsset, newList);
        }

        private void OpenAddSchemaPopup( int index ,Rect activatorRect)
        {
            DataTableCreateSchemaPopup.Show(
                activatorRect,
                targetAsset.BaseName,
                _recordPropertyUtil.SchemaInfos.Select(f=>f.Name).ToList(),
                _recordPropertyUtil.RowHeaders.Select(s=>s.name).ToList(), 
                RecordPropertyUtil.ReservWords,
                Manager?.Assemblies,
                (field) =>
                {
                    if ( field.IsValid )
                    {
                        var fields = SchemaInfo.FieldsFromType(targetAsset.RecordType);                        
                        fields.Insert(index>=0 ? index + 1 : fields.Count ,field);
                        
                        SaveDataTable.SaveScript(targetAsset, fields);
                    }
                });
        }
        
        
        private void OpenRefactorSchemaPopup( int index ,Rect activatorRect)
        {
            DataTableCreateSchemaPopup.Show(
                activatorRect,
                targetAsset.BaseName,
                _recordPropertyUtil.SchemaInfos.Select(f=>f.Name).ToList(),
                _recordPropertyUtil.RowHeaders.Select(s=>s.name).ToList(), 
                RecordPropertyUtil.ReservWords,
                Manager?.Assemblies,
                (field) =>
                {
                    if ( field.IsValid )
                    {
                        var fields = SchemaInfo.FieldsFromType(targetAsset.RecordType);                        
                        fields[index] = field;
                        SaveDataTable.SaveScript(targetAsset, fields);
                    }
                },
                SchemaInfo.FieldsFromType(targetAsset.RecordType)[index]);
        }
    }
}