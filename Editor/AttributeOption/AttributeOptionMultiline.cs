using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    [AttributeOption( typeof(MultilineAttribute),typeof(string) )]
    public class AttributeOptionMultiline : AttributeOptionBase
    {
        public override string Title => "Multi Line";
        
        public override string[] ToCode() => Array.Empty<string>();
        public override void FromCode( Type attributeType,  string[] code )
        {
        }
        protected override VisualElement CreateUI()
        {
            return null;
        }
    }
}