using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    public class AddressableElement : VisualElement
    {
        private UnityEngine.Object _target;
        public AddressableElement(UnityEngine.Object target)
        {
            _target = target;
            UpdateEditor();
        }

        private void UpdateEditor()
        {
            this.Clear();
            UpdateAddressableUI( _target );
        }

        public void UpdateAddressableUI(UnityEngine.Object _target)
        {
            VisualElement root = this;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                root.Add(new HelpBox("Addressable Asset Settings not found.",
                    HelpBoxMessageType.Warning));
                return;
            }

            string guid;
            long localId;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_target, out guid, out localId))
            {
                return;
            }

            var entry = settings.FindAssetEntry(guid);

            VisualElement addressableRoot = new VisualElement();
            addressableRoot.style.flexDirection = FlexDirection.Row;
            root.Add(addressableRoot);
            
            var toggle = new Toggle()
            {
                value = entry != null
            };
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var newEntry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                    UpdateEditor();
                }
                else
                {
                    settings.RemoveAssetEntry(guid);
                    UpdateEditor();
                }
            });
            addressableRoot.Add(toggle);
            var label = new Label("Addressable");
            addressableRoot.Add(label);

            if (entry != null)
            {
                var addressField = new TextField()
                {
                    value = entry.address
                };
                addressField.RegisterValueChangedCallback(evt => { entry.SetAddress(evt.newValue); });
                addressableRoot.Add(addressField);

                // Group Selection
                var groups = settings.groups;
                var groupNames = new System.Collections.Generic.List<string>();
                var currentGroupIndex = 0;
                for (int i = 0; i < groups.Count; i++)
                {
                    groupNames.Add(groups[i].Name);
                    if (groups[i] == entry.parentGroup)
                    {
                        currentGroupIndex = i;
                    }
                }

                var groupField = new PopupField<string>("Group", groupNames, currentGroupIndex);
                groupField.RegisterValueChangedCallback(evt =>
                {
                    var targetGroup = groups[groupNames.IndexOf(evt.newValue)];
                    settings.MoveEntry(entry, targetGroup);
                });
                root.Add(groupField);

                // Label Selection
                var labelHeader = new Label("Labels");
                labelHeader.style.marginTop = 5;
                labelHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
//                root.Add(labelHeader);

                var labelContainer = new IMGUIContainer(() =>
                {
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    if (settings == null || _target == null) return;

                    string guid;
                    long localId;
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_target, out guid, out localId)) return;

                    var entry = settings.FindAssetEntry(guid);
                    if (entry == null) return;

                    // Draw labels in a pill-shaped button that opens a popup
                    var labelCount = entry.labels.Count;
                    var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                    var contentRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                        rect.width - EditorGUIUtility.labelWidth, rect.height);

                    string labelString = labelCount == 0 ? "None" : string.Join(", ", entry.labels);
                    if (GUI.Button(contentRect, new GUIContent(labelString), EditorStyles.popup))
                    {
                        var entries = new List<AddressableAssetEntry> { entry };
                        var allLabels = settings.GetLabels();
                        var labelNameToFreq = new Dictionary<string, int>();
                        foreach (var l in allLabels)
                        {
                            if (entry.labels.Contains(l)) labelNameToFreq[l] = 1;
                        }

                        // Use Reflection to call LabelMaskPopupContent if it's internal
                        var type = typeof(AddressableAssetSettings).Assembly.GetType(
                            "UnityEditor.AddressableAssets.GUI.LabelMaskPopupContent");
                        if (type != null)
                        {
                            var constructor = type.GetConstructor(
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance, null,
                                new[]
                                {
                                    typeof(Rect), typeof(AddressableAssetSettings), typeof(List<AddressableAssetEntry>),
                                    typeof(Dictionary<string, int>)
                                }, null);
                            if (constructor != null)
                            {
                                var windowContent = constructor.Invoke(new object[]
                                    { contentRect, settings, entries, labelNameToFreq }) as PopupWindowContent;
                                UnityEditor.PopupWindow.Show(contentRect, windowContent);
                            }
                        }
                    }
                });
                root.Add(labelContainer);
            }
        }
    }
}