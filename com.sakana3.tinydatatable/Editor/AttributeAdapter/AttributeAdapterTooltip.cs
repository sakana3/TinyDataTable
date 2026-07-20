using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

#if false

namespace TinyDataTable.Editor
{
    [AttributeOption]
    public class AttributeAdapterTooltip : AttributeAdapterBase<TooltipAttribute>
    {
        private string tooltip = "";

        public override AttributeUsage AttributeUsage => AttributeUsage.Additional;

        protected override void FromAttribute(TooltipAttribute attribute)
        {
            tooltip = attribute.tooltip;
        }

        public override string[] ToAttributeArgs() => ToArgsStrings( tooltip);
        
        protected override void CreateUI(VisualElement root)
        {
            var tooltipField = new TextField(){value = tooltip};
            tooltipField.RegisterValueChangedCallback( evt => tooltip = evt.newValue );
            root.Add( tooltipField);
        }
    }
}
#endif