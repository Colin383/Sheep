using Config.Game;

public partial class GameSDKService
{
    public void UpdateIAPCoin()
    {
        /* DB.GameData.PurchaseGetTools.TryGetValue(GameProps.Tips.ToString(), out int count);
        SetUserProperty("iap_coin", count.ToString()); */
    }

    public void UpdateNonIAPCoin()
    {
        /*         DB.GameData.AdGetTools.TryGetValue(GameProps.Tips.ToString(), out int count);
                SetUserProperty("noniap_coin", count.ToString()); */
    }

    public void UpdateCoin()
    {
        /*         DB.GameData.GameTools.TryGetValue(GameProps.Tips.ToString(), out int count);
                SetUserProperty("coin", count.ToString()); */
    }

    // 当前通关总数    
    public int GetLevelPassedCount()
    {
        int count = 0;
        foreach (var pair in DB.GameData.LevelSuccessCount)
        {
            count += pair.Value;
        }
        return count;
    }

    /// <summary>
    /// 获取最大通过关卡
    /// </summary>
    /// <returns></returns>
    public int GetMaxPassedLevel()
    {
        var passed = DB.GameData.PassedLevels;
        if (passed == null || passed.Count == 0)
            return 0;
        int max = passed[0];
        for (int i = 1; i < passed.Count; i++)
        {
            if (passed[i] > max)
                max = passed[i];
        }
        return max;
    }

    public void UpdateBPlay()
    {
        int count = GetLevelPassedCount();
        SetUserProperty("b_play", count.ToString());
    }

    public void UpdateBLevel()
    {
        SetUserProperty("b_level",GetMaxPassedLevel().ToString());
    }
}