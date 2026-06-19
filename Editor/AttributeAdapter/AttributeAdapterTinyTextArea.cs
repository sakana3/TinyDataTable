using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption( typeof(TinyTextAreaAttribute),typeof(string) )]
    public class AttributeAdapterTinyTextArea : AttributeAdapterBase
    {
        public int minLines = 1;
        public int maxLines = 10;
        public float fontSize = 18;

        public override string[] ToCode() => ToArgStrings( minLines,maxLines,fontSize);
        public override void FromCode( Type attributeType,  string[] code )
        {
            if (code.Length > 0)
            {
                minLines = FromArg(code[0]) as int? ?? minLines;
            }
            else
            {
                minLines = 1;
            }
            if (code.Length > 1)
            {
                maxLines = FromArg(code[1]) as int? ?? maxLines;
            }
            else
            {
                maxLines = 10;
            }
            if (code.Length > 2)
            {
                fontSize = FromArg(code[2]) as float? ?? maxLines;
            }
            else
            {
                fontSize = 12;
            }
        }
        protected override void CreateUI(VisualElement root)
        {
            var minField = new IntegerField("Min Lines") { value = minLines };
            minField.RegisterValueChangedCallback( evt => minLines = evt.newValue );
            root.Add( minField);
            
            var maxField = new IntegerField("Max Lines"){value = maxLines};
            maxField.RegisterValueChangedCallback( evt => maxLines = evt.newValue );
            root.Add( maxField);
            
            var fontSizeField = new FloatField("Font Size"){value = fontSize};
            fontSizeField.RegisterValueChangedCallback( evt => fontSize = evt.newValue );
            root.Add( fontSizeField);
            
        }
    }
}