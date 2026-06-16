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
        public int lines = 3;

        public override bool DefaultEnable => false;

        public override string[] ToCode() => ToArgStrings(lines);
        public override void FromCode( Type attributeType,  string[] code )
        {
        }
        protected override void CreateUI(VisualElement root)
        {
            var minField = new IntegerField("Lines") { value = lines };
            minField.RegisterValueChangedCallback( evt => lines = evt.newValue );
            root.Add( minField);
        }
    }
}