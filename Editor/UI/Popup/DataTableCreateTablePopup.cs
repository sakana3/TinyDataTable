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
        
        public DataTableCreateTablePopup(string name)
        {
            namespaceName = name;
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
                if (confirmButton.enabledSelf)
                {
                    confirmButton.Focus();
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
            var className = textField.value;
            
            if (string.IsNullOrEmpty(className))
            {
                confirmButton.SetEnabled( false);           
                infoBox.text = "Input table name.";
                infoBox.style.display = DisplayStyle.Flex;
                infoBox.messageType = HelpBoxMessageType.Warning;
            }
            else if (SerializableUtility.CheckCSharpSafeName(className) is false )
            {
                infoBox.text = "Invalid table name.";
                confirmButton.SetEnabled( false);
                infoBox.style.display = DisplayStyle.Flex;
                infoBox.messageType = HelpBoxMessageType.Error;
            }
            else if (SerializableUtility.CheckExistClass( namespaceName,className+"Record") )
            {
                infoBox.text = "This name is already used.";
                confirmButton.SetEnabled( false);              
                infoBox.style.display = DisplayStyle.Flex;
                infoBox.messageType = HelpBoxMessageType.Error;
            }
            else if( Regex.IsMatch(className, @"[^\u0000-\u007F]") )
            {
                infoBox.text = "The name can only use half-width characters.";
                confirmButton.SetEnabled( false);
                infoBox.style.display = DisplayStyle.Flex;
                infoBox.messageType = HelpBoxMessageType.Error;
            }
            else
            {
                infoBox.text = "Press button to confirm.";                
                infoBox.messageType = HelpBoxMessageType.Info;                
                confirmButton.SetEnabled( true);
                infoBox.style.display = DisplayStyle.Flex;
            }
        }
    }
}