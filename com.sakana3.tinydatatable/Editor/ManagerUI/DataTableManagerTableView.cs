using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

namespace TinyDataTable.Editor
{
    internal class DataTableManagerTableView : VisualElement
    {
        private DataTableManager Manager = null;
        public DataTableTree.Item item { set; get; } = null;
        private DataTableBase asset => item.tableAsset;
        private bool IsStructureMode { set; get; } = false;

        public DataTableManagerTableView(DataTableManager manager,DataTableTree.Item item,bool isStructureMode)
        {
            this.Manager = manager;
            this.IsStructureMode = isStructureMode;
            this.item = item;
            viewDataKey = $"DataTableManagerTableView_{asset.name}";
            CreateGUI();
        }

        private void CreateGUI()
        {
            var sheet = new DataSheetField(Manager,item, IsStructureMode);
            Add( sheet);            
        }
    }
}