using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TinyDataTable.Editor
{
    
    /// <summary>
    /// カラムサイズの変更周りの処理
    /// </summary>
    public partial class DataSheetField
    {
        private string ColumWidthKey(Column column) => $"TinyDataTable.{targetAsset.BaseName}.{column.name}.Colum.Width";

        /// <summary>
        /// 幅が変更されたらEditorPrefsに保存する
        /// </summary>        
        private void RegisterColumnResizeCallbacks( Column column,VisualElement headerVisualElement )
        {
            if (headerVisualElement != null && column != null)
            {
                headerVisualElement.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    EditorPrefs.SetFloat( ColumWidthKey(column) ,column.width.value);
                });
            }
        }        
        
        /// <summary>
        /// EditorPrefsから横幅を読み込んで適用する
        /// </summary>
        private void LoadColumnWidths(Column column , float defaultWidth )
        {
            string key = ColumWidthKey(column);
            
            // 保存された値があれば取得（なければデフォルト値 100f）
            if (EditorPrefs.HasKey(key))
            {
                column.width = EditorPrefs.GetFloat(key);
            }
            else
            {
                column.width = defaultWidth;
            }
        }        
    }
}