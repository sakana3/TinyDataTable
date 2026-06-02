using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    public class DataTableManagerTableOperator : VisualElement
    {
        private DataTableManager manager = null;
        private DataTableRecordBase asset { set; get; } = null;

        private static Texture2D BuildIcon = EditorGUIUtility.IconContent("KnobCShape").image as Texture2D;

        private Button exportButton;

        private bool isDirty = false;
        
        public DataTableManagerTableOperator(DataTableManager manager, DataTableRecordBase asset)
        {
            this.manager = manager;
            this.asset = asset;
            
            isDirty = manager.CheckDirty(asset);            
            
            var so = new SerializedObject(asset);
            CreateGUI(so);

            this.TrackSerializedObjectValue(so, (s) =>
            {
                isDirty = manager.CheckDirty(asset);
                exportButton.style.backgroundColor =isDirty ? new StyleColor(Color.cornflowerBlue) : StyleKeyword.Null;
            });
        }

        private void CreateGUI(SerializedObject so)
        {
            var assetField = new ObjectField();
            assetField.objectType = typeof(DataTableRecordBase);
            assetField.value = asset;
            assetField.SetEnabled(false);
            MakeMargine(assetField);            
            Add(assetField);          
            
            var addressableElement = new AddressableElement(asset);
            MakeMargine(addressableElement);
            Add(addressableElement);
            
            var propGroup = new VisualElement();
            propGroup.style.flexDirection = FlexDirection.Row;
            MakeMargine(propGroup);            
            Add(propGroup);
            
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.Bind(so);
            propGroup.Add(root);

            var buttonGroup = new VisualElement();
            buttonGroup.style.flexDirection = FlexDirection.Row;
            root.Add(buttonGroup);
            
            var initializeOnLoadToggle = new Toggle();
            initializeOnLoadToggle.text = "InitializeOnLoad";
            initializeOnLoadToggle.BindProperty(so.FindProperty("_initializeOnLoad"));
            buttonGroup.Add(initializeOnLoadToggle);

            var initializeOnLoadEditorToggle = new Toggle();
            initializeOnLoadEditorToggle.text = "InitializeOnLoadEditor";
            initializeOnLoadEditorToggle.BindProperty(so.FindProperty("_initializeOnLoadEditor"));
            buttonGroup.Add(initializeOnLoadEditorToggle);

            var obsoleteField = new Toggle();
            obsoleteField.text = "Obsolete";
            obsoleteField.BindProperty(so.FindProperty("_isObsolete"));
            buttonGroup.Add(obsoleteField);

#if false            
            var scriptProp = so.FindProperty("classScript");
            if (scriptProp.objectReferenceValue != null)
            {
                var classGroup = new VisualElement();
                classGroup.style.flexDirection = FlexDirection.Row;
                root.Add(classGroup);                
                
                var typeNameField = new PropertyField(so.FindProperty("classType"));
                typeNameField.SetEnabled(false);
                typeNameField.style.flexGrow = 1;
                classGroup.Add(typeNameField);            
                
                var classField = new ObjectField();
                classField.BindProperty(scriptProp);
                classField.style.flexGrow = 1;
                classField.SetEnabled(false);
                classGroup.Add(classField);
            }
#endif            
            exportButton = new Button()
            {
                text = "Rebuild",
            };
            exportButton.iconImage = Background.FromTexture2D(BuildIcon);
            exportButton.clicked += () =>
            {
                SaveDataTable.CheckNeedEnsureAddressable(asset,false);

                SaveDataTable.SaveScript(asset);
            };

            exportButton.style.backgroundColor = isDirty ? new StyleColor(Color.cornflowerBlue) : StyleKeyword.Null;
            
            propGroup.Add(exportButton);
        }


        public bool OnChange(DataTableRecordBase target)
        {
            if ( this.asset != target)
            {
/*
                isDirty = manager.CheckDirty(this.asset);
                if (isDirty)
                {
                    var select = UnityEditor.EditorUtility.DisplayDialog(
                        "Confirm", "Changes require a rebuild. Do you want to build?",
                        "Yes,Build now", "Maybe Later");
                    if (select)
                    {

                        var scriptPath = AssetDatabase.GetAssetPath(asset.classScript);
                        var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);

                        SaveDataTable.SaveScript(
                            asset,
                            asset.classScript.GetClass().Name,
                            manager.DefaultNamespace,
                            scriptDir);
                    }
                }
*/                            
            }
            return true;
        }
        
        public static void MakeMargine(VisualElement ve)
        {
            ve.style.borderBottomColor = Color.gray;
            ve.style.borderBottomWidth = 1;
            ve.style.paddingBottom = 4;
            ve.style.marginBottom = 4;            
        }
    }
}