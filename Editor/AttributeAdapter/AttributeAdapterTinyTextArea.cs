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
        private float fontSize = 12;
        private bool noWrap = false;

        public override string[] ToCode() => ToArgsStrings( fontSize,noWrap);

        public override void FromCode( Type attributeType,  string[] code )
        {
            fontSize = (code.Length > 1) ? FromArgv<float>(code[0],fontSize) : fontSize;
            noWrap = (code.Length > 2) ? FromArgv<bool>(code[1],noWrap) : noWrap;
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var fontSizeField = new FloatField("Font Size"){value = fontSize};
            fontSizeField.RegisterValueChangedCallback( evt => fontSize = evt.newValue );
            root.Add( fontSizeField);
            
            var noWrapField = new Toggle("No Wrap"){value = noWrap};
            noWrapField.RegisterValueChangedCallback( evt => noWrap = evt.newValue );
            root.Add( noWrapField);
        }
    }
}