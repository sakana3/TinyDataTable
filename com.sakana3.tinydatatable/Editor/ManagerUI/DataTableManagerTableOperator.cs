using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    internal class DataTableManagerTableOperator : VisualElement
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
            {
                var assetGroup = new VisualElement();
                assetGroup.style.flexDirection = FlexDirection.Row;
                Add(assetGroup);
                MakeMargine(assetGroup);
                
                var assetField = new ObjectField();
                assetField.name = "Asset";
                assetField.objectType = typeof(DataTableRecordBase);
                assetField.value = asset;
                assetField.SetEnabled(false);
                assetGroup.Add(assetField);

                MonoScript script = MonoScript.FromScriptableObject(asset);
                var classField = new ObjectField();
//                classField.objectType = typeof(DataTableRecordBase);
                classField.value = script;
                classField.SetEnabled(false);
                assetGroup.Add(classField);
            }

#if USE_ADDRESSABLES            
            var addressableElement = new AddressableElement(asset);
            MakeMargine(addressableElement);
            Add(addressableElement);
#endif
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

            exportButton = new Button()
            {
                text = "Rebuild",
            };
            exportButton.iconImage = Background.FromTexture2D(BuildIcon);
            exportButton.clicked += () =>
            {
//                SaveDataTable.CheckNeedEnsureAddressable(asset,false);

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