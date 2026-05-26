using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    [CustomPropertyDrawer(typeof(DataSheet))]
    public class DataSheetPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var sheet = new DataSheetField(null,property,true);

            root.Add(sheet);
            
            return root;
        }
    }
}