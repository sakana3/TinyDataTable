using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(UnityEngine.RangeAttribute), typeof(int), typeof(float) )]
    public class AttributeAdapterRange : AttributeAdapterBase
    {
        public float Min { get; set; } = 0;
        public float Max { get; set; } = 100;

        public override string Title => "Range";
        
        public override string[] ToCode() => ToArgStrings( Min , Max );
        
        public override void FromCode( Type attributeType,  string[] code )
        {
            Min = FromArg(code[0]) as float? ?? Min;
            Max = FromArg(code[1]) as float? ?? Max;
        }
        
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