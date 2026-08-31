using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace TinyDataTable.Editor
{
    internal class DataTableCreateTablePopup : PopupWindowContent
    {
        //Set the window size
        public override Vector2 GetWindowSize() => new Vector2(256, 80);

        private TextField textField;
        private HelpBox infoBox;

        private Button confirmButton;
        private string namespaceName;

        public Action<string> clickCreateButton;
        private DataTableManager tableManager;
        
        public DataTableCreateTablePopup( DataTableManager tableManager )
        {
            namespaceName = tableManager.DefaultNamespace;
            this.tableManager = tableManager;
        }
        
        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;

            textField = new TextField("Table Name");
         
            textField.RegisterValueChangedCallback(evt => OnClassNameChangeCallback(textField,evt));            
            // 少し遅延させてフォーカス
            textField.schedule.Execute(() => 
            {
                textField.Focus();
            }).StartingIn(50); // 50ms後くらい         

            textField.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                if (CheckName(textField.value).enable)
                {
                    clickCreateButton?.Invoke(textField.value);         
                    editorWindow.Close();
                }
            });
      
            
            root.Add( textField);

            infoBox = new HelpBox("Input table name.", HelpBoxMessageType.Warning);
            root.Add( infoBox);
            
            confirmButton = new Button()
            {
                text = "Create",
            };
            confirmButton.clicked += () =>
            {
                clickCreateButton?.Invoke(textField.value);
                editorWindow.Close();
            };
            root.Add(confirmButton);
            
            confirmButton.SetEnabled( false);
        }
        
        public override void OnClose()
        {
        
        }

        private void OnClassNameChangeCallback(TextField textField, ChangeEvent<string> evt)
        {
            var check = CheckName(textField.value);
            confirmButton.SetEnabled( check.enable);           
            infoBox.text = check.messageText;
            infoBox.style.display = DisplayStyle.Flex;
            infoBox.messageType = check.messageType;            
        }
        
        private (bool enable,string messageText,HelpBoxMessageType messageType) CheckName(string className)
        {
            bool enable = false;
            var messageText = string.Empty;
            var messageType = HelpBoxMessageType.None;
            
            if (string.IsNullOrEmpty(className))
            {
                messageText = "Input table name.";
                messageType = HelpBoxMessageType.Warning;
            }
            else if (SerializableUtility.CheckCSharpSafeName(className) is false )
            {
                messageText = "Invalid table name.";
                messageType = HelpBoxMessageType.Error;
            }
            else if (SerializableUtility.CheckExistClass( namespaceName,className) ||
                     tableManager.Tree.Nodes.Any( t => t.IsFolder is false && t.Name == className )
            ){
                messageText = "This name is already used.";
                messageType = HelpBoxMessageType.Error;
            }
            else if( Regex.IsMatch(className, @"[^\u0000-\u007F]") )
            {
                messageText = "The name can only use half-width characters.";
                messageType = HelpBoxMessageType.Error;
            }
            else
            {
                messageText = "Press button to confirm.";                
                messageType = HelpBoxMessageType.Info;                
                enable = true;
            }

            return (enable, messageText, messageType);
        }
    }
}