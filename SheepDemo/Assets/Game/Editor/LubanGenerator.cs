#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Luban 配置生成工具
    /// 用于调用 Luban 的 gen.bat 生成配置代码和数据
    /// </summary>
    public static class LubanGenerator
    {
        private const string MENU_PATH = "Tools/Luban/Generate Config";
        private const string GEN_BAT_NAME = "gen.bat";
        private const string TABLES_CS_PATH = "Assets/Configs/Code/Tables.cs";
        private const string CONFIG_FILE_NAMES_PATH = "Assets/Game/Scripts/Config/ConfigFileNames.cs";

        private const string MENU_UPDATE_LEVELSORT = "Tools/Luban/Update LevelSort To Guru Spec";
        private const string LEVELSORT_JSON_ASSET_PATH = "Assets/Game/Configs/tblevelsort.json";
        private const string GURU_SPEC_YAML_ASSET_PATH = "Assets/Guru/guru_spec.yaml";
        
        [MenuItem(MENU_PATH, false, 1)]
        public static void GenerateConfig()
        {
            string genBatPath = GetGenBatPath();
            
            if (string.IsNullOrEmpty(genBatPath))
            {
                EditorUtility.DisplayDialog("Error", 
                    $"找不到 {GEN_BAT_NAME} 文件！\n\n请确保文件存在于：\nLuban/Configs/gen.bat", 
                    "OK");
                return;
            }

            if (!File.Exists(genBatPath))
            {
                EditorUtility.DisplayDialog("Error", 
                    $"文件不存在：\n{genBatPath}", 
                    "OK");
                return;
            }

            // 确认生成
            if (!EditorUtility.DisplayDialog("Generate Config", 
                $"即将执行配置生成：\n\n{genBatPath}\n\n这可能会覆盖现有的配置代码文件。", 
                "确定", "取消"))
            {
                return;
            }

            ExecuteGenBat(genBatPath);
        }

        [MenuItem(MENU_UPDATE_LEVELSORT, false, 10)]
        public static void UpdateLevelSortToGuruSpec()
        {
            string projectRoot = GetProjectRoot();
            if (string.IsNullOrEmpty(projectRoot))
            {
                EditorUtility.DisplayDialog("Error", "无法定位项目根目录。", "OK");
                return;
            }

            string jsonAbsPath = ToAbsolutePath(projectRoot, LEVELSORT_JSON_ASSET_PATH);
            string yamlAbsPath = ToAbsolutePath(projectRoot, GURU_SPEC_YAML_ASSET_PATH);

            if (!File.Exists(jsonAbsPath))
            {
                EditorUtility.DisplayDialog("Error", $"找不到 levelsort 配置文件：\n\n{jsonAbsPath}", "OK");
                return;
            }

            if (!File.Exists(yamlAbsPath))
            {
                EditorUtility.DisplayDialog("Error", $"找不到 guru spec 文件：\n\n{yamlAbsPath}", "OK");
                return;
            }

            List<string> levelSortPaths = ExtractPathListFromLevelSortJson(jsonAbsPath);
            if (levelSortPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", $"未从配置中提取到任何 path：\n\n{LEVELSORT_JSON_ASSET_PATH}", "OK");
                return;
            }

            string preview = string.Join(", ", levelSortPaths.Take(15));
            if (levelSortPaths.Count > 15)
            {
                preview += ", ...";
            }

            if (!EditorUtility.DisplayDialog(
                    "Update LevelSort",
                    $"即将更新 `{GURU_SPEC_YAML_ASSET_PATH}` 中 level_config.levelsort。\n\n来源：{LEVELSORT_JSON_ASSET_PATH}\n数量：{levelSortPaths.Count}\n预览：[{preview}]\n\n将覆盖 level_config 中的 levelsort 数组。",
                    "确定",
                    "取消"))
            {
                return;
            }

            bool allInt = levelSortPaths.All(p => int.TryParse(p, out _));
            string arrayText = allInt
                ? string.Join(",", levelSortPaths.Select(p => int.Parse(p).ToString()))
                : string.Join(",", levelSortPaths.Select(p => $"\"{EscapeJsonString(p)}\""));

            try
            {
                string yamlContent = File.ReadAllText(yamlAbsPath);
                if (!TryUpdateGuruSpecLevelSort(yamlContent, arrayText, out string newYamlContent, out string error))
                {
                    EditorUtility.DisplayDialog("Error", error, "OK");
                    return;
                }

                if (newYamlContent == yamlContent)
                {
                    EditorUtility.DisplayDialog("No Changes", "levelsort 内容一致，无需更新。", "OK");
                    return;
                }

                File.WriteAllText(yamlAbsPath, newYamlContent);
                AssetDatabase.ImportAsset(GURU_SPEC_YAML_ASSET_PATH);
                UnityEngine.Debug.Log($"[LubanGenerator] 已更新 {GURU_SPEC_YAML_ASSET_PATH} 的 level_config.levelsort（{levelSortPaths.Count} 条）。");
                EditorUtility.DisplayDialog("Success", "levelsort 已写入 guru_spec.yaml。", "OK");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[LubanGenerator] 更新 guru_spec.yaml 失败: {e}");
                EditorUtility.DisplayDialog("Error", $"更新失败：\n\n{e.Message}", "OK");
            }
        }

        /// <summary>
        /// 获取 gen.bat 文件路径
        /// </summary>
        private static string GetGenBatPath()
        {
            // 从 Assets 目录向上查找 Luban/Configs/gen.bat
            string assetsPath = Application.dataPath.Replace('\\', '/');
            string projectRoot = Directory.GetParent(assetsPath)?.FullName.Replace('\\', '/');
            
            if (string.IsNullOrEmpty(projectRoot))
            {
                return null;
            }

            string genBatPath = Path.Combine(projectRoot, "Luban", "Configs", GEN_BAT_NAME).Replace('\\', '/');
            return genBatPath;
        }

        private static string GetProjectRoot()
        {
            string assetsPath = Application.dataPath.Replace('\\', '/');
            return Directory.GetParent(assetsPath)?.FullName.Replace('\\', '/');
        }

        private static string ToAbsolutePath(string projectRoot, string assetPath)
        {
            if (string.IsNullOrEmpty(projectRoot) || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private static List<string> ExtractPathListFromLevelSortJson(string jsonAbsPath)
        {
            string json = File.ReadAllText(jsonAbsPath);

            // 轻量解析：仅提取 "path":"xxx"。避免 Unity JsonUtility 顶层数组限制。
            Regex regex = new Regex(@"""path""\s*:\s*""([^""]+)""", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(json);

            List<string> result = new List<string>(matches.Count);
            foreach (Match match in matches)
            {
                if (match.Groups.Count < 2)
                {
                    continue;
                }

                string value = match.Groups[1].Value?.Trim();
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                result.Add(value);
            }

            return result;
        }

        private static bool TryUpdateGuruSpecLevelSort(string yamlContent, string levelsortArrayText, out string newYamlContent, out string error)
        {
            newYamlContent = yamlContent;
            error = null;

            if (string.IsNullOrEmpty(yamlContent))
            {
                error = "guru_spec.yaml 内容为空。";
                return false;
            }

            if (string.IsNullOrEmpty(levelsortArrayText))
            {
                error = "levelsort 列表为空。";
                return false;
            }

            // 匹配单行：level_config: '{"enabled":false,"levelsort":[...]}'（保留原结构，只替换 levelsort 数组）
            Regex lineRegex = new Regex(@"^(?<indent>\s*)level_config:\s*'(?<json>\{[^']*\})'\s*$", RegexOptions.Multiline);
            Match match = lineRegex.Match(yamlContent);
            if (!match.Success)
            {
                error = $"未在 {GURU_SPEC_YAML_ASSET_PATH} 中找到 level_config 行。";
                return false;
            }

            string json = match.Groups["json"].Value;
            Regex levelsortRegex = new Regex(@"""levelsort""\s*:\s*\[[^\]]*\]");
            if (!levelsortRegex.IsMatch(json))
            {
                error = $"level_config JSON 中未找到 levelsort 字段：\n\n{json}";
                return false;
            }

            string newJson = levelsortRegex.Replace(json, $"\"levelsort\":[{levelsortArrayText}]", 1);
            if (newJson == json)
            {
                return true;
            }

            string indent = match.Groups["indent"].Value;
            string oldLine = match.Value;
            string newLine = $"{indent}level_config: '{newJson}'";
            newYamlContent = ReplaceFirst(newYamlContent, oldLine, newLine);
            return true;
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, System.StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        /// <summary>
        /// 执行 gen.bat 文件
        /// </summary>
        private static void ExecuteGenBat(string batPath)
        {
            string workingDirectory = Path.GetDirectoryName(batPath).Replace('\\', '/');
            
            UnityEngine.Debug.Log($"[LubanGenerator] 开始执行配置生成...");
            UnityEngine.Debug.Log($"[LubanGenerator] 工作目录: {workingDirectory}");
            UnityEngine.Debug.Log($"[LubanGenerator] 批处理文件: {batPath}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = batPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
                CreateNoWindow = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            try
            {
                Process process = Process.Start(startInfo);
                
                if (process != null)
                {
                    UnityEngine.Debug.Log($"[LubanGenerator] 进程已启动 (PID: {process.Id})");
                    
                    // 等待进程完成（可选，如果需要同步等待）
                    // process.WaitForExit();
                    
                    // 等待进程完成后更新配置文件名
                    EditorApplication.delayCall += () =>
                    {
                        // 延迟执行，确保文件写入完成
                        EditorApplication.delayCall += () =>
                        {
                            AssetDatabase.Refresh();
                            UpdateConfigFileNames();
                            UnityEngine.Debug.Log("[LubanGenerator] 配置生成完成，已刷新资源数据库并更新配置文件名。");
                        };
                    };
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "无法启动生成进程！", "OK");
                }
            }
            catch (System.Exception e)
            {
                string errorMsg = $"执行配置生成时发生错误：\n\n{e.Message}";
                UnityEngine.Debug.LogError($"[LubanGenerator] {errorMsg}");
                EditorUtility.DisplayDialog("Error", errorMsg, "OK");
            }
        }

        /// <summary>
        /// 验证 gen.bat 文件是否存在
        /// </summary>
        [MenuItem(MENU_PATH + " (Validate)", false, 2)]
        public static void ValidateGenBat()
        {
            string genBatPath = GetGenBatPath();
            
            if (string.IsNullOrEmpty(genBatPath))
            {
                EditorUtility.DisplayDialog("Validation", 
                    $"❌ 找不到 {GEN_BAT_NAME} 文件！", 
                    "OK");
                return;
            }

            if (File.Exists(genBatPath))
            {
                EditorUtility.DisplayDialog("Validation", 
                    $"✅ 找到 gen.bat 文件：\n\n{genBatPath}", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation", 
                    $"❌ 文件不存在：\n\n{genBatPath}", 
                    "OK");
            }
        }

        /// <summary>
        /// 从 Tables.cs 中提取配置文件名
        /// </summary>
        private static List<string> ExtractConfigFileNamesFromTables()
        {
            List<string> fileNames = new List<string>();
            
            string tablesPath = TABLES_CS_PATH.Replace('/', Path.DirectorySeparatorChar);
            if (!File.Exists(tablesPath))
            {
                UnityEngine.Debug.LogWarning($"[LubanGenerator] Tables.cs 文件不存在: {tablesPath}");
                return fileNames;
            }

            try
            {
                string content = File.ReadAllText(tablesPath);
                
                // 使用正则表达式匹配 loader("filename") 模式
                Regex regex = new Regex(@"loader\s*\(\s*""([^""]+)""\s*\)", RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(content);
                
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string fileName = match.Groups[1].Value;
                        if (!string.IsNullOrEmpty(fileName) && !fileNames.Contains(fileName))
                        {
                            fileNames.Add(fileName);
                        }
                    }
                }
                
                UnityEngine.Debug.Log($"[LubanGenerator] 从 Tables.cs 中提取到 {fileNames.Count} 个配置文件名");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[LubanGenerator] 解析 Tables.cs 时发生错误: {e.Message}");
            }
            
            return fileNames;
        }

        /// <summary>
        /// 更新 ConfigFileNames.cs 文件
        /// </summary>
        private static void UpdateConfigFileNames()
        {
            List<string> configFileNames = ExtractConfigFileNamesFromTables();
            
            if (configFileNames.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[LubanGenerator] 未找到配置文件名，跳过更新。");
                return;
            }

            string configFileNamesPath = CONFIG_FILE_NAMES_PATH.Replace('/', Path.DirectorySeparatorChar);
            if (!File.Exists(configFileNamesPath))
            {
                UnityEngine.Debug.LogWarning($"[LubanGenerator] ConfigFileNames.cs 文件不存在: {configFileNamesPath}");
                return;
            }

            try
            {
                string content = File.ReadAllText(configFileNamesPath);
                string originalContent = content;

                // 构建新的配置文件名数组代码
                string newArrayCode = "return new string[]\n            {\n";
                foreach (string fileName in configFileNames)
                {
                    newArrayCode += $"                \"{fileName}\",\n";
                }
                newArrayCode = newArrayCode.TrimEnd(',', '\n') + "\n            };";

                // 使用正则表达式匹配 GetFileNames 方法中的 return 语句
                Regex regex = new Regex(
                    @"(public\s+static\s+string\[\]\s+GetFileNames\(\)\s*\{[^}]*return\s+new\s+string\[\]\s*\{[^}]*\};)",
                    RegexOptions.Singleline | RegexOptions.Multiline
                );

                Match match = regex.Match(content);
                if (match.Success)
                {
                    // 找到方法体，替换 return 语句部分
                    string methodBody = match.Groups[1].Value;
                    Regex returnRegex = new Regex(@"return\s+new\s+string\[\]\s*\{[^}]*\};", RegexOptions.Singleline);
                    string newMethodBody = returnRegex.Replace(methodBody, newArrayCode);
                    content = content.Replace(methodBody, newMethodBody);
                }
                else
                {
                    // 如果没找到完整方法，尝试只替换 return 语句
                    Regex returnRegex = new Regex(@"return\s+new\s+string\[\]\s*\{[^}]*\};", RegexOptions.Singleline);
                    content = returnRegex.Replace(content, newArrayCode);
                }

                // 如果内容有变化，写入文件
                if (content != originalContent)
                {
                    File.WriteAllText(configFileNamesPath, content);
                    AssetDatabase.ImportAsset(CONFIG_FILE_NAMES_PATH);
                    UnityEngine.Debug.Log($"[LubanGenerator] 已更新 ConfigFileNames.cs 中的配置文件名列表:\n{string.Join("\n", configFileNames.Select(f => $"  - {f}"))}");
                }
                else
                {
                    UnityEngine.Debug.Log("[LubanGenerator] ConfigFileNames.cs 中的配置文件名列表无需更新。");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[LubanGenerator] 更新 ConfigFileNames.cs 时发生错误: {e.Message}");
            }
        }

        /// <summary>
        /// 手动更新配置文件名（用于测试）
        /// </summary>
        [MenuItem(MENU_PATH + " (Update File Names)", false, 3)]
        public static void ManualUpdateConfigFileNames()
        {
            UpdateConfigFileNames();
            EditorUtility.DisplayDialog("Update Complete", "配置文件名已更新！", "OK");
        }
    }
}
#endif
