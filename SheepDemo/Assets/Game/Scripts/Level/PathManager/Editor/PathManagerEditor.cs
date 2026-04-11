#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// PathManager 的自定义编辑器，支持在 Scene 视图中直接点击选择网格单元格作为路径
    /// </summary>
    [CustomEditor(typeof(PathManager))]
    public class PathManagerEditor : UnityEditor.Editor
    {
        private PathManager _pathManager;
        private Vector2Int _hoverCell = new Vector2Int(-1, -1);
        private bool _isEditing = true;

        private void OnEnable()
        {
            _pathManager = target as PathManager;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 编辑模式开关
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene 编辑模式", EditorStyles.boldLabel, GUILayout.Width(100));
            _isEditing = EditorGUILayout.Toggle(_isEditing, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            if (_isEditing)
            {
                EditorGUILayout.HelpBox(
                    "• 左键点击单元格：切换路径状态\n" +
                    "• Shift+左键点击：批量添加（框选区域）\n" +
                    "• Ctrl+左键点击：批量移除（框选区域）",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // 绘制默认 Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("路径操作", EditorStyles.boldLabel);

            // 快捷按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空路径", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要删除所有路径单元格吗？", "确定", "取消"))
                {
                    Undo.RecordObject(_pathManager, "清空路径");
                    _pathManager.ClearPath();
                    EditorUtility.SetDirty(_pathManager);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 显示路径单元格列表
            EditorGUILayout.LabelField($"路径单元格 ({_pathManager.PathCellCount})", EditorStyles.boldLabel);

            if (_pathManager.PathCellCount > 0)
            {
                EditorGUI.indentLevel++;
                var cells = _pathManager.PathCells;
                for (int i = 0; i < cells.Count && i < 20; i++) // 最多显示20个
                {
                    EditorGUILayout.LabelField($"[{i}] ({cells[i].x}, {cells[i].y})");
                }
                if (cells.Count > 20)
                {
                    EditorGUILayout.LabelField($"... 还有 {cells.Count - 20} 个单元格");
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("路径为空，请在 Scene 视图中点击单元格添加", MessageType.Warning);
            }

            // 悬停单元格信息
            if (_isEditing && _hoverCell.x >= 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("悬停单元格", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"坐标: ({_hoverCell.x}, {_hoverCell.y})");
                bool isPath = _pathManager.IsPathCell(_hoverCell);
                EditorGUILayout.LabelField($"状态: {(isPath ? "路径 ✓" : "非路径")}");
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (!_isEditing || _pathManager == null)
                return;

            Event e = Event.current;

            // 计算鼠标所在的单元格
            UpdateHoverCell(e);

            // 绘制悬停高亮
            DrawHoverHighlight();

            // 处理输入
            HandleSceneInput(e);
        }

        /// <summary>
        /// 更新悬停单元格
        /// </summary>
        private void UpdateHoverCell(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane gridPlane = new Plane(Vector3.up, _pathManager.transform.position);

            if (gridPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                Vector2Int cell = WorldToCell(worldPos);

                if (IsValidCell(cell))
                {
                    _hoverCell = cell;
                }
                else
                {
                    _hoverCell = new Vector2Int(-1, -1);
                }
            }
            else
            {
                _hoverCell = new Vector2Int(-1, -1);
            }
        }

        /// <summary>
        /// 绘制悬停高亮
        /// </summary>
        private void DrawHoverHighlight()
        {
            if (_hoverCell.x < 0)
                return;

            bool isPath = _pathManager.IsPathCell(_hoverCell);
            Color highlightColor = isPath
                ? new Color(1f, 0f, 0f, 0.5f)  // 红色表示可移除
                : new Color(0f, 1f, 0f, 0.5f); // 绿色表示可添加

            Handles.color = highlightColor;

            Vector3 center = GetCellCenter(_hoverCell);
            Vector3 size = new Vector3(_pathManager.CellSize * 0.9f, 0.05f, _pathManager.CellSize * 0.9f);

            // 绘制高亮框
            Handles.DrawWireCube(center, size);

            // 填充半透明
            Handles.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.2f);
            Handles.CubeHandleCap(0, center, Quaternion.identity, _pathManager.CellSize * 0.45f, EventType.Repaint);

            // 绘制坐标标签
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            string label = isPath ? $"[{_hoverCell.x},{_hoverCell.y}] 点击移除" : $"[{_hoverCell.x},{_hoverCell.y}] 点击添加";
            Handles.Label(center + Vector3.up * 0.5f, label, style);
        }

        /// <summary>
        /// 处理 Scene 视图输入
        /// </summary>
        private void HandleSceneInput(Event e)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (_hoverCell.x >= 0)
                {
                    Undo.RecordObject(_pathManager, "修改路径");

                    if (e.shift)
                    {
                        // Shift+左键：批量添加（从上一个路径点连线到当前点）
                        AddPathLineTo(_hoverCell);
                    }
                    else if (e.control || e.command)
                    {
                        // Ctrl+左键：仅移除当前单元格
                        if (_pathManager.IsPathCell(_hoverCell))
                        {
                            _pathManager.RemovePathCell(_hoverCell);
                        }
                    }
                    else
                    {
                        // 普通左键：切换状态
                        _pathManager.TogglePathCell(_hoverCell.x, _hoverCell.y);
                    }

                    EditorUtility.SetDirty(_pathManager);
                    e.Use();
                }
            }

            // 键盘快捷键
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                {
                    // Delete：清空路径
                    if (_pathManager.PathCellCount > 0)
                    {
                        Undo.RecordObject(_pathManager, "清空路径");
                        _pathManager.ClearPath();
                        EditorUtility.SetDirty(_pathManager);
                        e.Use();
                    }
                }
            }
        }

        /// <summary>
        /// 添加从最后一个路径点到目标点的直线
        /// </summary>
        private void AddPathLineTo(Vector2Int targetCell)
        {
            if (_pathManager.PathCellCount == 0)
            {
                _pathManager.AddPathCell(targetCell);
                return;
            }

            var lastCell = _pathManager.GetPathCell(_pathManager.PathCellCount - 1);

            // 使用 Bresenham 直线算法添加路径点
            int x0 = lastCell.x, y0 = lastCell.y;
            int x1 = targetCell.x, y1 = targetCell.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                _pathManager.AddPathCell(x0, y0);

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        #region 辅助方法

        private Vector2Int WorldToCell(Vector3 worldPos)
        {
            Vector3 origin = GetGridOrigin();

            Vector3 localPos = worldPos - origin;
            int x = Mathf.FloorToInt(localPos.x / _pathManager.CellSize);
            int y = Mathf.FloorToInt(localPos.z / _pathManager.CellSize);

            return new Vector2Int(x, y);
        }

        private Vector3 GetGridOrigin()
        {
            Vector3 rootPos = _pathManager.transform.position;
            float halfW = _pathManager.GridWidth * 0.5f * _pathManager.CellSize;
            float halfH = _pathManager.GridHeight * 0.5f * _pathManager.CellSize;
            Vector2 offset = _pathManager.OriginOffset;
            float leftX = rootPos.x - halfW + offset.x;
            float bottomZ = rootPos.z - halfH + offset.y;
            return new Vector3(leftX, rootPos.y, bottomZ);
        }

        private bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < _pathManager.GridWidth
                && cell.y >= 0 && cell.y < _pathManager.GridHeight;
        }

        private Vector3 GetCellCenter(Vector2Int cell)
        {
            Vector3 origin = GetGridOrigin();

            return origin + new Vector3(
                (cell.x + 0.5f) * _pathManager.CellSize,
                0,
                (cell.y + 0.5f) * _pathManager.CellSize
            );
        }

        #endregion
    }
}
#endif
