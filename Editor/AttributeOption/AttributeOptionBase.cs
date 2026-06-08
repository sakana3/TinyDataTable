using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TinyDataTable.Editor
{
    public abstract class AttributeOptionBase
    {
        public abstract string Name { get; }
        public abstract string ToCode();
        public abstract void FromCode(string code);
        public abstract float Height { get; }
        protected abstract VisualElement CreateUI();
        
        public bool IsEnable { set; get; } = false;
        VisualElement optionUI;
        public VisualElement MakeUI()
        {
            var root = new VisualElement();
            
            var toggle = new Toggle(Name);
            toggle.value = IsEnable;
            toggle.RegisterValueChangedCallback((evt) => OnChangeEnable(evt.newValue));
    
            root.Add( toggle );
            optionUI = CreateUI();
            if (optionUI != null)
            {
                root.Add(optionUI);
                optionUI.enabledSelf = IsEnable;
            }

            return root;            
        }

        private void OnChangeEnable(bool isEnable)
        {
            IsEnable = isEnable;
            if( optionUI != null)
            {
                optionUI.enabledSelf = isEnable;
            }
        }
        
        public static AttributeOptionBase[] FindAttributeOptions( Type type)
        {
            var types = TypeCache.GetTypesDerivedFrom<AttributeOptionBase>()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(typeof(AttributeOptionAttribute), true))
                .Where(t => t.GetCustomAttribute<AttributeOptionAttribute>().HasType(type) );
            
            var options = types
                .Select( t => Activator.CreateInstance(t))
                .OfType<AttributeOptionBase>()
                .OrderBy(t => t.Name)
                .ToArray();
            
            return options;
        }
    }
}