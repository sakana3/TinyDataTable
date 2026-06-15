using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(UnityEngine.ColorUsageAttribute), typeof(Color) )]
    public class AttributeAdapterColorUsage : AttributeAdapterBase
    {
        public bool showAlpha { get; set; } = true;
        public bool hdr { get; set; } = true;

        public override string Title => "ColorUsage";
        
        public override string[] ToCode() => ToArgStrings( showAlpha,hdr );
        
        public override void FromCode( Type attributeType,  string[] code )
        {
            showAlpha = FromArg(code[0]) as bool? ?? showAlpha;
            hdr = FromArg(code[0]) as bool? ?? hdr;
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var showAlphaField = new Toggle("Show Alpha") { value = showAlpha };
            showAlphaField.RegisterValueChangedCallback( evt => showAlpha = evt.newValue );
            root.Add( showAlphaField);

            var hdrField = new Toggle("HDR") { value = hdr };
            hdrField.RegisterValueChangedCallback( evt => hdr = evt.newValue );
            root.Add( hdrField);
        }
    }    
}