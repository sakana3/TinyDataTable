using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(
        typeof(int),typeof(float),typeof(long),typeof(double) ,
        typeof(byte),typeof(short),typeof(ushort), typeof(uint),typeof(ulong)
    )]
    public class AttributeAdapterRange : AttributeAdapterBase<RangeAttribute>
    {
        private float Min { get; set; } = 0;
        private float Max { get; set; } = 100;
        
        protected override void FromAttribute(RangeAttribute attribute)
        {
            Min = attribute.min;
            Max = attribute.max;
        }

        public override string[] ToAttributeArgs() => ToArgsStrings( Min , Max );
        
        protected override void CreateUI(VisualElement root)
        {
            var minField = new FloatField("Min") { value = Min };
            minField.RegisterValueChangedCallback( evt => Min = evt.newValue );
            root.Add( minField);
            
            var maxField = new FloatField("Max"){ value = Max };
            maxField.RegisterValueChangedCallback( evt => Max = evt.newValue );
            root.Add( maxField);
        }
    }    
}