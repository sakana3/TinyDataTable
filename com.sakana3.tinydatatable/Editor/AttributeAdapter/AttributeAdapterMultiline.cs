using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption( typeof(MultilineAttribute),typeof(string) )]
    public class AttributeAdapterMultiline : AttributeAdapterBase
    {
        private int Lines { set; get; } = 3;

        public override bool DefaultEnable => false;

        public override string[] ToCode() => ToArgsStrings(Lines);
        public override void FromCode( Type attributeType,  string[] code )
        {
            Lines = FromArgv<int>(code[0],Lines);            
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var minField = new IntegerField("Lines") { value = Lines };
            minField.RegisterValueChangedCallback( evt => Lines = evt.newValue );
            root.Add( minField);
        }
    }
}