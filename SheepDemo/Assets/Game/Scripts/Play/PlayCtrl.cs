using System;
using Bear.EventSystem;
using Bear.Fsm;
using Bear.Logger;
using Bear.UI;
using Config;
using Cysharp.Threading.Tasks;
using Game;
using Game.Common;
using Game.Events;
using Game.Level;
using Game.Play;
using Game.Scripts.Common;
using GameCommon;
using I2.Loc;
using UnityEngine;

/// <summary>
/// 关卡重置原因
/// </summary>
public enum GameResetType
{
    // 手动重置
    Manually,
    // 触发陷阱
    Failed,
}

/// <summary>
/// 
/// </summary>
public class PlayCtrl : Singleton<PlayCtrl>, IBearMachineOwner, IDebuger, IEventSender
{
    private StateMachine _machine;

    private EventSubscriber _subscriber;

    public EventSubscriber Subscriber => _subscriber;

    public SimpleBag Bag { get; private set; }

    public InterstitialAdPolicy InterstitialAdPolicy { get; private set; }

    #region Level
    private LevelCtrl _level;

    public LevelCtrl Level => _level;

    public GamePlayPanel @CurrentGamePlayPanel
    {
        private set;
        get;
    }

    public Transform SceneRoot;
    public BaseLevelCtrl LevelCtrl;
    public BaseLevelCtrl LevelPrefab;
    private const string LevelPath = "Level/{0}";

    #endregion

    private bool isReady = false;

    public void Init()
    {
        if (isReady)
            return;

        isReady = true;

        _level = new LevelCtrl();
        _level.Init();

        Bag = new SimpleBag();
        InterstitialAdPolicy = new InterstitialAdPolicy();

        _machine = new StateMachine(this);
        _machine.Inject(typeof(PlayCtrl_Start),
        typeof(PlayCtrl_Playing),
        typeof(PlayCtrl_Pause),
        typeof(PlayCtrl_Success),
        typeof(PlayCtrl_Failed));

        _machine.Apply(GetType());
        _machine.Enter(GamePlayStateName.START);

        StartPanel.Create();

        AddListener();
    }

    private void AddListener()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<SwitchGameStateEvent>(OnSwitchState);
        _subscriber.Subscribe<GameResetEvent>(OnGameResetEvent);

        _subscriber.Subscribe<EnterLevelEvent>(OnGameEnter);
        _subscriber.Subscribe<EnterNextLevelEvent>(OnEnterNextLevel);

        _subscriber.Subscribe<UseTipsEvent>(OnTipsUsed);
    }

    private void OnTipsUsed(UseTipsEvent evt)
    {
        Level.CurrentLevelState.SwitchTips(true);
    }

    private void OnGameEnter(EnterLevelEvent evt)
    {
        DestroyLevel();
        var sortId = evt.Data.Id;
        Level.SetCurrentLevel(sortId);

        var data = Level.CurrentLevelData;
        Level.CurrentLevelState.StartLevel(evt.Data.Id, data.Path);

        EnterLevelLoading.Create(() =>
        {
            CreateLevel(Level.CurrentLevelData.Path);
            _machine.Enter(GamePlayStateName.PLAYING);
        });

        TrackLevelStart(sortId);
        // 打点需要刷新 levelId
        GameAttribute.Instance.SetLevelId(DB.GameData.CurrentLevel);
    }

    private void OnEnterNextLevel(EnterNextLevelEvent evt)
    {
        var data = Level.CurrentLevelData;
        if (data == null)
        {
            DestroyLevel();
            // switch panel
            ClickTransformPanel.Create(() =>
            {
                ChoiceLevelPanel.Create();
            });
        }
        else
        {
            Level.CurrentLevelState.StartLevel(Level.CurrentLevel, data.Path);
            EnterLevelLoading.Create(() =>
            {
                DestroyLevel();
                CreateLevel(data.Path);
                _machine.Enter(GamePlayStateName.PLAYING);
            });
        }

        TrackLevelStart(Level.CurrentLevel);
        // 打点需要刷新 levelId
        GameAttribute.Instance.SetLevelId(DB.GameData.CurrentLevel);
    }

    public bool CheckState(string state)
    {
        return _machine.IsRunning(state);
    }

    private void OnSwitchState(SwitchGameStateEvent evt)
    {
        this.Log(evt.NewState);
        _machine.Enter(evt.NewState);
    }

    private void OnGameResetEvent(GameResetEvent evt)
    {
        if (evt.Type == GameResetType.Manually)
            Level.CurrentLevelState.RecordReset();
        else
            Level.CurrentLevelState.RecordFail();

        // 失败重试打点 ===========================================================
        GameSDKService.Instance.LevelEndEvent(GameSDKService.LevelEndTypeFailed);
        GameSDKService.Instance.LevelStartEvent(GameSDKService.LevelStartTypeReplay);
        // 重置局间时间
        Level.CurrentLevelState.ResetAttemptPlayTime();

        ResetGame().Forget();
    }

    private async UniTask ResetGame()
    {
        InterstitialAdHelper.TryToShowInterstitial(Config.Game.InterstitialPlacement.InGameFailed.ToString(), OnInterstitialCallback);

        await UniTask.WaitForSeconds(0.5f, ignoreTimeScale: true);

        // Show Ask 
        DestroyLevel();
        CreateLevel(Level.CurrentLevelData.Path);
        this.DispatchEvent(Witness<GamePlayPanelFadeInEvent>._);
        // 重置一下点击 Tips 的状态
        Level.CurrentLevelState.SwitchClickTips(false);

        _machine.Enter(GamePlayStateName.PAUSE);

        await UniTask.WaitForSeconds(1f, ignoreTimeScale: true);

        this.Log("-------------------------------- !!");

        _machine.Enter(GamePlayStateName.PLAYING);
    }

    private void OnInterstitialCallback(string placement, bool isSuc)
    {

    }

    /// <summary>
    /// LevelStart 打点
    /// </summary>
    /// <param name="sortId"></param>
    private void TrackLevelStart(int sortId)
    {
        // 打点 =============================================== 
        if (DB.GameData.PassedLevels.Contains(sortId))
        {
            GameSDKService.Instance.LevelStartEvent(GameSDKService.LevelStartTypeReplay);
        }
        else
        {
            GameSDKService.Instance.LevelStartEvent(GameSDKService.LevelStartTypeNewGame);
        }
    }

    /// <summary>
    /// 清理當前關卡
    /// </summary>
    public void DestroyLevel()
    {
        if (LevelCtrl == null)
            return;

        LevelCtrl.DestroyLevel();
        LevelCtrl = null;
        LevelPrefab = null;

        AudioManager.StopAllSound();
        Announce.CloseStraightly();
        GameManager.Instance.OpenCamera();
    }

    public void CreateLevel(string levelName)
    {
        if (LevelCtrl != null)
            return;

        GameManager.Instance.CloseCamera();

        // id, 测试关卡
        if (!LevelPrefab)
            LevelPrefab = Resources.Load<BaseLevelCtrl>(string.Format(LevelPath, levelName));

        if (!LevelPrefab)
        {
            this.LogError($"Level lost: {levelName}");
            return;
        }

        LevelCtrl = GameObject.Instantiate(LevelPrefab, SceneRoot);
        RefreshGamePanel();
    }

    /// <summary>
    /// 因为需求会有多种不同的 gamePanel，所以我们需要针对变化，设置变体
    /// </summary>
    private void RefreshGamePanel()
    {
        if (CurrentGamePlayPanel != null)
        {
            UIManager.Instance.DestroyUI(CurrentGamePlayPanel);
        }

        CurrentGamePlayPanel = GamePlayPanel.Create(LevelCtrl.GamePlayPanelName);
        CurrentGamePlayPanel.SetData(Level.CurrentLevel);
        CurrentGamePlayPanel.PlayShowPanelAnim().Forget();

        // TBC 关卡播放音效
        if (Level.IsLastLevel)
        {
            AudioManager.PlaySound("TBC");
        }
    }

    public void Update()
    {
        _machine?.Update();

        // 处理手机返回键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowExitPopup();
        }
    }

    /// <summary>
    /// 显示退出确认弹窗
    /// </summary>
    private void ShowExitPopup()
    {
        CommmonPopup.Create(
            title: LocalizationManager.GetTranslation("U_Quit_Title_01"),
            content: LocalizationManager.GetTranslation("U_Quit_Des_01"),
            onYes: () =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            },
            onNo: null
        );
    }

    public void OnDestroy()
    {
        _machine?.Dispose();
    }
}
