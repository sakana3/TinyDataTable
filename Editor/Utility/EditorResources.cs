using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TinyDataTable.Editor
{
    internal static class EditorResources
    {
        public static Texture FolderNormalIcon { private set; get; } = EditorGUIUtility.IconContent("d_Folder Icon").image;
        public static Texture FolderEmptyIcon { private set; get; } = EditorGUIUtility.IconContent( "d_FolderEmpty Icon").image;
        public static Texture FolderOpenIcon {private set; get; } = EditorGUIUtility.IconContent("d_FolderOpened Icon").image;
        public static Texture RefreshIcon {private set; get; } = EditorGUIUtility.IconContent("Refresh").image;
        public static Texture2D FoldOnIcon = EditorGUIUtility.IconContent("d_IN_foldout_on@2x").image as Texture2D;
        public static Texture2D FoldOffIcon = EditorGUIUtility.IconContent("d_IN_foldout@2x").image as Texture2D;

        public static Texture FolderIcon( bool isExpanded , bool isEmpty )
        {
            if (isEmpty)
            {
                return FolderEmptyIcon;
            }
            if (isExpanded)
            {
                return FolderOpenIcon;
            }
            
            return FolderNormalIcon;
        }
        
        private static string GetAssetPath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DataSheetField).Assembly);
            return $"{packageInfo.assetPath}/Editor/Assets/";
        }

        public static string AssetPath { private set; get; } = GetAssetPath();

        public static Texture ItemIcon { private set; get; } = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath+"TinyDataTableIcon.png");
    }
}