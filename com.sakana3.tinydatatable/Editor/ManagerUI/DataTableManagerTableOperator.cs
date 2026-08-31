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
        private DataTableTree.Item item { set; get; } = null;
        private DataTableBase asset => item.tableAsset;

        private static Texture2D BuildIcon = EditorGUIUtility.IconContent("KnobCShape").image as Texture2D;

        private Button exportButton;

        private bool isDirty = false;
        
        public DataTableManagerTableOperator(DataTableManager manager, DataTableTree.Item item)
        {
            this.manager = manager;
            this.item = item;
            
            var so = new SerializedObject(asset);
            CreateGUI(so);
            CheckDirty();
            this.TrackPropertyValue(so.FindProperty( nameof(DataTableBase.EditorFlags) ), (s) => CheckDirty());
            this.TrackPropertyValue(so.FindProperty( "_headers" ), (s) => CheckDirty());
        }

        private void CheckDirty()
        {
            if (asset.CheckNameSafe() is false)
            {
                exportButton.style.backgroundColor = new StyleColor(Color.softRed);
                exportButton.enabledSelf = false;
            }
            else
            {
                isDirty = manager.CheckDirty(asset);
                exportButton.style.backgroundColor =isDirty ? new StyleColor(Color.cornflowerBlue) : StyleKeyword.Null;
                exportButton.enabledSelf = true;
            }
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
                assetField.objectType = typeof(DataTableBase);
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
            Add(propGroup);

            //Build Button
            exportButton = new Button()
            {
                text = "Build",
            };
            exportButton.iconImage = Background.FromTexture2D(BuildIcon);
            exportButton.style.borderTopLeftRadius = 8f;
            exportButton.style.borderTopRightRadius = 8f;
            exportButton.style.borderBottomRightRadius = 8f;
            exportButton.style.borderBottomLeftRadius = 8f;
            exportButton.clicked += () =>
            {
                SaveDataTable.SaveScript(asset);
            };
            exportButton.style.backgroundColor = isDirty ?
                new StyleColor(Color.cornflowerBlue) :
                StyleKeyword.Null;
            
            propGroup.Add(exportButton);

            //Flag
            var prop = so.FindProperty(nameof(DataTableBase.EditorFlags));
            var editorFlagProp = new EnumFlagLabel(prop);
            editorFlagProp.style.height =　EditorGUIUtility.singleLineHeight;
            editorFlagProp.style.flexGrow = 1;
            propGroup.Add(editorFlagProp);
        }


        public bool OnChange(DataTableTree.Item item)
        {
            if ( this.item != item)
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