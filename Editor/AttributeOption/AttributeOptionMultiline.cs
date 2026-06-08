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
        public override string Name => "Multi Line";
        public override float Height => 0;
        
        public override string ToCode()
        {
            return "Multiline";
        }
        public override void FromCode( string code )
        {
        }
        protected override VisualElement CreateUI()
        {
            return null;
        }
    }
}