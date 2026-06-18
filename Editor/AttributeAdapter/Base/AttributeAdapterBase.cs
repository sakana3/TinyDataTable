using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Globalization;

namespace TinyDataTable.Editor
{
    public abstract class AttributeAdapterBase
    {
        public abstract string[] ToCode();
        public abstract void FromCode( Type attributeType,  string[] code );
        protected abstract void CreateUI(VisualElement root);
        public virtual bool DefaultEnable => false;
        
        public bool IsEnable { set; get; } = false;
        VisualElement optionUI;
        public virtual string Title
        {
            get
            {
                var val = AttributeValue;
                var title = val.type.Name;
                if (title.EndsWith("Attribute"))
                {
                    title = title.Substring(0, title.Length - "Attribute".Length);
                }
                
                return ObjectNames.NicifyVariableName(title);;
            }
        }

        public (Type type,string[] args) AttributeValue
        {
            get
            {
                var attr = this.GetType().GetCustomAttribute<AttributeOptionAttribute>();
                if (attr != null)
                {
                    return (attr.AttributeType,ToCode());
                }
                return (null, null);
            }
        }
        
        

        internal void FormFiledInfo(FieldInfo fieldInfo)
        {
            if (fieldInfo != null)
            {
                var attr = fieldInfo.CustomAttributes
                    .FirstOrDefault(t => t.Type == AttributeValue.type);
                if (attr.Type != null)
                {
                    IsEnable = true;
                    FromCode( attr.Type, attr.args);
                }
                else
                {
                    IsEnable = false;
                }
            }
            else
            {
                IsEnable = DefaultEnable;
            }
        }
        
        public VisualElement MakeUI()
        {
            var root = new VisualElement();

            root.style.backgroundColor = new Color(0.2f,0.2f,0.2f,0.5f);
            
            var toggle = new Toggle(Title);
            toggle.value = IsEnable;
            toggle.RegisterValueChangedCallback((evt) => OnChangeEnable(evt.newValue));
    
            root.Add( toggle );
            optionUI = new VisualElement();
            root.Add(optionUI);
            CreateUI(optionUI);
            OnChangeEnable(IsEnable);

            return root;            
        }

        
        protected static string[] ToArgStrings(params object[] args)
        {
            return args.Select( a => ToArgString(a)).ToArray();
        }
        
        protected static string ToArgString( object args)
        {
            return SerializableUtility.ToArgString(args);
        }

        protected static object FromArg(string argStr)
        {
            return SerializableUtility.FromArg(argStr);
        }
        
        
        private void OnChangeEnable(bool isEnable)
        {
            IsEnable = isEnable;
            if( optionUI != null)
            {
                optionUI.enabledSelf = isEnable;
                optionUI.style.display = isEnable ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        public static List<AttributeAdapterBase> FindAttributeOptions( Type type , IReadOnlyCollection<Type> baseTypes)
        {
            var types = TypeCache.GetTypesDerivedFrom<AttributeAdapterBase>()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(typeof(AttributeOptionAttribute), true))
                .Where(t => t.GetCustomAttribute<AttributeOptionAttribute>().HasType(type) );

            
            var options = types
                .Select( t => Activator.CreateInstance(t))
                .OfType<AttributeAdapterBase>()
                .OrderBy(t => t.Title)
                .ToList();
            
            if (baseTypes != null)
            {
                foreach (var baseType in baseTypes.Reverse())
                {
                    options = options
                        .OrderByDescending(f => f.AttributeValue.type == baseType)
                        .ToList();                    
                }                
            }
            
            return options;
        }
    }
}