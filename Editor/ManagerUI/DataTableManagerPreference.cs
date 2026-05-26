using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;

namespace TinyDataTable.Editor
{
    public class DataTableManagerPreference : VisualElement
    {
        private DataTableManager manager = null;

        public DataTableManagerPreference(DataTableManager manager)
        {
            this.manager = manager;
            CreateGUI();
        }

        private void CreateGUI()
        {
            var so = new SerializedObject(manager);
            
            //Name Space
            var namespaceTextField = new TextField("Default namespace");
            namespaceTextField.Bind(so);
            namespaceTextField.bindingPath = "DefaultNamespace";
            Add(namespaceTextField);
            
            //Root Path
            var rootPathTextField = new TextField("Root path");
            rootPathTextField.Bind(so);
            rootPathTextField.bindingPath = "RootPath";
            Add(rootPathTextField);

            //Root Path
            var TablesPathTextField = new TextField("Tables path");
            TablesPathTextField.Bind(so);
            TablesPathTextField.bindingPath = "TablesPath";
            Add(TablesPathTextField);

            //Root Path
            var ScriptsPathTextField = new TextField("Scripts Path");
            ScriptsPathTextField.Bind(so);
            ScriptsPathTextField.bindingPath = "ScriptsPath";
            Add(ScriptsPathTextField);
            
            //Assemblies
            var assembliesProp = so.FindProperty("Assemblies");
            var assembliesList = new ListView()
            {
                makeItem = () => new IMGUIContainer(),
                bindItem = (e, i) =>
                {
                    if (e is IMGUIContainer gui)
                    {
                        gui.onGUIHandler += () =>
                        {
                            Rect rect = EditorGUILayout.GetControlRect();
                            var nameProp = assembliesProp.GetArrayElementAtIndex(i);
                            if (EditorGUI.DropdownButton(rect, new GUIContent(nameProp.stringValue), FocusType.Keyboard))
                            {
                                var state = new AdvancedDropdownState();                        
                                var dropdown = new AssemblieSelectorDropdown(state, nameProp.stringValue,(assembly) => 
                                {
                                    nameProp.stringValue = assembly.GetName().Name;
                                    so.ApplyModifiedProperties();
                                });
                                dropdown.Show(rect);                        
                            }
                        };
                    }
                },
                unbindItem = (e, i) =>
                {
                    e.Clear();
                },
                showFoldoutHeader = true,
                headerTitle = "Assemblies",
                reorderable = true,
                showAddRemoveFooter = true,
            };
            assembliesList.style.flexGrow = 1;
            assembliesList.Bind(so);
            assembliesList.bindingPath = "Assemblies";
            Add(assembliesList);            
        }
    }

}