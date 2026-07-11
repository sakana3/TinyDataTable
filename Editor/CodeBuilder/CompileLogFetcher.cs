#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CompileLogFetcher
{
    // 💡 Unity内部のログ判別用ビットマスクフラグ（コンパイルエラーと警告）
    private const int kScriptCompileError = 1 << 11;   // 2048
    private const int kScriptCompileWarning = 1 << 12; // 4096

    /// <summary>
    /// 取得したログデータを格納する構造クラス
    /// </summary>
    public class CompileLogData
    {
        public string Message;  // 警告・エラーの本文
        public string FilePath; // 発生したC#ファイルのパス
        public int Line;        // 発生した行数
        public bool IsError;    // trueならエラー、falseならWarning
    }

    private static MethodInfo startGettingEntries;
    private static MethodInfo endGettingEntries;
    private static MethodInfo getCount;
    private static MethodInfo getEntryInternal;
    private static Type logEntryType;
    private static Type logEntriesType;
    private static FieldInfo conditionField;
    private static FieldInfo modeField;
    private static FieldInfo fileField;
    private static FieldInfo lineField;

    private static bool ChashMethod()
    {

        if (logEntryType == null || logEntryType == null)
        {
            Assembly editorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            logEntriesType = editorAssembly.GetType("UnityEditor.LogEntries")
                             ?? editorAssembly.GetType("UnityEditorInternal.LogEntries");
            logEntryType = editorAssembly.GetType("UnityEditor.LogEntry")
                           ?? editorAssembly.GetType("UnityEditorInternal.LogEntry");

            if (logEntriesType == null || logEntryType == null)
            {
                Debug.LogError("Unityの内部ログクラスの取得に失敗しました。");
                return false;
            }

            startGettingEntries =
                logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
            endGettingEntries =
                logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
            getCount = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
            getEntryInternal = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);

            if (getCount == null || getEntryInternal == null)
            {
                Debug.LogError("LogEntriesのメソッド取得に失敗しました。");
                return false;
            }
            
            conditionField = logEntryType.GetField("condition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                       ?? logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                  ?? logEntryType.GetField("errorMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        return true;
    }
    
    /// <summary>
    /// 現在コンソールに残っているコンパイルエラーとWarningをすべて取得します
    /// </summary>
    public static List<CompileLogData> GetAllCurrentCompileLogs()
    {
        var resultList = new List<CompileLogData>();

        if (ChashMethod() is false)
        {
            return resultList;
        }

        // 3. 💡 ログの走査開始
        try
        {
            // ログ配列の読み込みロックを開始（Unityの安全対策）
            startGettingEntries?.Invoke(null, null);

            int totalLogCount = (int)getCount.Invoke(null, null);
            object logEntryInstance = Activator.CreateInstance(logEntryType);

            // コンソールに表示されているログを上から全走査
            for (int i = 0; i < totalLogCount; i++)
            {
                // i番目のログの詳細データを logEntryInstance に流し込む
                getEntryInternal.Invoke(null, new object[] { i, logEntryInstance });

                // ログの種類を表すビットマスク（mode）を取得
                int mode = (int)(modeField?.GetValue(logEntryInstance) ?? 0);
                
                // 🔥 ビット演算で「コンパイルエラー」か「コンパイル警告」かを判定
                bool isCompileError = (mode & kScriptCompileError) != 0;
                bool isCompileWarning = (mode & kScriptCompileWarning) != 0;

                // どちらでもない（通常のDebug.Logや通常の例外など）ならスキップ
                if (!isCompileError && !isCompileWarning) continue;

                // 条件に一致したデータだけをパッケージングしてリストに追加
                var logData = new CompileLogData
                {
                    Message = conditionField?.GetValue(logEntryInstance)?.ToString(),
                    FilePath = fileField?.GetValue(logEntryInstance)?.ToString(),
                    Line = (int)(lineField?.GetValue(logEntryInstance) ?? 0),
                    IsError = isCompileError
                };

                resultList.Add(logData);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            // 💡 ロックを解除して終了する（これを忘れるとUnityが不安定になります）
            endGettingEntries?.Invoke(null, null);
        }

        return resultList;
    }
}
#endif