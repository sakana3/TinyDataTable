using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption( typeof(string) )]
    public class AttributeAdapterMultiline : AttributeAdapterBase<MultilineAttribute>
    {
        private int Lines { set; get; } = 3;

        public override bool DefaultEnable => false;

        protected override void FromAttribute(MultilineAttribute attribute)
        {
            Lines = attribute.lines;
        }        

        public override string[] ToAttributeArgs() => ToArgsStrings(Lines);
        
        protected override void CreateUI(VisualElement root)
        {
            var minField = new IntegerField("Lines") { value = Lines };
            minField.RegisterValueChangedCallback( evt => Lines = evt.newValue );
            root.Add( minField);
        }
    }
}