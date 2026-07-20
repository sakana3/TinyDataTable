using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption(typeof(Color) )]
    public class AttributeAdapterColorUsage : AttributeAdapterBase<UnityEngine.ColorUsageAttribute>
    {
        private bool ShowAlpha { get; set; } = true;
        private bool HDR { get; set; } = true;
        
        protected override void FromAttribute(ColorUsageAttribute attribute)
        {
            ShowAlpha = attribute.showAlpha;
            HDR = attribute.hdr;
        }

        public override string[] ToAttributeArgs() => ToArgsStrings( ShowAlpha,HDR );
        
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