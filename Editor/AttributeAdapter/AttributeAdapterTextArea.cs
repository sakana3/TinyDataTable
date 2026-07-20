using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(string) )]
    public class AttributeAdapterTextArea : AttributeAdapterBase<TextAreaAttribute>
    {
        private int minLines = 1;
        private int maxLines = 3;
 
        protected override void FromAttribute(TextAreaAttribute attribute)
        {
            minLines = attribute.maxLines;
            maxLines = attribute.maxLines;
        }

        public override string[] ToAttributeArgs() => ToArgsStrings( minLines,maxLines);

        protected override void CreateUI(VisualElement root)
        {
            var minField = new IntegerField("Min Lines") { value = minLines };
            minField.RegisterValueChangedCallback( evt => minLines = evt.newValue );
            root.Add( minField);
            
            var maxField = new IntegerField("Max Lines"){value = maxLines};
            maxField.RegisterValueChangedCallback( evt => maxLines = evt.newValue );
            root.Add( maxField);
        }
    }
}