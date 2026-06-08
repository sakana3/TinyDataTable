using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(UnityEngine.MinAttribute), typeof(int), typeof(float) )]
    public class AttributeOptionMin : AttributeOptionBase
    {
        public float Min { get; set; } = 0;

        public override string Title => "Min";
        
        public override string[] ToCode() => ToArgStrings( Min );
        
        public override void FromCode( Type attributeType,  string[] code )
        {
            Min = FromArg(code[0]) as float? ?? Min;
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var minField = new FloatField("Min") { value = Min };
            minField.RegisterValueChangedCallback( evt => Min = evt.newValue );
            root.Add( minField);
        }
    }    
}