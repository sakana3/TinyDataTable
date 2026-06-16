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
        public int minLines = 1;
        public int maxLines = 3;

        public override string[] ToCode() => ToArgStrings( minLines,maxLines);
        public override void FromCode( Type attributeType,  string[] code )
        {
            minLines = FromArg(code[0]) as int? ?? minLines;
            maxLines = FromArg(code[1]) as int? ?? maxLines;            
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