using Bear.Logger;

/// <summary>
/// 记录当前关卡的一些运行时状态：
/// - 当前关卡 Id
/// - 当前关卡游玩总时长（所有尝试累计的秒数）
/// - 当前关卡本局游玩时长（当前尝试内的秒数）
/// - 当前关卡的重置次数
/// - 当前关卡的失败次数
/// 
/// 仅负责数据存储和简单累加，由外部（如 PlayCtrl）在合适的时机调用对应方法。
/// </summary>
public class LevelRuntimeState : IDebuger
{
    /// <summary>当前关卡 Id。</summary>
    public int LevelId { get; private set; }

    /// <summary>当前关卡 name。</summary>
    public string LevelPath { get; private set; }

    /// <summary>当前关卡游玩总时长（单位：秒），包括本局与之前所有尝试。</summary>
    public float TotalPlayTimeSeconds { get; private set; }

    /// <summary>当前关卡这一局游玩时长（单位：秒）。</summary> replay 就重置为 0
    public float CurrentAttemptTimeSeconds { get; private set; }

    /// <summary>当前关卡这一局游玩时长（单位：秒）。</summary> replay 不重置为 0
    public float CurrentLevelTimeSeconds { get; private set; }

    /// <summary>当前关卡的重置次数（主动重开次数）。</summary>
    public int ResetCount { get; private set; }

    /// <summary>当前关卡的失败次数（不含主动重开，视业务需要外部调用）。</summary>
    public int FailCount { get; private set; }

    /// <summary>
    /// 已经使用提示了
    /// </summary>
    public bool HasTips { get; private set; }

    /// <summary>
    /// 是否点击提示
    /// </summary>
    public bool HasClickTips { get; private set; }

    /// <summary>
    /// 切换到一个新的关卡时调用，重置所有统计数据。
    /// </summary>
    public void StartLevel(int levelId, string levelPath)
    {
        LevelId = levelId;
        LevelPath = levelPath;

        TotalPlayTimeSeconds = 0f;
        CurrentAttemptTimeSeconds = 0f;
        CurrentLevelTimeSeconds = 0f;
        ResetCount = 0;
        FailCount = 0;

        HasTips = false;
        HasClickTips = false;

        TryToAddPlayCount();
    }

    /// <summary>
    /// 开始一局新的尝试时调用，只重置当前时间
    /// </summary>
    public void ResetAttemptPlayTime()
    {
        CurrentAttemptTimeSeconds = 0f;
    }

    /// <summary>
    /// 每帧或固定时间间隔调用，用于累加当前关卡的总时长和本局时长。
    /// deltaSeconds 通常为 Time.deltaTime 或 Time.unscaledDeltaTime。
    /// </summary>
    public void Tick(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
            return;

        TotalPlayTimeSeconds += deltaSeconds;
        CurrentAttemptTimeSeconds += deltaSeconds;
        CurrentLevelTimeSeconds += deltaSeconds;
    }


    /// <summary>
    /// 对应关卡游戏时长
    /// </summary>
    /// <param name="duration"></param>
    private void TryToAddPlayDuration(long duration)
    {
        DB.GameData.LevelPlayDuration.TryAdd(LevelPath, 0);
        DB.GameData.LevelPlayDuration[LevelPath] += duration;
        DB.GameData.Save();
    }

    /// <summary>
    /// 记录档次关卡时间
    /// </summary>
    public void RecordLevelDuration()
    {
        TryToAddPlayDuration(GetCurrentAttemptTimeSeconds());
    }

    public long GetCurrentAttemptTimeSeconds()
    {
        // 历史命名保留不变，返回值单位为毫秒。
        if (CurrentAttemptTimeSeconds <= 0f)
            return 0L;

        return (long)(CurrentAttemptTimeSeconds * 1000f);
    }

    public long GetCurrentLevelTimeSeconds()
    {
        // 历史命名保留不变，返回值单位为毫秒。
        if (CurrentLevelTimeSeconds <= 0f)
            return 0L;

        return (long)(CurrentLevelTimeSeconds * 1000f);
    }


    /// <summary>
    /// 记录一次主动重开当前关卡。
    /// </summary>
    public void RecordReset()
    {
        ResetCount++;

        TryToAddReviveCount();
    }

    /// <summary>
    /// 记录一次当前关卡的失败（不含主动重开，是否包括由外部业务决定）。
    /// </summary>
    public void RecordFail()
    {
        FailCount++;

        TryToAddReviveCount();
    }

    public void SwitchTips(bool has)
    {
        HasTips = has;
    }

    public void SwitchClickTips(bool hasClick)
    {
        HasClickTips = hasClick;
    }

    /// <summary>
    /// 重置当前关卡的所有统计数据，但保留 LevelId 不变。
    /// </summary>
    public void ResetData()
    {
        TotalPlayTimeSeconds = 0f;
        CurrentLevelTimeSeconds = 0;
        CurrentAttemptTimeSeconds = 0f;
        ResetCount = 0;
        FailCount = 0;

        HasTips = false;
        HasClickTips = false;

        this.Log("Reset state data");
    }

    /// <summary>
    /// 记录当前关卡游玩次数，成功计数
    /// </summary>
    public void TryToAddPlayCount()
    {
        DB.GameData.LevelPlayCount.TryAdd(LevelPath, 0);
        DB.GameData.LevelPlayCount[LevelPath]++;
        DB.GameData.Save();
    }

    /// <summary>
    /// 记录当前关卡游玩次数，成功计数
    /// </summary>
    public void TryToAddSuccessCount()
    {
        DB.GameData.LevelSuccessCount.TryAdd(LevelPath, 0);
        DB.GameData.LevelSuccessCount[LevelPath]++;

        // 插屏关卡胜利次数记录
        DB.GameData.InterstitialLevelSuccessCount++;

        DB.GameData.Save();
    }

    /// <summary>
    /// 记录当前关卡游玩次数
    /// </summary>
    private void TryToAddReviveCount()
    {
        DB.GameData.LevelReviveCount.TryAdd(LevelPath, 0);
        DB.GameData.LevelReviveCount[LevelPath]++;
        DB.GameData.Save();
    }
}

