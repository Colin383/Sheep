using System;
using System.Collections.Generic;
using System.Linq;
using Bear.Logger;
using Config;
using Game;
using Game.ConfigModule;
using UnityEngine;

/// <summary>
/// 用于处理一些 Level 相关的数据
/// </summary>
public partial class LevelCtrl : MonoBehaviour, IDebuger
{
    private bool isReady = false;

    private List<LevelSort> levelsort;

    // 关卡顺序数据，只能从这里获取
    public List<LevelSort> LevelSorts { get => levelsort; }

    /// <summary>
    /// LevelSortId!!! 名字是历史遗留问题
    /// </summary>
    public int CurrentLevel
    {
        get
        {
            return DB.GameData.CurrentLevel;
        }
    }

    public int MaxLevel
    {
        get
        {
            return DB.GameData.MaxLevel;
        }
    }

    public bool IsLastLevel
    {
        get
        {
            if (levelsort == null || levelsort.Count == 0)
            {
                return true;
            }

            return CurrentLevel >= levelsort.Last().Id;
        }
    }

    /// <summary>
    /// 当前关卡对应的 LevelSort（根据当前关卡 Id）。
    /// </summary>
    public LevelSort CurrentLevelSort
    {
        get
        {
            if (levelsort == null || levelsort.Count == 0)
            {
                this.LogError("[LevelCtrl] CurrentLevelSort: levelsort not initialized.");
                return null;
            }

            int minId = levelsort.First().Id;
            int maxId = levelsort.Last().Id;
            int currentId = Math.Clamp(CurrentLevel, minId, maxId);

            var sort = levelsort.Find(d => d.Id == currentId);
            if (sort == null)
            {
                this.LogError($"[LevelCtrl] CurrentLevelSort: LevelSort not found for id = {currentId}, fallback to last.");
                sort = levelsort.Last();
            }

            return sort;
        }
    }

    /// <summary>
    /// 运行时关卡数据记录，不做缓存
    /// </summary>
    public LevelRuntimeState CurrentLevelState;

    public void Init()
    {
        if (isReady)
            return;

        // 数据加载配置
        levelsort = ConfigManager.Instance.Tables.TbLevelSort.DataList;
        isReady = true;

        CurrentLevelState = new LevelRuntimeState();
    }

    // 默认第一关
    private void InitUnlockLevel()
    {
        if (!DB.GameData.UnlockLevels.Contains(1))
            DB.GameData.UnlockLevels.Add(1);
    }

    public void Victory()
    {
        int nextLevel = DB.GameData.CurrentLevel + 1;

        if (!IsPassed(CurrentLevel))
        {
            DB.GameData.PassedLevels.Add(CurrentLevel);

            // 暂时没啥用，因为可以跳关
            DB.GameData.MaxLevel = Math.Max(DB.GameData.MaxLevel, nextLevel);

            // 打点需要，记录最好关卡数据
            GameAttribute.Instance.SetLevelId(DB.GameData.CurrentLevel, true);
        }

        // 通关当前关卡，解锁下一关
        if (!DB.GameData.UnlockLevels.Contains(nextLevel))
        {
            DB.GameData.UnlockLevels.Add(nextLevel);
        }

        if (nextLevel <= levelsort.Last().Id)
            DB.GameData.CurrentLevel = nextLevel;

        DB.GameData.Save();

        this.Log("----- Victroy currentLevel" + DB.GameData.CurrentLevel);

    }

    public LevelSort GetLevelSortById(int id)
    {
        if (levelsort == null || levelsort.Count == 0)
        {
            this.LogError("[LevelCtrl] GetLevelSortById: levelsort is not initialized.");
            return null;
        }

        var sort = levelsort.Find(d => d.Id == id);
        if (sort == null)
        {
            this.LogError($"[LevelCtrl] GetLevelSortById: LevelSort not found, id = {id}.");
        }

        return sort;
    }

    /// <summary>
    /// 是否已解锁
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool IsUnlock(int id)
    {
        return DB.GameData.UnlockLevels.Contains(id);
    }

    /// <summary>
    /// 是否已通关
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool IsPassed(int id)
    {
        return DB.GameData.PassedLevels.Contains(id);
    }


    /// <summary>
    /// 解锁关卡
    /// </summary>
    /// <param name="id"></param>
    public void UnlockLevel(int id)
    {
        if (IsUnlock(id))
            return;

        DB.GameData.UnlockLevels.Add(id);
        DB.GameData.Save();
    }

    /// <summary>
    /// For Test
    /// </summary>
    /// <param name="id"></param>
    public void SetCurrentLevelId(int id)
    {
        if (levelsort == null || levelsort.Count == 0)
        {
            DB.GameData.CurrentLevel = id;
        }
        else
        {
            int minId = levelsort.First().Id;
            int maxId = levelsort.Last().Id;
            DB.GameData.CurrentLevel = Math.Clamp(id, minId, maxId);
        }

        DB.GameData.Save();
    }

    /// <summary>
    /// 销毁当前关卡（销毁自身 GameObject）
    /// </summary>
    public void DestroyLevel()
    {
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
    }

    private void OnDestroy()
    {
        ClearSpawned();
    }
}
