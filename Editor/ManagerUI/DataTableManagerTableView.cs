using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

namespace TinyDataTable.Editor
{
    public class DataTableManagerTableView : VisualElement
    {
        private DataTableManager Manager = null;
        private DataTableAsset asset;
        private bool IsStructureMode { set; get; } = false;

        public DataTableManagerTableView(DataTableManager manager,DataTableAsset asset,bool isStructureMode)
        {
            this.Manager = manager;
            this.IsStructureMode = isStructureMode;
            this.asset = asset;
            viewDataKey = $"DataTableManagerTableView_{asset.name}";
            CreateGUI();
        }

        private void CreateGUI()
        {
            var property = new SerializedObject(asset)
                .FindProperty("dataSheet");
            
            var sheet = new DataSheetField(Manager,property,IsStructureMode);            

            Add( sheet);            
        }
    }
}