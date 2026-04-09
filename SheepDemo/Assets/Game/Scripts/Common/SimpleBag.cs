using System.Collections.Generic;
using Bear.EventSystem;
using Config.Game;
using Game.Events;
using Unity.VisualScripting;
using UnityEngine;

namespace GameCommon
{
    /// <summary>
    /// 获得方式
    /// </summary>
    public enum RewardType
    {
        Ad,
        Purchase,
        Other
    }

    /// <summary>
    /// 消耗类型
    /// </summary>
    public enum CostType
    {
        //道具使用
        Hint,

        Other
    }

    /// <summary>
    /// 简单背包管理器
    /// 用于控制 DB.GameData.GameTools，新增删减道具数量
    /// </summary>
    public class SimpleBag : IEventSender
    {
        /// <summary>
        /// 发送道具更新事件
        /// </summary>
        private void DispatchPropUpdateEvent(GameProps prop, int oldCount, int newCount)
        {
            if (oldCount != newCount)
            {
                this.DispatchEvent(Witness<UpdatePropEvent>._, prop, oldCount, newCount);
            }
        }

        /// <summary>
        /// 获取指定道具的数量
        /// </summary>
        /// <param name="toolName">道具名称</param>
        /// <returns>道具数量，如果不存在返回 0</returns>
        public int GetToolCount(string toolName)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogWarning("[SimpleBag] Tool name is null or empty.");
                return 0;
            }

            var gameData = DB.GameData;
            if (gameData == null)
            {
                Debug.LogError("[SimpleBag] GameData is null.");
                return 0;
            }

            if (gameData.GameTools == null)
            {
                return 0;
            }

            return gameData.GameTools.TryGetValue(toolName, out int count) ? count : 0;
        }

        /// <summary>
        /// 获取指定道具的数量（使用枚举）
        /// </summary>
        /// <param name="tool">道具枚举</param>
        /// <returns>道具数量，如果不存在返回 0</returns>
        public int GetToolCount(GameProps tool)
        {
            return GetToolCount(tool.ToString());
        }

        /// <summary>
        /// 添加道具（如果道具不存在则新增，存在则增加数量）
        /// </summary>
        /// <param name="toolName">道具名称</param>
        /// <param name="count">要添加的数量</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>添加后的总数量</returns>
        public int AddTool(string toolName, int count, bool autoSave = true)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogWarning("[SimpleBag] Tool name is null or empty.");
                return 0;
            }

            if (count <= 0)
            {
                Debug.LogWarning($"[SimpleBag] Add count must be greater than 0. Tool: {toolName}, Count: {count}");
                return GetToolCount(toolName);
            }

            var gameData = DB.GameData;
            if (gameData == null)
            {
                Debug.LogError("[SimpleBag] GameData is null.");
                return 0;
            }

            if (gameData.GameTools == null)
            {
                gameData.GameTools = new Dictionary<string, int>();
            }

            int currentCount = GetToolCount(toolName);
            int newCount = currentCount + count;
            gameData.GameTools[toolName] = newCount;

            // 尝试转换为 GameProps 并发送事件
            if (System.Enum.TryParse<GameProps>(toolName, out GameProps prop))
            {
                DispatchPropUpdateEvent(prop, currentCount, newCount);
            }

            if (autoSave)
            {
                DB.SaveGameData();
            }

            Debug.Log($"[SimpleBag] Added {count} {toolName}. Total: {newCount}");
            return newCount;
        }

        /// <summary>
        /// 添加道具（使用枚举）
        /// </summary>
        /// <param name="tool">道具枚举</param>
        /// <param name="count">要添加的数量</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>添加后的总数量</returns>
        public int AddTool(GameProps tool, int count, RewardType type, bool autoSave = true)
        {
            int oldCount = GetToolCount(tool);
            int newCount = AddTool(tool.ToString(), count, false); // 不自动保存，避免重复保存
            // 直接发送事件（因为 toolName 一定能转换为 GameProps）
            DispatchPropUpdateEvent(tool, oldCount, newCount);
            if (autoSave)
            {
                DB.SaveGameData();
            }

            // 打点需要 ================================================
            if (tool == GameProps.Tips)
            {
                var toolKey = tool.ToString();
                switch (type)
                {
                    case RewardType.Ad:
                        AddOrIncrement(DB.GameData.AdGetTools, toolKey, count);
                        // GameSDKService.Instance.UpdateNonIAPCoin();
                        break;
                    case RewardType.Purchase:
                        AddOrIncrement(DB.GameData.PurchaseGetTools, toolKey, count);
                        // GameSDKService.Instance.UpdateIAPCoin();
                        break;
                    default:
                        break;
                }

                GameSDKService.Instance.BulbsGet(count, oldCount, newCount, type);
                // GameSDKService.Instance.UpdateCoin();
            }

            GameSDKService.Instance.EarnVirtualCurrentcy(tool, count, type, newCount);

            return newCount;
        }

        /// <summary>
        /// 在打点用字典中累加数量：不存在则添加，存在则累加。
        /// </summary>
        private static void AddOrIncrement(Dictionary<string, int> dict, string key, int count)
        {
            if (dict == null)
            {
                return;
            }

            if (!dict.TryAdd(key, count))
            {
                dict[key] += count;
            }
        }

        /// <summary>
        /// 移除道具（减少数量，如果数量为 0 或负数则删除该道具）
        /// </summary>
        /// <param name="toolName">道具名称</param>
        /// <param name="count">要移除的数量</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>移除后的数量，如果道具被删除返回 0</returns>
        public int RemoveTool(string toolName, int count, bool autoSave = true)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogWarning("[SimpleBag] Tool name is null or empty.");
                return 0;
            }

            if (count <= 0)
            {
                Debug.LogWarning($"[SimpleBag] Remove count must be greater than 0. Tool: {toolName}, Count: {count}");
                return GetToolCount(toolName);
            }

            var gameData = DB.GameData;
            if (gameData == null)
            {
                Debug.LogError("[SimpleBag] GameData is null.");
                return 0;
            }

            if (gameData.GameTools == null)
            {
                Debug.LogWarning($"[SimpleBag] GameTools is null. Cannot remove {toolName}.");
                return 0;
            }

            int currentCount = GetToolCount(toolName);
            if (currentCount <= 0)
            {
                Debug.LogWarning($"[SimpleBag] Tool {toolName} does not exist or count is 0.");
                return 0;
            }

            int newCount = currentCount - count;
            if (newCount <= 0)
            {
                // 数量为 0 或负数，删除该道具
                gameData.GameTools.Remove(toolName);
                newCount = 0;
                Debug.Log($"[SimpleBag] Removed all {toolName}. (Removed {currentCount})");
            }
            else
            {
                gameData.GameTools[toolName] = newCount;
                Debug.Log($"[SimpleBag] Removed {count} {toolName}. Remaining: {newCount}");
            }

            // 尝试转换为 GameProps 并发送事件
            if (System.Enum.TryParse<GameProps>(toolName, out GameProps prop))
            {
                DispatchPropUpdateEvent(prop, currentCount, newCount);
            }

            if (autoSave)
            {
                DB.SaveGameData();
            }

            return newCount;
        }

        /// <summary>
        /// 移除道具（使用枚举）
        /// </summary>
        /// <param name="tool">道具枚举</param>
        /// <param name="count">要移除的数量</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>移除后的数量，如果道具被删除返回 0</returns>
        public int RemoveTool(GameProps tool, int count, CostType type, bool autoSave = true)
        {
            // 直接调用 string 版本，事件会在 string 版本中发送（因为 tool.ToString() 一定能转换回 GameProps）
            int newCount = RemoveTool(tool.ToString(), count, autoSave);

            // 打点需要 ================================================
            if (tool == GameProps.Tips)
            {
                int oldCount = newCount + count;
                GameSDKService.Instance.BulbsCost(count, oldCount, newCount, type);
            }

            // 消耗打点
            GameSDKService.Instance.SpendVirtualCurrentcy(tool, count, type, newCount);

            return newCount;
        }

        /// <summary>
        /// 设置道具数量（直接设置，不累加）
        /// </summary>
        /// <param name="toolName">道具名称</param>
        /// <param name="count">要设置的数量</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>设置后的数量</returns>
        public int SetToolCount(string toolName, int count, bool autoSave = true)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogWarning("[SimpleBag] Tool name is null or empty.");
                return 0;
            }

            if (count < 0)
            {
                Debug.LogWarning($"[SimpleBag] Count cannot be negative. Tool: {toolName}, Count: {count}");
                count = 0;
            }

            var gameData = DB.GameData;
            if (gameData == null)
            {
                Debug.LogError("[SimpleBag] GameData is null.");
                return 0;
            }

            if (gameData.GameTools == null)
            {
                gameData.GameTools = new Dictionary<string, int>();
            }

            int oldCount = GetToolCount(toolName);
            int newCount = count;

            if (count == 0)
            {
                // 数量为 0，删除该道具
                gameData.GameTools.Remove(toolName);
                Debug.Log($"[SimpleBag] Set {toolName} count to 0 (removed).");
            }
            else
            {
                gameData.GameTools[toolName] = count;
                Debug.Log($"[SimpleBag] Set {toolName} count to {count}.");
            }

            // 尝试转换为 GameProps 并发送事件
            if (System.Enum.TryParse<GameProps>(toolName, out GameProps prop))
            {
                DispatchPropUpdateEvent(prop, oldCount, newCount);
            }

            if (autoSave)
            {
                DB.SaveGameData();
            }

            return count;
        }

        /// <summary>
        /// 设置道具数量（使用枚举）
        /// </summary>
        /// <param name="tool">道具枚举</param>
        /// <param name="count">要设置的数量</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>设置后的数量</returns>
        public int SetToolCount(GameProps tool, int count, bool autoSave = true)
        {
            // 直接调用 string 版本，事件会在 string 版本中发送（因为 tool.ToString() 一定能转换回 GameProps）
            return SetToolCount(tool.ToString(), count, autoSave);
        }

        /// <summary>
        /// 删除道具（完全移除该道具）
        /// </summary>
        /// <param name="toolName">道具名称</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteTool(string toolName, bool autoSave = true)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogWarning("[SimpleBag] Tool name is null or empty.");
                return false;
            }

            var gameData = DB.GameData;
            if (gameData == null)
            {
                Debug.LogError("[SimpleBag] GameData is null.");
                return false;
            }

            if (gameData.GameTools == null)
            {
                return false;
            }

            int oldCount = GetToolCount(toolName);
            bool removed = gameData.GameTools.Remove(toolName);
            if (removed)
            {
                Debug.Log($"[SimpleBag] Deleted tool: {toolName}");

                // 尝试转换为 GameProps 并发送事件（数量变为0）
                if (System.Enum.TryParse<GameProps>(toolName, out GameProps prop))
                {
                    DispatchPropUpdateEvent(prop, oldCount, 0);
                }

                if (autoSave)
                {
                    DB.SaveGameData();
                }
            }
            else
            {
                Debug.LogWarning($"[SimpleBag] Tool {toolName} does not exist.");
            }

            return removed;
        }

        /// <summary>
        /// 删除道具（使用枚举）
        /// </summary>
        /// <param name="tool">道具枚举</param>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteTool(GameProps tool, bool autoSave = true)
        {
            // 直接调用 string 版本，事件会在 string 版本中发送（因为 tool.ToString() 一定能转换回 GameProps）
            return DeleteTool(tool.ToString(), autoSave);
        }

        /// <summary>
        /// 检查道具是否存在
        /// </summary>
        /// <param name="toolName">道具名称</param>
        /// <returns>是否存在</returns>
        public bool HasTool(string toolName)
        {
            return GetToolCount(toolName) > 0;
        }

        /// <summary>
        /// 检查道具是否存在（使用枚举）
        /// </summary>
        /// <param name="tool">道具枚举</param>
        /// <returns>是否存在</returns>
        public bool HasTool(GameProps tool)
        {
            return GetToolCount(tool) > 0;
        }

        /// <summary>
        /// 获取所有道具
        /// </summary>
        /// <returns>所有道具的字典副本</returns>
        public Dictionary<string, int> GetAllTools()
        {
            var gameData = DB.GameData;
            if (gameData == null || gameData.GameTools == null)
            {
                return new Dictionary<string, int>();
            }

            return new Dictionary<string, int>(gameData.GameTools);
        }

        /// <summary>
        /// 清空所有道具
        /// </summary>
        /// <param name="autoSave">是否自动保存，默认为 true</param>
        public void ClearAllTools(bool autoSave = true)
        {
            var gameData = DB.GameData;
            if (gameData == null)
            {
                Debug.LogError("[SimpleBag] GameData is null.");
                return;
            }

            if (gameData.GameTools == null)
            {
                return;
            }

            int count = gameData.GameTools.Count;
            gameData.GameTools.Clear();

            Debug.Log($"[SimpleBag] Cleared all tools. (Removed {count} tools)");

            if (autoSave)
            {
                DB.SaveGameData();
            }
        }

        /// <summary>
        /// 获取道具总数（所有道具的数量之和）
        /// </summary>
        /// <returns>道具总数</returns>
        public int GetTotalToolCount()
        {
            var gameData = DB.GameData;
            if (gameData == null || gameData.GameTools == null)
            {
                return 0;
            }

            int total = 0;
            foreach (var count in gameData.GameTools.Values)
            {
                total += count;
            }

            return total;
        }

        /// <summary>
        /// 获取道具种类数量（不同道具的数量）
        /// </summary>
        /// <returns>道具种类数量</returns>
        public int GetToolTypeCount()
        {
            var gameData = DB.GameData;
            if (gameData == null || gameData.GameTools == null)
            {
                return 0;
            }

            return gameData.GameTools.Count;
        }
    }
}
