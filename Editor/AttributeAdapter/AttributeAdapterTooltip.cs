using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

#if false

namespace TinyDataTable.Editor
{
    [AttributeOption( typeof(TooltipAttribute) )]
    public class AttributeAdapterTooltip : AttributeAdapterBase
    {
        public override AttributeType AttributeType => AttributeType.Additional;
        
        private string tooltip = "";

        public override string[] ToCode() => ToArgsStrings( tooltip);

        public override void FromCode( Type attributeType,  string[] code )
        {
            tooltip = FromArgv<string>(code[0],tooltip);
        }
        
        protected override void CreateUI(VisualElement root)
        {
            var tooltipField = new TextField("Tool Tip"){value = tooltip};
            tooltipField.RegisterValueChangedCallback( evt => tooltip = evt.newValue );
            root.Add( tooltipField);
        }
    }
}
#endif