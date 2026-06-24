using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption( typeof(TextAreaAttribute),typeof(string) )]
    public class AttributeAdapterTextArea : AttributeAdapterBase
    {
        private int minLines = 1;
        private int maxLines = 3;
 
        public override string[] ToCode() => ToArgsStrings( minLines,maxLines);
        public override void FromCode( Type attributeType,  string[] code )
        {
            minLines = FromArgv<int>(code[0],minLines);
            maxLines = FromArgv<int>(code[1],maxLines);
        }
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