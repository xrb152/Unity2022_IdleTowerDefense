using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace WorkbuddyTools
{
    /// <summary>
    /// 中文字体修复工具。
    ///
    /// 解决 Console 里的这两类报错：
    /// 1) UnassignedReferenceException: The variable m_AtlasTextures of TMP_FontAsset has not been assigned.
    ///    —— 字体资产的图集贴图在运行时是空引用，TMP 想动态补字就会抛异常。
    /// 2) The character with Unicode value \uXXXX was not found in the [xxx] font asset ...
    ///    —— 汉字没有被烘进图集，文字变成方框。
    ///
    /// 做法：
    ///   A. 检查 / 补建 DroidSansFallback SDF 的图集贴图，并把它挂到材质上；
    ///   B. 把工程里用到的全部汉字一次性烘进图集（多图集自动扩展），运行时就不用再动态补字；
    ///   C. 把该字体挂到其它所有 TMP 字体的兜底（Fallback）上，英文美术字里的中文也能显示。
    ///
    /// 菜单：工具 / 中文显示修复 / 一键修复
    /// </summary>
    public static class ChineseFontFixer
    {
        private const string FontAssetPath = "Assets/AdSDK/Font/DroidSansFallback SDF.asset";
        private const string CharsetPath = "Assets/Editor/ChineseCharset.txt";
        private const string DoneFlagPath = "Library/WorkbuddyFontFix.done";
        private const string ReportPath = "Library/WorkbuddyFontFix.log";

        private static readonly StringBuilder _report = new StringBuilder();

        // ---------------------------------------------------------------- 菜单

        [MenuItem("工具/中文显示修复/一键修复（补图集+烘焙汉字+挂兜底）", false, 0)]
        public static void FixAll()
        {
            _report.Length = 0;
            Log("==== 开始修复中文字体 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====");
            try
            {
                FixAtlasAndBake();
                AddFallbackToAllFonts();
            }
            catch (Exception e)
            {
                Log("!! 异常: " + e);
            }
            Flush();
        }

        [MenuItem("工具/中文显示修复/仅补图集并烘焙汉字", false, 10)]
        public static void FixAtlasAndBakeMenu()
        {
            _report.Length = 0;
            try { FixAtlasAndBake(); }
            catch (Exception e) { Log("!! 异常: " + e); }
            Flush();
        }

        [MenuItem("工具/中文显示修复/仅给所有 TMP 字体挂中文兜底", false, 20)]
        public static void AddFallbackMenu()
        {
            _report.Length = 0;
            try { AddFallbackToAllFonts(); }
            catch (Exception e) { Log("!! 异常: " + e); }
            Flush();
        }

        [InitializeOnLoadMethod]
        private static void AutoFixOnce()
        {
            if (File.Exists(DoneFlagPath)) return;

            EditorApplication.delayCall += () =>
            {
                if (File.Exists(DoneFlagPath)) return;

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Debug.LogWarning("[中文显示修复] 当前在播放模式，退出播放后点菜单 工具/中文显示修复/一键修复（补图集+烘焙汉字+挂兜底）。");
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(DoneFlagPath));
                File.WriteAllText(DoneFlagPath, DateTime.Now.ToString("s"));
                FixAll();
            };
        }

        // ------------------------------------------------------------ 核心逻辑

        private static void FixAtlasAndBake()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                Log("!! 找不到字体资产：" + FontAssetPath);
                Debug.LogError("[中文显示修复] 找不到字体资产：" + FontAssetPath);
                return;
            }

            Texture2D[] atlases = font.atlasTextures;
            int atlasCount = atlases == null ? 0 : atlases.Length;
            string atlas0 = "无";
            if (atlasCount > 0 && atlases[0] != null)
            {
                atlas0 = atlases[0].name + " " + atlases[0].width + "x" + atlases[0].height + " readable=" + atlases[0].isReadable;
            }
            int charCount = font.characterTable == null ? 0 : font.characterTable.Count;

            Log("字体: " + font.name);
            Log(" 图集数量: " + atlasCount + "，图集[0]: " + atlas0);
            Log(" 已烘字符数: " + charCount + "，populationMode: " + font.atlasPopulationMode);
            Log(" 材质: " + (font.material == null ? "NULL" : font.material.name));

            // 0. 关键：图集数组里可能有 fileID:0 的空槽位（历次重建留下的）。
            //    TMP 补字时把 m_AtlasTextureIndex 往后挪一格就撞上空槽，
            //    于是抛 UnassignedReferenceException: m_AtlasTextures ... has not been assigned。
            CleanupNullAtlasSlots(font);

            // 1. 保证有一张可读图集
            if (atlasCount == 0 || atlases[0] == null)
            {
                int w = font.atlasWidth > 0 ? font.atlasWidth : 1024;
                int h = font.atlasHeight > 0 ? font.atlasHeight : 1024;

                Texture2D tex = new Texture2D(w, h, TextureFormat.Alpha8, false);
                tex.name = font.name + " Atlas";
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.AddObjectToAsset(tex, font);

                font.atlasTextures = new Texture2D[] { tex };

                if (font.material != null)
                {
                    font.material.SetTexture("_MainTex", tex);
                    EditorUtility.SetDirty(font.material);
                }
                Log(" >> 图集为空，已新建 " + w + "x" + h + " Alpha8 图集并挂到材质");
            }
            else if (!atlases[0].isReadable)
            {
                // 图集存在但不可读：TMP 动态补字也会失败，这里改成可读
                string path = AssetDatabase.GetAssetPath(atlases[0]);
                if (!string.IsNullOrEmpty(path))
                {
                    TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti != null && !ti.isReadable)
                    {
                        ti.isReadable = true;
                        ti.SaveAndReimport();
                        Log(" >> 图集改为可读");
                    }
                }
            }

            // 2. 保证是动态模式（否则 TryAddCharacters 不干活）
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                Log(" >> populationMode 改为 Dynamic");
            }

            // 3. 烘焙字符
            string charset = BuildCharset();
            Log(" 待烘字符集长度: " + charset.Length);

            string missing = string.Empty;
            bool ok = false;
            try
            {
                ok = font.TryAddCharacters(charset, out missing);
            }
            catch (Exception e)
            {
                Log(" !! TryAddCharacters 抛异常: " + e.Message);
            }

            Log(" TryAddCharacters ok=" + ok + "，缺失=" + (missing == null ? 0 : missing.Length) + " 个");
            if (!string.IsNullOrEmpty(missing))
            {
                Log(" 缺失字符: " + missing);
            }
            Log(" 烘焙后字符数: " + (font.characterTable == null ? 0 : font.characterTable.Count)
                + "，图集数: " + (font.atlasTextures == null ? 0 : font.atlasTextures.Length));

            // 4. 新增的图集 / 材质都要注册成子资产，否则存盘后引用会丢
            if (font.atlasTextures != null)
            {
                for (int i = 0; i < font.atlasTextures.Length; i++)
                {
                    Texture2D t = font.atlasTextures[i];
                    if (t == null) continue;
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(t)))
                    {
                        t.name = font.name + " Atlas " + i;
                        AssetDatabase.AddObjectToAsset(t, font);
                        Log(" >> 注册图集子资产: " + t.name);
                    }
                }
            }

            if (font.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(font.material)))
            {
                AssetDatabase.AddObjectToAsset(font.material, font);
                Log(" >> 注册材质子资产: " + font.material.name);
            }

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Log(" 已保存字体资产");

            // 5. 烘焙后可能又生成新图集，再清一次空槽并校验
            CleanupNullAtlasSlots(font);
        }

        /// <summary>
        /// 把 m_AtlasTextures 里的空槽位（null）删掉，保持数组紧凑。
        /// TMP 内部假定这个数组是连续的：m_AtlasTextureIndex 每用完一张就 +1，
        /// 一旦落到 null 槽位就会抛 UnassignedReferenceException。
        /// </summary>
        private static void CleanupNullAtlasSlots(TMP_FontAsset font)
        {
            Texture2D[] atlases = font.atlasTextures;
            if (atlases == null || atlases.Length == 0) return;

            int nullCount = 0;
            List<Texture2D> valid = new List<Texture2D>(atlases.Length);
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i] == null) { nullCount++; continue; }
                valid.Add(atlases[i]);
            }

            if (nullCount == 0) return;

            font.atlasTextures = valid.ToArray();
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Log(" >> 清理空图集槽位 " + nullCount + " 个，剩余图集 " + valid.Count + " 张");
        }

        private static void AddFallbackToAllFonts()
        {
            TMP_FontAsset cn = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (cn == null)
            {
                Log("!! 找不到中文字体，跳过兜底设置");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            int changed = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                if (path.StartsWith("Assets/TextMesh Pro/Examples")) continue;

                TMP_FontAsset other = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (other == null || other == cn) continue;

                if (other.fallbackFontAssetTable == null)
                {
                    other.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                // 已经是兜底了，或者会把兜底绕成环，都跳过
                if (other.fallbackFontAssetTable.Contains(cn)) continue;
                if (cn.fallbackFontAssetTable != null && cn.fallbackFontAssetTable.Contains(other)) continue;

                other.fallbackFontAssetTable.Add(cn);
                EditorUtility.SetDirty(other);
                changed++;
            }

            if (changed > 0)
            {
                AssetDatabase.SaveAssets();
            }
            Log(" 兜底设置：共给 " + changed + " 个字体挂上 [" + cn.name + "]");
        }

        // ------------------------------------------------------------ 工具方法

        private static string BuildCharset()
        {
            StringBuilder sb = new StringBuilder();

            // 可见 ASCII
            for (int c = 32; c < 127; c++) sb.Append((char)c);

            if (File.Exists(CharsetPath))
            {
                string s = File.ReadAllText(CharsetPath);
                sb.Append(s);
            }
            else
            {
                Log(" !! 未找到字符集文件 " + CharsetPath + "，只烘 ASCII");
            }

            // 常用中文标点补一手（字符集文件里通常已有，重复不影响）
            sb.Append("，。、；：？！…—～·“”‘’（）【】《》〈〉％＋－×÷＝");

            return sb.ToString();
        }

        private static void Log(string line)
        {
            _report.AppendLine(line);
            Debug.Log("[中文显示修复] " + line);
        }

        private static void Flush()
        {
            try
            {
                Directory.CreateDirectory("Library");
                File.WriteAllText(ReportPath, _report.ToString(), new UTF8Encoding(false));
            }
            catch (Exception)
            {
                // 写不了报告不影响修复
            }
        }
    }
}
