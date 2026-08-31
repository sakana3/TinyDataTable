using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal class DataTableConstructTableView : VisualElement
    {
        private DataTableManager _manager = null;
        private string _treeName;
        
        public DataTableConstructTableView(DataTableManager manager,string treeName)
        {
            _manager = manager;
            _treeName = treeName;
            IntroPage();
        }

        public void IntroPage()
        {
            var root = this;

            root.style.alignSelf = Align.Stretch;
            root.style.fontSize = 16;
//            root.style.whiteSpace = WhiteSpace.Normal;
            root.style.marginTop = 16;
            root.style.marginLeft = 100;
            root.style.marginRight = 100;
            root.style.flexGrow = 1;

            var label = new Label("Construct Data Table");
            label.style.alignSelf = Align.Center;
            root.Add(label);
            
            AddSpace(root, 20);
            
            var button = new Button();
            button.text = "Construct";
            button.clicked += () =>
            {
                button.enabledSelf = false;
                onClickCreate();
            };
            root.Add(button);            
        }
        private void onClickCreate()
        {
            SaveDataTable.CreateNewScript(
                _treeName,
                _manager.DefaultNamespace,
                _manager.ScriptsPath,
                _manager.TablesPath);
        }
        
        public void AddLabel(VisualElement root, string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            root.Add(label);            
        }
        
        public void AddSpace(VisualElement root, float height )
        {
            var space = new VisualElement() { };
            space.style.height = height;
            root.Add(space);     
        }                
        

    }
}