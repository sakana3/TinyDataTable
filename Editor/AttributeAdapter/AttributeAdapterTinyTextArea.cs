using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(string) )]
    public class AttributeAdapterTinyTextArea : AttributeAdapterBase<TinyTextAreaAttribute>
    {
        private float fontSize = 12;
        private bool noWrap = false;

        protected override void FromAttribute(TinyTextAreaAttribute attribute)
        {
            fontSize = attribute.FontSize;
            noWrap = attribute.NoWrap;
        }
        
        public override string[] ToAttributeArgs() => ToArgsStrings( fontSize,noWrap);

        protected override void CreateUI(VisualElement root)
        {
            var fontSizeField = new FloatField("Font Size"){value = fontSize};
            fontSizeField.RegisterValueChangedCallback( evt => fontSize = evt.newValue );
            root.Add( fontSizeField);
            
            var noWrapField = new Toggle("No Wrap"){value = noWrap};
            noWrapField.RegisterValueChangedCallback( evt => noWrap = evt.newValue );
            root.Add( noWrapField);
        }
    }
}