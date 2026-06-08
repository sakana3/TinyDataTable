using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(RangeAttribute), typeof(int) )]
    public class AttributeOptionRange : AttributeOptionBase
    {
        public override string Name => "Range";
        public override float Height => 0;

        public float Min { get; set; } = 0;
        public float Max { get; set; } = 100;
        
        public override string ToCode()
        {
            return $"Range({Min},{Max})";
        }
        
        public override void FromCode( string code )
        {
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