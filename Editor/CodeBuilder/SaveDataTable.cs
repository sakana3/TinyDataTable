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
        private const string CompilError = "TinyDataTable_CompilError";
        private const string ScriptFilePath = "TinyDataTableScript_FilePath";        
        private const string AssetFilePath = "TinyDataTableAsset_FilePath";



        private static (string scriptName, string namespaceName, string assetpath,string fullPath, string address ) MakeInfo(
            DataTableAsset dataTableAsset ,
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
            
            //未インポートだった場合、新規名をつける
            if (dataTableAsset.classScript != null)
            {
                var script = dataTableAsset.classScript;
                scriptName = script.GetClass().Name;
                namespaceName = script.GetClass().Namespace;
                fullPath = AssetDatabase.GetAssetPath(script);
                address = GetAddressFromObject(dataTableAsset);                       
            }
            else
            {
                var fileName = $"{newClassName}.cs";
                fullPath = Path.Combine(scriptOutputPath, fileName);
                scriptName = newClassName;
                namespaceName = newNamespace;
            }            
            
            return (scriptName, namespaceName, assetpath,fullPath, address);
        }
        
        /// <summary>
        /// スクリプトが変更されているか確認する
        /// スクリプトの再生成と比較をするので結構重い
        /// </summary>
        public static bool CheckScriptModified(
            DataTableAsset dataTableAsset,
            string newClassName,
            string newNamespace,
            string scriptOutputPath)
        {
            if (dataTableAsset.classScript == null)
            {
                Debug.Log(1);
                return false;
            }
            
            
            var info = MakeInfo(
                dataTableAsset, newClassName, newNamespace, scriptOutputPath);

            if (File.Exists(info.fullPath) is false)
            {
                return true;
            }
            
            var code = TinyDataTable.Editor.ExportDataSheetToCSharp.Export(
                dataTableAsset,
                info.scriptName,
                info.namespaceName,
                info.assetpath,
                info.address
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
        
        public static bool SaveScript(
            DataTableAsset dataTableAsset ,
            string newClassName ,
            string newNamespace,
            string scriptOutputPath )
        {
            var info = MakeInfo(
                dataTableAsset, newClassName, newNamespace, scriptOutputPath);
       
            var code = TinyDataTable.Editor.ExportDataSheetToCSharp.Export(
                dataTableAsset,
                info.scriptName,
                info.namespaceName,
                info.assetpath,
                info.address
            );            

            SaveScript(info.fullPath, code);

            // アセットデータベースを更新してUnityに認識させる
            AssetDatabase.Refresh();
            // セッションにデータを保存
            SessionState.SetBool(KeyIsGenerating, true);
            SessionState.SetString(ScriptFilePath, info.fullPath);
            SessionState.SetString(AssetFilePath, AssetDatabase.GetAssetPath(dataTableAsset));
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

            SessionState.SetBool(CompilError, hasErrors);                
            CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;            
        }
        
        [InitializeOnLoadMethod]
        private static void OnCompileFinished()
        {
            if (!SessionState.GetBool(KeyIsGenerating, false)) return;
            
            string scriptPath = SessionState.GetString(ScriptFilePath, string.Empty);
            string assetPath = SessionState.GetString(AssetFilePath, string.Empty);

            SessionState.EraseBool(KeyIsGenerating);
            SessionState.EraseBool(CompilError);
            SessionState.EraseString(ScriptFilePath);
            SessionState.EraseString(AssetFilePath);            
            
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            DataTableAsset asset = AssetDatabase.LoadAssetAtPath<DataTableAsset>(assetPath);
            if (script != null && asset != null)
            {
                asset.classScript = script;
                var recordTypeProp = script.GetClass().GetProperty("RecordType" , BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.NonPublic);
                if (recordTypeProp != null)
                {
                    asset.Bind( script.GetClass() , recordTypeProp.GetValue(null) as Type);
                }
                else
                {
                    asset.Bind( script.GetClass() , null);
                }
                var serializedObject =  new SerializedObject(asset);
                
                EditorUtility.SetDirty(asset);
                serializedObject.Update();
            }
            else
            {
                if (script == null)
                {
                    Debug.LogError($"Failed to load script. {scriptPath} {script}");
                }
                if (asset == null)
                {
                    Debug.LogError($"Failed to load asset.  {assetPath} {asset}");
                }
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