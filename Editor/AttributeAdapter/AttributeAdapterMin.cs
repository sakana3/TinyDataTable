using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(int), typeof(float) )]
    public class AttributeAdapterMin : AttributeAdapterBase<MinAttribute>
    {
        private float Min { get; set; } = 0;

        protected override void FromAttribute(MinAttribute attribute)
        {
            Min = attribute.min;
        }

        public override string[] ToAttributeArgs() => ToArgsStrings( Min );
        
        protected override void CreateUI(VisualElement root)
        {
            var minField = new FloatField("Min") { value = Min };
            minField.RegisterValueChangedCallback( evt => Min = evt.newValue );
            root.Add( minField);
        }
    }    
}