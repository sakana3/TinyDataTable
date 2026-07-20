using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace TinyDataTable.Editor
{
    internal class AddressableElement : VisualElement
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

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_target, out var guid, out var localId))
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

                var flagLabel = new AddressableGroupLabel(_target);
                root.Add(flagLabel);
            }
        }

        private class AddressableGroupLabel : FlagLabel
        {
            private Object _target;
            
            public AddressableGroupLabel(Object target)
            {
                _target = target;
                Initialize();
                
                RegisterCallback<AttachToPanelEvent>((evt) =>
                {
                    AddressableAssetSettings.OnModificationGlobal += OnAddressablesModified;
                });
                RegisterCallback<DetachFromPanelEvent>((evt) =>
                {
                    AddressableAssetSettings.OnModificationGlobal -= OnAddressablesModified;
                });
                    
                var settings = AddressableAssetSettingsDefaultObject.Settings;                
                AddButton.clicked += () =>
                {
                    AddressableElement.PopupAddressableGroup(target,AddButton.worldBound);
                };
            }
            
            private void OnAddressablesModified(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent modificationEvent, object data)
            {
                ResetLables();
            }   
            
            protected override IEnumerable<string> EnumrateLables()
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null || _target == null)
                {
                    yield break;
                }

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_target, out var guid, out var localId))
                {
                    yield break;
                }

                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    yield break;
                }

                foreach (var label in entry.labels)
                {
                    yield return label;
                }
            }
        }
        
        protected static void PopupAddressableGroup( Object target , Rect contentRect )
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || target == null) return;

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out var localId)) return;

            var entry = settings.FindAssetEntry(guid);
            if (entry == null) return;
            
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
    }
}

#endif
