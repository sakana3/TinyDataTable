using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(UnityEngine.MinAttribute), typeof(int), typeof(float) )]
    public class AttributeAdapterMin : AttributeAdapterBase
    {
        private float Min { get; set; } = 0;

        public override string[] ToCode() => ToArgsStrings( Min );
        
        public override void FromCode( Type attributeType,  string[] code )
        {
            Min = FromArgv<float>(code[0],Min);
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var minField = new FloatField("Min") { value = Min };
            minField.RegisterValueChangedCallback( evt => Min = evt.newValue );
            root.Add( minField);
        }
    }    
}