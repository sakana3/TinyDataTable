using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TinyDataTable.Editor
{
    public enum AttributeUsage
    {
        Drawer ,
        Additional                
    }

    public abstract class AttributeAdapterBase<T> : AttributeAdapterBase where T : Attribute
    {
        /// <summary> TargetType </summary>
        protected sealed override Type TargetType => typeof(T);
        /// <summary> FromAttribute </summary>
        protected abstract void FromAttribute(T attribute);
        /// <summary> FromAttribute </summary>
        protected sealed override void FromAttribute(Attribute attribute)
        {
            FromAttribute(attribute as T);
        }
    }


    public abstract class AttributeAdapterBase
    {
        /// <summary> Default Enable </summary>
        public virtual AttributeUsage AttributeUsage => AttributeUsage.Drawer;
        
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

        /// <summary> TargetType </summary>        
        protected abstract Type TargetType { get; }
        
        /// <summary> To Code </summary>
        public abstract string[] ToAttributeArgs();

        /// <summary> Initialize From Code </summary>
        protected abstract void FromAttribute(Attribute attribute);

        /// <summary> Create UI </summary>
        protected abstract void CreateUI(VisualElement root);
        
        /// <summary> Attribute Value tupple </summary>
        public (Type type,Attribute attribute,string text) AttributeValue
        {
            get
            {
                var attr = this.GetType().GetCustomAttribute<AttributeOptionAttribute>();
                if (attr != null)
                {
                    return (TargetType,attr,MakeAttributeCode());
                }
                return (null,null, null);
            }
        }
        
        /// <summary> AMake Attribute Code</summary>
        private string MakeAttributeCode()
        {
            var attr = this.GetType().GetCustomAttribute<AttributeOptionAttribute>();
            if (attr != null)
            {
                var args = ToAttributeArgs();
                if (args.Length > 0)
                {
                    return $"{TargetType}({string.Join(",", args)})";
                }
                else
                {
                    return $"[{TargetType}]";
                }
            }
            return string.Empty;
        }

        
        /// <summary> Makr root UI </summary>
        internal void InitializeFormFiledInfo(FieldInfo fieldInfo)
        {
            if (fieldInfo != null)
            {
                var attr = fieldInfo.Attributes
                    .FirstOrDefault(t => t.Type == AttributeValue.type);
                if (attr.Type != null)
                {
                    IsEnable = true;
                    FromAttribute(attr.Item2);
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
        internal VisualElement CreateRootUI( bool hasEnableHeader )
        {
            var root = new VisualElement();

            root.style.backgroundColor = new Color(0.2f,0.2f,0.2f,0.5f);

            if (hasEnableHeader)
            {
                var toggle = new Toggle(Title);
                toggle.style.flexDirection = FlexDirection.RowReverse;
                toggle.style.justifyContent = Justify.FlexEnd; // 左寄せにする
                var toggleInput = toggle.Q(className: "unity-toggle__input");
                if (toggleInput != null)
                {
                    toggleInput.style.flexGrow = 0;      // 勝手に広がらないように固定
                    toggleInput.style.marginRight = 6f;  // 文字との間に少し隙間をあける
                }
                toggle.value = IsEnable;
                toggle.RegisterValueChangedCallback((evt) => OnChangeEnable(evt.newValue));
                root.Add(toggle);
            }

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
        protected static string[] ToArgsStrings(params object[] args)
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