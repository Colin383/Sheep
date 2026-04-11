using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bear.EventSystem;
using Bear.Logger;
using Bear.SaveModule;
using Bear.UI;
using DG.Tweening;
using Game;
using Game.Common;
using Game.ConfigModule;
using Game.HotReload;
using Game.Scripts.Common;
using GF;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SDK.Remote;

// 当前场景，用状态机，处理一些资源加载或者配置初始化的功能设计
public class LoadManager : MonoBehaviour, IDebuger
{
    public Booster booster;
    [SerializeField] private Image ProcessFill;
    [SerializeField] private TextMeshProUGUI processTxt;

    private Queue<Func<IEnumerator<float>>> functionQueue = new Queue<Func<IEnumerator<float>>>();
    private Coroutine currentCoroutine;
    private EventSubscriber _hotReloadSubscriber;
    private Tweener _processFillTweener;

    // private bool isPlayCompleted = false;

    public void Awake()
    {
        EnqueueFunction(EnterGame);

        EnqueueFunction(HotReload);

        // 加载配置表
        EnqueueFunction(LoadConfigAsync);

        // On Finshed
        EnqueueFunction(OnPostProcess);

        ProcessQueue();
    }

    private void OnDestroy()
    {
        _processFillTweener?.Kill();
        EventsUtils.ResetEvents(ref _hotReloadSubscriber);
    }

    private IEnumerator<float> EnterGame()
    {
        // 这里需要注意一下 iap 的初始化进度是不是很快
        booster.Init();
        GameManager.Instance.Init();
        DBManager.Instance.Initialize();
        InitLanguage();

        DB.GameData.PlayCount++;
        DB.GameData.Save();

        yield return 1;
    }

    private IEnumerator<float> HotReload()
    {
        var packageName = GameSettingData.Setting.defaultPackageName;
        BuiltinPackageElement packageElement = GameSettingData.Setting.GetDefaultPackageElement(packageName);

        // 初始化事件订阅
        EventsUtils.ResetEvents(ref _hotReloadSubscriber);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.PatchStepsChange>(OnPatchStepsChange);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.DownloadUpdate>(OnDownloadUpdate);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.FoundUpdateFiles>(OnFoundUpdateFiles);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.InitializeFailed>(OnInitializeFailed);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.PackageVersionRequestFailed>(OnPackageVersionRequestFailed);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.PackageManifestUpdateFailed>(OnPackageManifestUpdateFailed);
        _hotReloadSubscriber.Subscribe<HotReloadEvents.WebFileDownloadFailed>(OnWebFileDownloadFailed);

        // 初始化热更新流程
        HotReloadCtrl.Instance.Init(packageName, packageElement.playMode);

        // 等待热更新完成
        float progress = 0f;
        while (!HotReloadCtrl.Instance.IsFinish || !booster.IsReady)
        {
            // 可以根据需要更新进度
            yield return progress;
            progress = Mathf.Min(progress + 0.01f, 0.99f);
        }

        // 热更新完成，重置事件订阅
        EventsUtils.ResetEvents(ref _hotReloadSubscriber);
        yield return 1f;
    }

    #region HotReload Event Handlers
    private void OnPatchStepsChange(HotReloadEvents.PatchStepsChange evt)
    {
        Debug.Log($"[LoadManager] HotReload Step: {evt.Tips}");
        processTxt.text = evt.Tips;
    }

    private void OnDownloadUpdate(HotReloadEvents.DownloadUpdate evt)
    {
        if (evt.TotalDownloadCount > 0)
        {
            float progress = (float)evt.CurrentDownloadCount / evt.TotalDownloadCount;
            processTxt.text = $"[LoadManager] Download Progress: {evt.CurrentDownloadCount}/{evt.TotalDownloadCount} ({progress * 100:F1}%)";
        }
    }

    private void OnFoundUpdateFiles(HotReloadEvents.FoundUpdateFiles evt)
    {
        processTxt.text = $"[LoadManager] Found {evt.TotalCount} files to download, total size: {evt.TotalSizeBytes / 1024 / 1024} MB";
    }

    private void OnInitializeFailed(HotReloadEvents.InitializeFailed evt)
    {
        processTxt.color = Color.red;
        processTxt.text = "[LoadManager] HotReload Initialize Failed!";
        this.LogError("[LoadManager] HotReload Initialize Failed!");
    }

    private void OnPackageVersionRequestFailed(HotReloadEvents.PackageVersionRequestFailed evt)
    {
        processTxt.color = Color.red;
        processTxt.text = "[LoadManager] HotReload Initialize Failed!";
        this.LogError("[LoadManager] HotReload Package Version Request Failed!");
    }

    private void OnPackageManifestUpdateFailed(HotReloadEvents.PackageManifestUpdateFailed evt)
    {
        processTxt.color = Color.red;
        processTxt.text = "[LoadManager] HotReload Initialize Failed!";
        this.LogError("[LoadManager] HotReload Package Manifest Update Failed!");
    }

    private void OnWebFileDownloadFailed(HotReloadEvents.WebFileDownloadFailed evt)
    {
        processTxt.color = Color.red;
        processTxt.text = "[LoadManager] HotReload Initialize Failed!";
        this.LogError($"[LoadManager] HotReload Download Failed: {evt.FileName}, Error: {evt.Error}");
    }
    #endregion

    /// <summary>
    /// 异步加载配置表
    /// </summary>
    private IEnumerator<float> LoadConfigAsync()
    {

        // 先按 LoadManager 风格执行 RemoteConfig 的加载协程
/*         var remoteEnumerator = RemoteConfigService.UpdateRemoteConfigsForLoadManager();
        while (remoteEnumerator.MoveNext())
        {
            yield return remoteEnumerator.Current;
        } */

        // 确保 ConfigManager 实例存在
        if (ConfigManager.Instance == null)
        {
            Debug.LogError("[LoadManager] ConfigManager instance is null!");
            yield return 1f;
            yield break;
        }

        // 如果已经初始化，直接返回
        if (ConfigManager.Instance.IsInitialized)
        {
            Debug.Log("[LoadManager] Config tables already initialized.");
            yield return 1f;
            yield break;
        }

        bool isLoading = true;
        bool loadSuccess = false;

        // 启动异步加载
        ConfigManager.Instance.InitializeAsync(() =>
        {
            isLoading = false;
            loadSuccess = ConfigManager.Instance.IsInitialized;
        });

        // 等待加载完成，并报告进度
        while (isLoading)
        {
            float progress = ConfigManager.Instance.LoadProgress;
            yield return progress;
        }

        // 加载完成
        if (loadSuccess)
        {
            Debug.Log("[LoadManager] Config tables loaded successfully.");
            yield return 1f;
        }
        else
        {
            Debug.LogError("[LoadManager] Failed to load config tables!");
            yield return 1f;
        }
    }

    // 最后处理模块
    private IEnumerator<float> OnPostProcess()
    {
        InitUIManager();

        // 初始化游戏设置
        // GameManager.Instance.Init();
        AudioManager.Init();

        // 内购配置初始化
        GameManager.Instance.Purchase.InitConfig();

        // 打点占位
        GameSDKService.Instance.UpdateBPlay();
        GameSDKService.Instance.UpdateBLevel();

        // 进入游戏
        GameManager.Instance.ReadyToPlay();

        // 等待 1 秒，确保恢复列表已准备好后再手动同步恢复奖励。
        /*         float progress = 0f;
                var restoreDelayStart = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - restoreDelayStart < 1f)
                {
                    yield return progress;
                    progress = Mathf.Min(progress + 0.01f, 0.99f);
                }

                GameManager.Instance.Purchase.SyncRestore(); */

        yield return 1f;
    }

    private void InitLanguage()
    {
        var key = DB.GameSetting.CurrentLanguageKeyCode;
#if UNITY_EDITOR
        LocalizationManager.CurrentLanguageCode = key;
#endif
        // 初次进入, 根据系统语言设置默认语言
        if (DB.GameData.PlayCount == 0)
        {
            key = LocalizatioinUtils.GetCodeFromSystemCode();

#if UNITY_IOS
            // 判断当前语言
            if (key == "zh-CN")
            {
                // 注意: 此处一定要将 RememberLanguage 设置为 false, 不会保存到 PlayerPrefs 中, 方便之后切语言回退
                LocalizationManager.SetLanguageAndCode(
                    LanguageName: "Chinese (Traditional)",
                    LanguageCode: "zh-TW",
                    RememberLanguage: false);
            }
            else {
                LocalizationManager.CurrentLanguageCode = key;
            }
#else 
                LocalizationManager.CurrentLanguageCode = key;
#endif
                DB.GameSetting.CurrentLanguageKeyCode = key;
                DB.GameSetting.Save();
            }
        }

    private void InitUIManager()
    {
        UIManager.Instance.Initialize();
        // YooAssetUILoader yooAssetLoader = new YooAssetUILoader();
        ResourcesUILoader newLoader = new ResourcesUILoader("");
        // UIManager.Instance.RegisterLoader(yooAssetLoader, 4);
        UIManager.Instance.RegisterLoader(newLoader, 5);

        ObjectPoolManager.Instance.RegisterPool<SystemTips>(SystemTips.Create, 2, 5);
    }

    // 添加函数到队列
    public void EnqueueFunction(Func<IEnumerator<float>> function)
    {
        functionQueue.Enqueue(function);

        // 如果当前没有在执行，开始执行队列
        if (currentCoroutine == null)
        {
            currentCoroutine = StartCoroutine(ProcessQueue());
        }
    }

    // 处理队列中的函数
    private IEnumerator ProcessQueue()
    {
        while (functionQueue.Count > 0)
        {
            Func<IEnumerator<float>> function = functionQueue.Dequeue();
            IEnumerator<float> coroutine = function();
            ProcessFill.fillAmount = 0;

            // 执行协程并获取返回值
            while (coroutine.MoveNext())
            {
                float value = coroutine.Current;

                // 更新进度条
                if (ProcessFill != null)
                {
                    _processFillTweener?.Kill();
                    if (ProcessFill.fillAmount < value)
                    {
                        _processFillTweener = DOTween.To(() => ProcessFill.fillAmount, d =>
                        {
                            if (ProcessFill != null)
                                ProcessFill.fillAmount = d;
                        }, value, Mathf.Max(0.02f, value - ProcessFill.fillAmount)).SetTarget(ProcessFill);
                    }
                    else
                    {
                        ProcessFill.fillAmount = value;
                        _processFillTweener = null;
                    }
                }

                yield return null;
            }
        }

        currentCoroutine = null;
        LoadSceneAsync("Main");
    }

    // 异步加载场景（推荐）
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        /*         while(!isPlayCompleted)
                    yield return null;
                 */
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            // 获取加载进度值（0-0.9 映射到 0-1）
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 更新进度条
            if (ProcessFill != null)
            {
                ProcessFill.fillAmount = progress;
            }

            // 当进度达到 0.9 时，允许场景激活
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
