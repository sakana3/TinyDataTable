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
        /// <summary> Default Enable </summary>
        public virtual bool DefaultEnable => false;
        
        /// <summary> Enable </summary>
        public bool IsEnable { set; get; } = false;

        /// <summary> UI </summary>
        private VisualElement optionUI;

        /// <summary> Title </summary>
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
        
        /// <summary>
        /// To Code
        /// </summary>
        public abstract string[] ToCode();
        
        /// <summary>
        /// From Code
        /// </summary>
        public abstract void FromCode( Type attributeType,  string[] code );

        /// <summary>
        /// Create UI
        /// </summary>
        protected abstract void CreateUI(VisualElement root);
        
        /// <summary>
        /// Attribute Value tupple
        /// </summary>
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
        
        
        /// <summary>
        /// Makr root UI
        /// </summary>
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
        
        /// <summary>
        /// Makr root UI
        /// </summary>
        internal VisualElement CreateRootUI()
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
        
        /// <summary>
        /// Args to strings
        /// </summary>
        /// <param name="args">args</param>
        /// <returns></returns>
        protected static string[] ToArgStrings(params object[] args)
        {
            return args.Select( a => ToArgString(a)).ToArray();
        }
        
        /// <summary>
        /// Argv to string
        /// </summary>
        protected static string ToArgString( object argv)
        {
            return SerializableUtility.ToArgString(argv);
        }

        /// <summary>
        /// String to argv
        /// </summary>
        protected static object FromArg(string argvStr)
        {
            return SerializableUtility.FromArg(argvStr);
        }
        
        /// <summary>
        /// OnChangeEnable
        /// </summary>
        private void OnChangeEnable(bool isEnable)
        {
            IsEnable = isEnable;
            if( optionUI != null)
            {
                optionUI.enabledSelf = isEnable;
                optionUI.style.display = isEnable ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Find Attribute Options
        /// </summary>
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