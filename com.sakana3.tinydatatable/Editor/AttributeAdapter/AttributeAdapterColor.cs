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
        private bool ShowAlpha { get; set; } = true;
        private bool HDR { get; set; } = true;
        
        public override string[] ToCode() => ToArgsStrings( ShowAlpha,HDR );
        
        public override void FromCode( Type attributeType,  string[] code )
        {
            ShowAlpha = FromArgv<bool>(code[0],ShowAlpha);
            HDR = FromArgv<bool>(code[1],HDR);
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var showAlphaField = new Toggle("Show Alpha") { value = ShowAlpha };
            showAlphaField.RegisterValueChangedCallback( evt => ShowAlpha = evt.newValue );
            root.Add( showAlphaField);

            var hdrField = new Toggle("HDR") { value = HDR };
            hdrField.RegisterValueChangedCallback( evt => HDR = evt.newValue );
            root.Add( hdrField);
        }
    }    
}