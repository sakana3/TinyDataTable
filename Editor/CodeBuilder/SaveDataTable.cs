using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Compilation;
    
namespace TinyDataTable.Editor
{
    
    public static class SaveDataTable
    {
        private const string KeyIsGenerating = "TinyDataTable_IsGenerating";
        private const string KeyCompilError = "TinyDataTable_CompilError";
        private const string KeyScriptFullPath = "TinyDataTable_Script_FilePath";        
        private const string KeyAssetFilePath = "TinyDataTable_Asset_FilePath";
        private const string KeyBackuoCode = "TinyDataTable_BackupCode";


        private static (string scriptName, string namespaceName, string assetpath,string fullPath, string address,string assetPath ) MakeInfo(
            DataTableRecordBase dataTableAsset ,
            string newClassName ,
            string newNamespace,
            string scriptOutputPath )            
        {
            var scriptName = String.Empty;
            var fullPath = String.Empty;
            var namespaceName = string.Empty;
            string assetpath = null;
            string address = null;

            var resourcePath = GetResourcePath(dataTableAsset);
            if (resourcePath != null)
            {
                //リソースフォルダ以下に配置されているなら
                assetpath = resourcePath;
            }
            else
            {
                address = GetAddressFromObject(dataTableAsset);
            }

            var fileName = $"{newClassName}Record.cs";
            fullPath = Path.Combine(scriptOutputPath, fileName);
            scriptName = newClassName;
            namespaceName = newNamespace;

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(dataTableAsset);

            return (scriptName, namespaceName, assetpath,fullPath, address,assetPath);
        }
        
        /// <summary>
        /// スクリプトが変更されているか確認する
        /// スクリプトの再生成と比較をするので結構重い
        /// </summary>
        public static bool CheckScriptModified(DataTableRecordBase recordAsset)
        {
            var script = MonoScript.FromScriptableObject(recordAsset);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
            
            var info = MakeInfo(
                recordAsset, recordAsset.BaseName, recordAsset.IdentifierType.Namespace, scriptDir);

            if (File.Exists(info.fullPath) is false)
            {
                return true;
            }
            
            List<RecordFieldInfo> fileds = new();
            if (recordAsset.RecordType != null)
            {
                fileds = RecordFieldInfo.FieldsFromType(recordAsset.RecordType);
            }
            
            var code = TinyDataTable.Editor.ExportDataSheetToCSharp.Export(
                recordAsset,
                fileds,
                info.scriptName,
                info.namespaceName,
                info.assetpath,
                info.address,
                info.assetPath
            );

            using (StreamReader reader = new StreamReader(info.fullPath, System.Text.Encoding.UTF8))
            {
                const int bufferSize = 4096; // 4KBの文字バッファ
                char[] buffer = new char[bufferSize];
                int textIndex = 0;
                int charsRead;

                while ((charsRead = reader.Read(buffer, 0, bufferSize)) > 0)
                {
                    if (textIndex + charsRead > code.Length)
                        return true;

                    ReadOnlySpan<char> fileSpan = buffer.AsSpan(0, charsRead);
                    ReadOnlySpan<char> textSpan = code.AsSpan(textIndex, charsRead);

                    if (!fileSpan.SequenceEqual(textSpan))
                    {
                        return true;
                    }

                    textIndex += charsRead;
                }

                return textIndex != code.Length;
            }
        }




        public static bool CreateNewScript(
            string newClassName ,
            string newNamespace,
            string scriptPath,
            string assetPath)
        {

            var assetName = Path.Combine(assetPath, $"{newClassName}Record.asset");
            var code = TinyDataTable.Editor.ExportDataSheetToCSharp.Export(
                null,
                new List<RecordFieldInfo>(),
                newClassName,
                newNamespace,
                null,
                null,//Path.Combine(assetPath, $"{newClassName}.asset"),
                assetName
            );

            var fullPath = Path.Combine(scriptPath, $"{newClassName}Record.cs");
            SaveScript(fullPath, code);

            // アセットデータベースを更新してUnityに認識させる
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            // セッションにデータを保存
            SessionState.SetBool(KeyIsGenerating, true);
            SessionState.SetString(KeyScriptFullPath, fullPath);
            SessionState.SetString(KeyAssetFilePath, assetName);
            //コンパイラーが走ってないなら直接呼び出す
            if (EditorApplication.isCompiling is false)
            {
                OnCompileFinished();
            }
            else
            {
                CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
            }

            return true;            
        }
        
        public static bool SaveScript(DataTableRecordBase dataTableAsset , IList<RecordFieldInfo> fields = null)
        {
            var script = MonoScript.FromScriptableObject(dataTableAsset);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
            
            var info = MakeInfo(
                dataTableAsset, dataTableAsset.BaseName, dataTableAsset.IdentifierType.Namespace, scriptDir);
       

            if (fields == null)
            {
                if (dataTableAsset.RecordType != null)
                {
                    fields = RecordFieldInfo.FieldsFromType(dataTableAsset.RecordType);
                }
                else
                {
                    fields = new List<RecordFieldInfo>();
                }
            }
            var code = TinyDataTable.Editor.ExportDataSheetToCSharp.Export(
                dataTableAsset,
                fields,
                info.scriptName,
                info.namespaceName,
                info.assetpath,
                info.address,
                info.assetPath
            );

            if (File.Exists(info.fullPath))
            {
                var original = File.ReadAllText(info.fullPath);
                SessionState.SetString(KeyBackuoCode, original);
            }
            SaveScript(info.fullPath, code);

            // アセットデータベースを更新してUnityに認識させる
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            // セッションにデータを保存
            SessionState.SetBool(KeyIsGenerating, true);
            SessionState.SetString(KeyScriptFullPath, info.fullPath);
            SessionState.SetString(KeyAssetFilePath, AssetDatabase.GetAssetPath(dataTableAsset));
            
            //コンパイラーが走ってないなら直接呼び出す
            if (EditorApplication.isCompiling is false)
            {
                OnCompileFinished();
            }
            else
            {
                CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
            }

            return true;
        }

        private static void OnCompilationFinished(string assemblyPath, UnityEditor.Compilation.CompilerMessage[] messages)
        {
            // エラーメッセージが含まれているかチェック
            bool hasErrors = messages.Any(m => m.type == CompilerMessageType.Error);

            SessionState.SetBool(KeyCompilError, hasErrors);
            CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
            if (hasErrors)
            {
                var originalCode = SessionState.GetString(KeyBackuoCode, string.Empty);
                Debug.Log($"Compilation failed. {assemblyPath}");
                Debug.Log(originalCode);
                if (string.IsNullOrEmpty(originalCode) is false)
                {
                    Debug.LogWarning("Reverting changes because a compilation error occurred.");
                    
                    string scriptPath = SessionState.GetString(KeyScriptFullPath, string.Empty);                    
//                    SaveScript(scriptPath, originalCode);
//                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                }
            }
        }
        
        
        /// <summary>
        /// コンパイルが終了した後InitializeOnLoadMethodで呼び出される
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnCompileFinished()
        {
            string scriptPath = SessionState.GetString(KeyScriptFullPath, string.Empty);
            string assetPath = SessionState.GetString(KeyAssetFilePath, string.Empty);
            bool hasErrors = SessionState.GetBool(KeyCompilError, false);
            bool isGenerating = SessionState.GetBool(KeyIsGenerating, false);

            SessionState.EraseBool(KeyIsGenerating);
            SessionState.EraseBool(KeyCompilError);
            SessionState.EraseString(KeyScriptFullPath);
            SessionState.EraseString(KeyAssetFilePath);
            SessionState.EraseString(KeyBackuoCode);

            if (!isGenerating ) return;            

            if ( hasErrors)
            {
                return;
            }

            if ( string.IsNullOrEmpty(assetPath) is false && File.Exists(assetPath) is false)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                var dataTableAsset = ScriptableObject.CreateInstance(script.GetClass()) as DataTableRecordBase;
                var assetDir = System.IO.Path.GetDirectoryName(assetPath);
                if (!System.IO.Directory.Exists(assetDir))
                {
                    System.IO.Directory.CreateDirectory(assetDir);
                    AssetDatabase.Refresh();
                }

                AssetDatabase.CreateAsset(dataTableAsset, assetPath);
                EditorGUIUtility.SetIconForObject(dataTableAsset, DataTableManagerTreeView.ItemIcon as Texture2D);
                EditorGUIUtility.SetIconForObject(script, DataTableManagerTreeView.ItemIcon as Texture2D);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                
                DataTableManager.OnCreateAsset(dataTableAsset);
                
//            SaveDataTable.CheckNeedEnsureAddressable(dataTableAsset,true);
            }
        }
        
        private static string GetAddressFromObject(UnityEngine.Object obj)
        {
            if (obj == null) return null;

            string path = AssetDatabase.GetAssetPath(obj);
            string guid = AssetDatabase.AssetPathToGUID(path);

            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return null;
            
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                return entry.address; // アドレスを返す
            }

            return null; // Addressableではない
        }
               
        /// <summary>
        /// Resources以下のパスを取得する。Resources以下にないならnull
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string GetResourcePath(UnityEngine.Object obj)
        {
            if (obj == null) return null;

            // アセットのパスを取得 (例: "Assets/MyGame/Resources/Characters/Hero.prefab")
            string fullPath = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(fullPath)) return null;

            // "Resources/" が含まれているかチェック
            int index = fullPath.LastIndexOf("/Resources/");
        
            if (index >= 0)
            {
                // "Resources/" の後ろを切り出す
                string path = fullPath.Substring(index + "/Resources/".Length);

                // 拡張子を削除
                int extIndex = path.LastIndexOf('.');
                if (extIndex >= 0)
                {
                    path = path.Substring(0, extIndex);
                }

                return path;
            }

            return null;
        }


        
        public static void SaveScript(string fullPath, string content)
        {
//            var fileName = Path.GetFileName(fullPath);
            var filePath = Path.GetDirectoryName(fullPath);            
            
            if (!AssetDatabase.IsValidFolder(filePath))
            {
                CreateFolderRecursively(filePath);
            }
            
            // ファイル書き込み
            File.WriteAllText(fullPath, content);
        }

        // フォルダを再帰的に作成するヘルパー
        private static void CreateFolderRecursively(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;


            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFolderRecursively(parent);
            }

            string parentFolder = Path.GetDirectoryName(path);
            string newFolder = Path.GetFileName(path);
            var t = AssetDatabase.CreateFolder(parentFolder, newFolder);
        }
        

        public static bool CheckNeedEnsureAddressable(UnityEngine.Object asset , bool setAddressIfNotEntry)
        {
            if (asset == null) return false;

            //Resources以下にあるならは登録しない
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (assetPath.Contains("/Resources/"))
            {
                return false;
            }
            
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            
            // エントリを検索
            var entry = settings.FindAssetEntry(guid);            
            
            // まだ登録されていなければ登録
            if (setAddressIfNotEntry && entry == null)
            {
                // デフォルトグループに登録
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            
                // アドレスを設定
                entry.SetAddress(path);
            
                EditorUtility.SetDirty(settings);
            }
            
            return entry == null;
        }        
        
    }
}