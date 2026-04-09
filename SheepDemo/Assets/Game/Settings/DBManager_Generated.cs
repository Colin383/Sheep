// =========================================
// 此文件由 DBManagerGenerator 自动生成
// 请勿手动修改此文件
// 如需重新生成，请使用菜单: Tools/Save Module/Generate DBManager Code
// 生成时间: 2026-03-05 19:32:50
// =========================================

using UnityEngine;
using Bear.SaveModule;

/// <summary>
/// DBManager 生成的静态数据访问类
/// 此文件由 DBManagerGenerator 自动生成
/// </summary>
public static partial class DB
{

    /// <summary>
    /// 获取 GameData 数据实例
    /// </summary>
    public static GameData GameData
    {
        get
        {
            return DBManager.Instance.Get<GameData>();
        }
    }

    /// <summary>
    /// 获取 GameSetting 数据实例
    /// </summary>
    public static GameSetting GameSetting
    {
        get
        {
            return DBManager.Instance.Get<GameSetting>();
        }
    }

    /// <summary>
    /// 保存指定类型的数据
    /// </summary>
    public static bool SaveGameData()
    {
        return DBManager.Instance.Save<GameData>();
    }

    public static bool SaveGameSetting()
    {
        return DBManager.Instance.Save<GameSetting>();
    }

}
