using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(UnityEngine.RangeAttribute), typeof(int), typeof(float) )]
    public class AttributeOptionRange : AttributeOptionBase
    {
        public float Min { get; set; } = 0;
        public float Max { get; set; } = 100;

        public override string Title => "Range";
        
        public override string[] ToCode() => ToArgs( Min , Max );
        
        public override void FromCode( Type attributeType,  string[] code )
        {
            Min = FromArg(code[0]) as float? ?? Min;
            Max = FromArg(code[1]) as float? ?? Max;
        }
        
        protected override VisualElement CreateUI()
        {
            var root = new VisualElement();
            
            var minField = new FloatField("Min");
            minField.value = Min;
            minField.RegisterValueChangedCallback( evt => Min = evt.newValue );
            root.Add( minField);
            
            var maxField = new FloatField("Max");
            maxField.value = Max;
            maxField.RegisterValueChangedCallback( evt => Max = evt.newValue );
            root.Add( maxField);
            
            return root;
        }
    }    
}