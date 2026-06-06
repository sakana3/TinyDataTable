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
        /// <summary>
        /// 幅が変更されたらEditorPrefsに保存する
        /// </summary>        
        private void RegisterColumnResizeCallbacks( VisualElement headerVisualElement , string columnName )
        {
            if (headerVisualElement != null)
            {
                headerVisualElement.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    EditorPrefs.SetFloat( $"{targetAsset.BaseName}.{columnName}" ,evt.newRect.width);
                });
            }
        }        
        
        /// <summary>
        /// EditorPrefsから横幅を読み込んで適用する
        /// </summary>
        private void LoadColumnWidths(Column column)
        {
            string key = $"{targetAsset.BaseName}.{column.name}";
            
            // 保存された値があれば取得（なければデフォルト値 100f）
            if (EditorPrefs.HasKey(key))
            {
                column.width = EditorPrefs.GetFloat(key);
            }
        }        
    }
}