using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Bear.SaveModule.Editor
{
    /// <summary>
    /// 数据查看器 - 查看和管理 PlayerPrefs 和 Json 存储的数据
    /// </summary>
    public class DataViewer : EditorWindow
    {
        private enum StorageTab
        {
            PlayerPrefs,
            Json
        }
        
        private StorageTab _currentTab = StorageTab.PlayerPrefs;
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        
        // DBSetting 引用
        private DBSetting _dbSetting;
        
        // PlayerPrefs 数据
        private List<PlayerPrefsData> _playerPrefsData = new List<PlayerPrefsData>();
        private int _selectedPlayerPrefsIndex = -1;
        
        // Json 数据
        private List<JsonData> _jsonData = new List<JsonData>();
        private int _selectedJsonIndex = -1;
        
        // 编辑状态
        private string _editingKey = "";
        private string _editingValue = "";
        private bool _isEditing = false;
        
        [MenuItem("Tools/DataViewer", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<DataViewer>("Data Viewer");
        }
        
        private void OnEnable()
        {
            // 尝试加载 DBSetting
            LoadDBSetting();
            RefreshData();
        }
        
        private void LoadDBSetting()
        {
            if (_dbSetting == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DBSetting");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _dbSetting = AssetDatabase.LoadAssetAtPath<DBSetting>(path);
                }
            }
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();
            DrawToolbar();
            DrawDataList();
            
            // 使用 FlexibleSpace 确保编辑面板在底部可见
            GUILayout.FlexibleSpace();
            DrawEditPanel();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("数据查看器", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
            {
                RefreshData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            bool playerPrefsSelected = _currentTab == StorageTab.PlayerPrefs;
            bool playerPrefsNewValue = GUILayout.Toggle(playerPrefsSelected, "PlayerPrefs", EditorStyles.toolbarButton);
            
            if (playerPrefsNewValue != playerPrefsSelected)
            {
                if (playerPrefsNewValue)
                {
                    _currentTab = StorageTab.PlayerPrefs;
                    _selectedPlayerPrefsIndex = -1;
                    _selectedJsonIndex = -1;
                    _isEditing = false;
                }
            }
            
            bool jsonSelected = _currentTab == StorageTab.Json;
            bool jsonNewValue = GUILayout.Toggle(jsonSelected, "Json Files", EditorStyles.toolbarButton);
            
            if (jsonNewValue != jsonSelected)
            {
                if (jsonNewValue)
                {
                    _currentTab = StorageTab.Json;
                    _selectedPlayerPrefsIndex = -1;
                    _selectedJsonIndex = -1;
                    _isEditing = false;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("搜索:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            
            GUILayout.FlexibleSpace();
            
            if (_currentTab == StorageTab.Json)
            {
                if (GUILayout.Button("打开保存目录", EditorStyles.toolbarButton))
                {
                    OpenJsonSaveDirectory();
                }
            }
            
            if (GUILayout.Button("删除选中", EditorStyles.toolbarButton))
            {
                DeleteSelected();
            }
            
            if (GUILayout.Button("删除所有", EditorStyles.toolbarButton))
            {
                DeleteAll();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawDataList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_currentTab == StorageTab.PlayerPrefs)
            {
                DrawPlayerPrefsList();
            }
            else
            {
                DrawJsonList();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawPlayerPrefsList()
        {
            var filteredData = _playerPrefsData.Where(d => 
                string.IsNullOrEmpty(_searchFilter) || 
                d.Key.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                d.Value.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            if (filteredData.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到 PlayerPrefs 数据", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField($"找到 {filteredData.Count} 条数据", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            
            for (int i = 0; i < filteredData.Count; i++)
            {
                var data = filteredData[i];
                int originalIndex = _playerPrefsData.IndexOf(data);
                
                EditorGUILayout.BeginHorizontal();
                
                bool isSelected = _selectedPlayerPrefsIndex == originalIndex;
                bool newValue = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));
                
                if (newValue != isSelected)
                {
                    if (newValue)
                    {
                        _selectedPlayerPrefsIndex = originalIndex;
                    }
                    else
                    {
                        _selectedPlayerPrefsIndex = -1;
                    }
                    _isEditing = false;
                }
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(data.Key, GUILayout.Width(200));
                EditorGUILayout.TextField(data.Value, GUILayout.ExpandWidth(true));
                EditorGUILayout.TextField(data.Type, GUILayout.Width(80));
                EditorGUI.EndDisabledGroup();
                
                if (GUILayout.Button("编辑", GUILayout.Width(50)))
                {
                    _selectedPlayerPrefsIndex = originalIndex;
                    _editingKey = data.Key;
                    _editingValue = data.Value;
                    _isEditing = true;
                }
                
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除键 '{data.Key}' 吗？", "删除", "取消"))
                    {
                        DeletePlayerPrefsKey(data.Key);
                        RefreshData();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawJsonList()
        {
            var filteredData = _jsonData.Where(d => 
                string.IsNullOrEmpty(_searchFilter) || 
                d.FileName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                d.Content.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            if (filteredData.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到 Json 文件", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField($"找到 {filteredData.Count} 个文件", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            
            for (int i = 0; i < filteredData.Count; i++)
            {
                var data = filteredData[i];
                int originalIndex = _jsonData.IndexOf(data);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                
                bool isSelected = _selectedJsonIndex == originalIndex;
                bool newValue = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));
                
                if (newValue != isSelected)
                {
                    if (newValue)
                    {
                        _selectedJsonIndex = originalIndex;
                    }
                    else
                    {
                        _selectedJsonIndex = -1;
                    }
                    _isEditing = false;
                }
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("文件名:", data.FileName, GUILayout.ExpandWidth(true));
                EditorGUILayout.TextField("大小:", FormatFileSize(data.FileSize), GUILayout.Width(80));
                EditorGUI.EndDisabledGroup();
                
                if (GUILayout.Button("打开", GUILayout.Width(50)))
                {
                    OpenJsonFileInExplorer(data.FilePath);
                }
                
                if (GUILayout.Button("编辑", GUILayout.Width(50)))
                {
                    _selectedJsonIndex = originalIndex;
                    _editingKey = data.FileName;
                    _editingValue = data.Content;
                    _isEditing = true;
                }
                
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除文件 '{data.FileName}' 吗？", "删除", "取消"))
                    {
                        DeleteJsonFile(data.FilePath);
                        RefreshData();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 直接展示存储数据
                EditorGUILayout.Space(1);
                EditorGUILayout.LabelField("存储数据:", EditorStyles.miniLabel);
                EditorGUILayout.TextArea(data.Content, GUILayout.Height(40));
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }
        
        private void DrawEditPanel()
        {
            if (!_isEditing)
            {
                return;
            }
            
            // 使用固定区域确保编辑面板始终可见
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(220));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("编辑数据", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginDisabledGroup(_currentTab == StorageTab.Json);
            _editingKey = EditorGUILayout.TextField("键名:", _editingKey);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("值:");
            _editingValue = EditorGUILayout.TextArea(_editingValue, EditorStyles.textArea, GUILayout.Height(100));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("保存", GUILayout.Height(25)))
            {
                SaveEditedData();
            }
            
            if (GUILayout.Button("取消", GUILayout.Height(25)))
            {
                _isEditing = false;
                _editingKey = "";
                _editingValue = "";
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            
            EditorGUILayout.EndVertical();
        }
        
        private void RefreshData()
        {
            RefreshPlayerPrefs();
            RefreshJsonFiles();
            _isEditing = false;
            _selectedPlayerPrefsIndex = -1;
            _selectedJsonIndex = -1;
        }
        
        private void RefreshPlayerPrefs()
        {
            _playerPrefsData.Clear();
            
            try
            {
                LoadDBSetting();
                
                // 从 DBSetting 获取使用 PlayerPrefs 存储的数据类
                List<string> saveModuleKeys = new List<string>();
                
                if (_dbSetting != null && _dbSetting.DataClasses != null)
                {
                    foreach (var dataClass in _dbSetting.DataClasses)
                    {
                        if (dataClass.storageType == StorageType.PlayerPrefs)
                        {
                            // PlayerPrefs 存储使用 "SaveData_" 前缀
                            string key = "SaveData_" + dataClass.className;
                            saveModuleKeys.Add(key);
                        }
                    }
                }
                
                // 如果没有 DBSetting，尝试扫描所有 BaseSaveDataSO 类
                if (saveModuleKeys.Count == 0)
                {
                    Type baseSaveDataSOType = typeof(BaseSaveDataSO);
                    System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    
                    foreach (var assembly in assemblies)
                    {
                        try
                        {
                            Type[] types = assembly.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.IsClass && !type.IsAbstract && baseSaveDataSOType.IsAssignableFrom(type) && type != baseSaveDataSOType)
                                {
                                    // 检查存储类型
                                    StorageType storageType = type.GetStorageType();
                                    if (storageType == StorageType.PlayerPrefs)
                                    {
                                        string key = "SaveData_" + type.Name;
                                        if (PlayerPrefs.HasKey(key) && !saveModuleKeys.Contains(key))
                                        {
                                            saveModuleKeys.Add(key);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {ex.Message}");
                        }
                    }
                }
                
                // 添加其他 PlayerPrefs 键（非 SaveModule 管理的）
                string[] allKeys = GetAllPlayerPrefsKeys();
                foreach (var key in allKeys)
                {
                    if (!key.StartsWith("SaveData_") && PlayerPrefs.HasKey(key))
                    {
                        saveModuleKeys.Add(key);
                    }
                }
                
                // 读取所有键的数据
                foreach (var key in saveModuleKeys)
                {
                    if (PlayerPrefs.HasKey(key))
                    {
                        string value = "";
                        string type = "String";
                        
                        // Unity 的 PlayerPrefs 在同一键上只能存储一种类型
                        // 我们需要尝试所有类型来确定实际类型
                        string stringValue = PlayerPrefs.GetString(key, null);
                        if (stringValue != null)
                        {
                            value = stringValue;
                            type = "String";
                        }
                        else
                        {
                            // 尝试作为 int（使用一个不存在的默认值来检测）
                            int testInt = PlayerPrefs.GetInt(key, int.MinValue);
                            if (testInt != int.MinValue)
                            {
                                value = testInt.ToString();
                                type = "Int";
                            }
                            else
                            {
                                // 尝试作为 float
                                float testFloat = PlayerPrefs.GetFloat(key, float.MinValue);
                                if (testFloat != float.MinValue)
                                {
                                    value = testFloat.ToString();
                                    type = "Float";
                                }
                                else
                                {
                                    // 如果都失败，尝试再次读取 string
                                    value = PlayerPrefs.GetString(key, "");
                                    type = "String";
                                }
                            }
                        }
                        
                        _playerPrefsData.Add(new PlayerPrefsData
                        {
                            Key = key,
                            Value = value,
                            Type = type
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新 PlayerPrefs 数据失败: {ex.Message}");
            }
        }
        
        private string[] GetAllPlayerPrefsKeys()
        {
            List<string> keys = new List<string>();
            
            // 添加常见的 Unity 键（非 SaveModule 管理的）
            keys.AddRange(new[] {
                "UnityGraphicsQuality",
                "UnitySelectMonitor",
                "Screenmanager Resolution Width",
                "Screenmanager Resolution Height",
                "Screenmanager Is Fullscreen mode",
                "Screenmanager Fullscreen mode"
            });
            
            // 过滤掉不存在的键
            return keys.Where(k => PlayerPrefs.HasKey(k)).ToArray();
        }
        
        private void RefreshJsonFiles()
        {
            _jsonData.Clear();
            
            try
            {
                LoadDBSetting();
                
                string saveDirectory = Path.Combine(Application.persistentDataPath, "SaveData");
                
                if (Directory.Exists(saveDirectory))
                {
                    // 从 DBSetting 获取使用 Json 存储的数据类
                    HashSet<string> expectedFileNames = new HashSet<string>();
                    
                    if (_dbSetting != null && _dbSetting.DataClasses != null)
                    {
                        foreach (var dataClass in _dbSetting.DataClasses)
                        {
                            if (dataClass.storageType == StorageType.Json)
                            {
                                // Json 存储使用 "类名.json" 文件名
                                string fileName = dataClass.className + ".json";
                                expectedFileNames.Add(fileName);
                            }
                        }
                    }
                    
                    // 如果没有 DBSetting，尝试扫描所有 BaseSaveDataSO 类
                    if (expectedFileNames.Count == 0)
                    {
                        Type baseSaveDataSOType = typeof(BaseSaveDataSO);
                        System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                        
                        foreach (var assembly in assemblies)
                        {
                            try
                            {
                                Type[] types = assembly.GetTypes();
                                foreach (var type in types)
                                {
                                    if (type.IsClass && !type.IsAbstract && baseSaveDataSOType.IsAssignableFrom(type) && type != baseSaveDataSOType)
                                    {
                                        // 检查存储类型
                                        StorageType storageType = type.GetStorageType();
                                        if (storageType == StorageType.Json || storageType == StorageType.Auto)
                                        {
                                            string fileName = type.Name + ".json";
                                            expectedFileNames.Add(fileName);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {ex.Message}");
                            }
                        }
                    }
                    
                    // 读取所有 Json 文件
                    string[] jsonFiles = Directory.GetFiles(saveDirectory, "*.json");
                    
                    foreach (var filePath in jsonFiles)
                    {
                        try
                        {
                            string fileName = Path.GetFileName(filePath);
                            
                            // 如果 DBSetting 存在，只显示预期的文件；否则显示所有文件
                            if (expectedFileNames.Count > 0 && !expectedFileNames.Contains(fileName))
                            {
                                continue;
                            }
                            
                            string content = File.ReadAllText(filePath);
                            FileInfo fileInfo = new FileInfo(filePath);
                            
                            _jsonData.Add(new JsonData
                            {
                                FileName = fileName,
                                FilePath = filePath,
                                Content = content,
                                FileSize = fileInfo.Length
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"读取文件失败 {filePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新 Json 文件失败: {ex.Message}");
            }
        }
        
        private void DeleteSelected()
        {
            if (_currentTab == StorageTab.PlayerPrefs)
            {
                if (_selectedPlayerPrefsIndex >= 0 && _selectedPlayerPrefsIndex < _playerPrefsData.Count)
                {
                    var data = _playerPrefsData[_selectedPlayerPrefsIndex];
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除键 '{data.Key}' 吗？", "删除", "取消"))
                    {
                        DeletePlayerPrefsKey(data.Key);
                        RefreshData();
                    }
                }
            }
            else
            {
                if (_selectedJsonIndex >= 0 && _selectedJsonIndex < _jsonData.Count)
                {
                    var data = _jsonData[_selectedJsonIndex];
                    if (EditorUtility.DisplayDialog("确认删除", $"确定要删除文件 '{data.FileName}' 吗？", "删除", "取消"))
                    {
                        DeleteJsonFile(data.FilePath);
                        RefreshData();
                    }
                }
            }
        }
        
        private void DeleteAll()
        {
            string message = _currentTab == StorageTab.PlayerPrefs 
                ? "确定要删除所有 PlayerPrefs 数据吗？此操作不可恢复！" 
                : "确定要删除所有 Json 文件吗？此操作不可恢复！";
            
            if (EditorUtility.DisplayDialog("确认删除所有", message, "删除", "取消"))
            {
                if (_currentTab == StorageTab.PlayerPrefs)
                {
                    foreach (var data in _playerPrefsData)
                    {
                        DeletePlayerPrefsKey(data.Key);
                    }
                }
                else
                {
                    foreach (var data in _jsonData)
                    {
                        try
                        {
                            File.Delete(data.FilePath);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"删除文件失败 {data.FilePath}: {ex.Message}");
                        }
                    }
                }
                
                RefreshData();
                EditorUtility.DisplayDialog("完成", "所有数据已删除", "确定");
            }
        }
        
        private void DeletePlayerPrefsKey(string key)
        {
            try
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"已删除 PlayerPrefs 键: {key}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除 PlayerPrefs 键失败 {key}: {ex.Message}");
            }
        }
        
        private void DeleteJsonFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"已删除文件: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除文件失败 {filePath}: {ex.Message}");
            }
        }
        
        private void OpenJsonSaveDirectory()
        {
            try
            {
                string saveDirectory = Path.Combine(Application.persistentDataPath, "SaveData");
                
                if (Directory.Exists(saveDirectory))
                {
                    EditorUtility.RevealInFinder(saveDirectory);
                    Debug.Log($"已打开保存目录: {saveDirectory}");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"保存目录不存在: {saveDirectory}", "确定");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"打开保存目录失败: {ex.Message}", "确定");
                Debug.LogError($"打开保存目录失败: {ex.Message}");
            }
        }
        
        private void OpenJsonFileInExplorer(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // 获取文件所在目录
                    string directory = Path.GetDirectoryName(filePath);
                    
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        EditorUtility.RevealInFinder(filePath);
                        Debug.Log($"已在文件资源管理器中打开: {filePath}");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", $"文件目录不存在: {directory}", "确定");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", $"文件不存在: {filePath}", "确定");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"打开文件失败: {ex.Message}", "确定");
                Debug.LogError($"打开文件失败 {filePath}: {ex.Message}");
            }
        }
        
        private void SaveEditedData()
        {
            if (string.IsNullOrEmpty(_editingKey) || string.IsNullOrEmpty(_editingValue))
            {
                EditorUtility.DisplayDialog("错误", "键名和值不能为空", "确定");
                return;
            }
            
            if (_currentTab == StorageTab.PlayerPrefs)
            {
                try
                {
                    PlayerPrefs.SetString(_editingKey, _editingValue);
                    PlayerPrefs.Save();
                    Debug.Log($"已保存 PlayerPrefs: {_editingKey} = {_editingValue}");
                    RefreshData();
                    _isEditing = false;
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("错误", $"保存失败: {ex.Message}", "确定");
                }
            }
            else
            {
                // 使用文件名查找对应的文件路径
                var data = _jsonData.FirstOrDefault(d => d.FileName == _editingKey);
                if (data == null)
                {
                    EditorUtility.DisplayDialog("错误", $"找不到文件 '{_editingKey}'", "确定");
                    return;
                }
                
                try
                {
                    // 验证 JSON 格式
                    try
                    {
                        JsonConvert.DeserializeObject(_editingValue);
                    }
                    catch
                    {
                        if (!EditorUtility.DisplayDialog("警告", 
                            "内容可能不是有效的 JSON 格式，是否继续保存？", "继续", "取消"))
                        {
                            return;
                        }
                    }
                    
                    File.WriteAllText(data.FilePath, _editingValue);
                    Debug.Log($"已保存文件: {data.FilePath}");
                    RefreshData();
                    _isEditing = false;
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("错误", $"保存失败: {ex.Message}", "确定");
                }
            }
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        
        [Serializable]
        private class PlayerPrefsData
        {
            public string Key;
            public string Value;
            public string Type;
        }
        
        [Serializable]
        private class JsonData
        {
            public string FileName;
            public string FilePath;
            public string Content;
            public long FileSize;
        }
    }
}

