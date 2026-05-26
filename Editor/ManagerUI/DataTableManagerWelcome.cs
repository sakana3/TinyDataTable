using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    public class DataTableManagerWelcome : VisualElement
    {
        private DataTableManager manager = null;

        private TextField textInput1;
        private TextField textInput2;
        private DataTableManager.DataType dataType = DataTableManager.DataType.Resources;
        
        public Action<DataTableManager> OnClickStart;
        
        public DataTableManagerWelcome(DataTableManager manager)
        {
            this.manager = manager;
            IntroPage();

        }

        public void IntroPage()
        {
            var root = this;
            
            root.style.alignSelf = Align.Stretch;
            root.style.fontSize = 16;
//            root.style.whiteSpace = WhiteSpace.Normal;
            root.style.marginTop = 16;
            root.style.marginLeft = 16;
            root.style.marginRight = 16;
            
            var label = new Label("Welcome to the Tiny Data Table");
            label.style.alignSelf = Align.Center;
            label.style.fontSize = 24;
            root.Add(label);
            AddSpace(root, 20);
            AddLabel(root, "First. Please make changes if there are any necessary items.");
            AddSpace(root, 20);
            
            var toggleGroup = new ToggleButtonGroup("Data type");
            var button1 = new Button() { text = "Resources" };
            button1.clicked += () => { dataType = DataTableManager.DataType.Resources; };
            toggleGroup.Add(button1);
            var button2 = new Button() { text = "Addresable" };
            button2.clicked += () => { dataType = DataTableManager.DataType.Addresable; };
            toggleGroup.Add(button2);
            toggleGroup.value = new ToggleButtonGroupState(1,2);
            root.Add(toggleGroup);
            
            textInput1 = AddText(root,"Root Path", "TinyDataTable");
            textInput2 = AddText(root,"ID namespace", "ID");

            AddSpace(root,16);
            
            AddLabel(root, "If you don't mind, please press the Start button.");

            AddSpace(root,16);

            var button = new Button();
            button.text = "Start";
            button.clicked += onClickStart;
            root.Add(button);
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
        
        public TextField AddText(VisualElement root,string label, string defalutText)
        {
            var textInput = new TextField(label);
            textInput.value = defalutText;
            root.Add(textInput);

            return textInput; 
        }

        private void onClickStart()
        {
            var dataTableManager = ScriptableObject.CreateInstance<DataTableManager>();
            var rootPath = textInput1.value;
            dataTableManager.Initialize(dataType,rootPath,textInput2.value);

             MakeDirectory(rootPath, "Editor");
//            MakeDirectory(rootPath, "Tables");
//            MakeDirectory(rootPath, "Resources");
//            MakeDirectory(rootPath, "Scripts\\ID");

            UnityEditor.AssetDatabase.CreateAsset(dataTableManager, $"Assets/{rootPath}\\Editor\\TinyDataTableManager.asset");
            UnityEditor.AssetDatabase.SaveAssets();            
            
            OnClickStart?.Invoke(dataTableManager);
        }



        private void MakeDirectory(string rootPath, string subPath)
        {
            var directory = $"Assets/{rootPath}\\{subPath}";
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            
                // Unity側にフォルダが作成されたことを認識させる
                AssetDatabase.Refresh();
            }
        }        
    }
}