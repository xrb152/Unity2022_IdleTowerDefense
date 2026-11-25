#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 包体优化（一次性工具）：把中文字体资产的 SDF 采样尺寸从 90 降到 56，并用同一套字符重新烘焙。
///
/// 背景：DroidSansFallback SDF 用 90pt 采样，1109 个汉字被摊进 14 张 1024x1024 的 Alpha8 图集，
/// 打包报告里单这个资源就 14.1MB（几乎占整个 data 包的一半）。SDF 是距离场，56pt 采样在
/// 30~90px 的界面文字下肉眼看不出差别，但每页能多放约 2.5 倍的字，图集页数 14 → 5~6 张。
///
/// 为什么不是「清空图集、运行时动态补字」：本工程历史上出过
/// 「m_AtlasTextures has not been assigned / character not found」的丢字事故（见 ChineseFontFixer），
/// 所以这里坚持「全量预烘焙」，只把烘焙密度调合理，保证上机一定有字。
///
/// 自动执行一次（EditorPrefs 记标记）；也可以手动：Tools/包体优化/压缩中文字体图集。
/// 失败会自动用 Backup/包体优化_* 里的 .bak 回滚。
/// </summary>
public static class TT_FontAtlasSlim
{
    private const string FontPath = "Assets/AdSDK/Font/DroidSansFallback SDF.asset";
    private const string CharsetPath = "Assets/Editor/ChineseCharset.txt";
    private const string DoneKey = "TT_FontAtlasSlim_v2_done";
    private const string ResultPath = ".workbuddy/font_slim_result.txt";

    /// <summary>目标采样字号。90 → 56 让每页容纳的字数提升约 2.5 倍</summary>
    private const float TargetPointSize = 56f;

    /// <summary>FaceInfo 里所有「像素」量级的字段，改字号时必须等比缩放，否则行距会错</summary>
    private static readonly string[] ScaledFaceFields =
    {
        "m_PointSize",
        "m_LineHeight",
        "m_AscentLine",
        "m_CapLine",
        "m_MeanLine",
        "m_Baseline",
        "m_DescentLine",
        "m_SuperscriptOffset",
        "m_SubscriptOffset",
        "m_UnderlineOffset",
        "m_UnderlineThickness",
        "m_StrikethroughOffset",
        "m_StrikethroughThickness",
        "m_TabWidth",
    };

    static TT_FontAtlasSlim()
    {
        if (EditorPrefs.GetBool(DoneKey, false))
            return;

        EditorApplication.delayCall += AutoRun;
    }

    private static void AutoRun()
    {
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += AutoRun;
            return;
        }

        // 播放模式下改字体资产很危险，跳过，等下次编译/重启编辑器或手动跑菜单
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[包体] 当前在播放模式，中文字体图集压缩已跳过；请退出播放后点菜单 Tools/包体优化/压缩中文字体图集");
            return;
        }

        Slim();
    }

    [MenuItem("Tools/包体优化/压缩中文字体图集")]
    public static void Slim()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Log("跳过：找不到 " + FontPath);
            return;
        }

        float oldPointSize = font.faceInfo.pointSize;
        int charsBefore = font.characterTable != null ? font.characterTable.Count : 0;
        int atlasBefore = font.atlasTextures != null ? font.atlasTextures.Length : 0;

        if (oldPointSize <= TargetPointSize + 0.5f)
        {
            Log($"已经是 {oldPointSize}pt / {atlasBefore} 张图集，无需处理");
            EditorPrefs.SetBool(DoneKey, true);
            return;
        }

        if (charsBefore <= 0)
        {
            Log("中止：字体里没有已烘焙的字形，重烘会变成空字体");
            return;
        }

        if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic || font.sourceFontFile == null)
        {
            Log("中止：字体不是 Dynamic 或缺少源 ttf，无法重烘");
            return;
        }

        string charset = BuildCharset();
        if (charset.Length < 100)
        {
            Log("中止：字符集太小（" + charset.Length + "），检查 " + CharsetPath);
            return;
        }

        string backup = FindBackup();
        float scale = TargetPointSize / oldPointSize;

        try
        {
            ScaleFaceInfo(font, scale);

            // 清空字形表/字符表，保留 1 张 1024 图集，再按新字号整批重烘
            font.ClearFontAssetData(false);

            string missing;
            bool ok = font.TryAddCharacters(charset, out missing);

            if (!ok || font.characterTable == null || font.characterTable.Count == 0)
            {
                throw new Exception("TryAddCharacters 失败（ok=" + ok + "，缺失 " +
                                    (missing == null ? 0 : missing.Length) + " 个）");
            }

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int charsAfter = font.characterTable.Count;
            int atlasAfter = font.atlasTextures != null ? font.atlasTextures.Length : 0;

            Log($"完成：字号 {oldPointSize} -> {font.faceInfo.pointSize}；字符 {charsBefore} -> {charsAfter}；" +
                $"图集页 {atlasBefore} -> {atlasAfter}；缺失 {(string.IsNullOrEmpty(missing) ? 0 : missing.Length)} 个");

            EditorPrefs.SetBool(DoneKey, true);
        }
        catch (Exception e)
        {
            Log("失败：" + e.Message + "；开始回滚字体资产");
            Rollback(backup);
        }
    }

    /// <summary>把 FaceInfo 里所有像素量按比例缩放，保证行高/基线关系不变</summary>
    private static void ScaleFaceInfo(TMP_FontAsset font, float scale)
    {
        SerializedObject so = new SerializedObject(font);
        SerializedProperty faceInfo = so.FindProperty("m_FaceInfo");
        if (faceInfo == null)
        {
            Debug.LogWarning("[包体] 找不到 m_FaceInfo，字号未改（图集仍会重烘）");
            return;
        }

        List<string> changed = new List<string>();
        for (int i = 0; i < ScaledFaceFields.Length; i++)
        {
            SerializedProperty p = faceInfo.FindPropertyRelative(ScaledFaceFields[i]);
            if (p == null) continue;

            float before = p.floatValue;
            p.floatValue = before * scale;
            changed.Add($"{ScaledFaceFields[i]}:{before:0.##}->{p.floatValue:0.##}");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        so.Update();

        Debug.Log("[包体] FaceInfo 缩放: " + string.Join(", ", changed.ToArray()));
    }

    private static string BuildCharset()
    {
        StringBuilder sb = new StringBuilder();

        // 可见 ASCII
        for (int c = 32; c < 127; c++) sb.Append((char)c);

        if (File.Exists(CharsetPath))
        {
            sb.Append(File.ReadAllText(CharsetPath));
        }

        // 常用中文标点兜底（重复不影响）
        sb.Append("，。、；：？！…—～·“”‘’（）【】《》〈〉％＋－×÷＝");

        return sb.ToString();
    }

    /// <summary>找最近一次包体优化备份里的字体原件，用于失败回滚</summary>
    private static string FindBackup()
    {
        try
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Backup");
            if (!Directory.Exists(root)) return null;

            string[] dirs = Directory.GetDirectories(root, "包体优化_*");
            Array.Sort(dirs, string.CompareOrdinal);
            for (int i = dirs.Length - 1; i >= 0; i--)
            {
                string candidate = Path.Combine(dirs[i], "DroidSansFallback SDF.asset.bak");
                if (File.Exists(candidate)) return candidate;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[包体] 查找备份失败：" + e.Message);
        }

        return null;
    }

    private static void Rollback(string backup)
    {
        if (string.IsNullOrEmpty(backup) || !File.Exists(backup))
        {
            Log("回滚失败：找不到备份文件，请手工用 Backup/包体优化_* 里的 .bak 覆盖 " + FontPath);
            return;
        }

        try
        {
            File.Copy(backup, FontPath, true);
            AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceUpdate);
            Log("已回滚到备份：" + backup);
        }
        catch (Exception e)
        {
            Log("回滚异常：" + e.Message);
        }
    }

    private static void Log(string message)
    {
        Debug.Log("[包体] 中文字体图集精简：" + message);

        try
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), ResultPath);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, message + "\n");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[包体] 写结果文件失败：" + e.Message);
        }
    }
}
#endif
