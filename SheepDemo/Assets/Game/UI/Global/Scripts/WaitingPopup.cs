using System;
using Bear.UI;
using Cysharp.Threading.Tasks;
using Game.ItemEvent;
using Guru.SDK.Framework.Utils.Ads.Data;
using I2.Loc;
using UnityEngine;

public partial class WaitingPopup : BaseUIView
{
    /// <summary>最大等待时间（秒），超过后自动关闭。0 或以下表示不限制。</summary>
    [SerializeField] private float maxWaitingSeconds = 10f;
    [SerializeField] private RotateFloatHandle rotateHandle;

    /// <summary>在关闭（包括按钮点击或超时调用 CloseStraightly）时触发的取消回调。</summary>
    private static Action? _onCancel;
    private float _startTime;
    private bool _started;

    private bool isAutoClose = true;

    void OnEnable()
    {
        rotateHandle.StartRotate();
    }

    private void Update()
    {
        if (!isAutoClose)
            return;

        if (!_started)
        {
            _startTime = Time.unscaledTime;
            _started = true;
        }

        if (maxWaitingSeconds > 0f &&
            Time.unscaledTime - _startTime >= maxWaitingSeconds)
        {
            SystemTips.Show(LocalizationManager.GetTranslation("U_HurryTips_GetAnswer_failed"));
            CloseStraightly();
        }
    }

    public static WaitingPopup Create(bool isAutoClose = true, float maxWaitingSeconds = 10)
    {
        var panel = UIManager.Instance.OpenUI<WaitingPopup>(nameof(WaitingPopup), UILayer.System);
        panel.isAutoClose = isAutoClose;
        panel.maxWaitingSeconds = maxWaitingSeconds;
        if (isAutoClose)
        {
            panel._started = false;
        }

        return panel;
    }

    /// <summary>
    /// 设置在本次 WaitingPopup 关闭时要触发的回调（可选）。
    /// 传入 null 表示清除回调。
    /// </summary>
    public static void SetOnCancel(Action? onCancel)
    {
        _onCancel = onCancel;
    }

    public static void CloseStraightly()
    {
        _onCancel?.Invoke();
        _onCancel = null;
        UIManager.Instance.CloseUI<WaitingPopup>();
    }

    /// <summary>
    /// WaitingPopup 上的取消按钮回调。
    /// 主动取消当前激励视频加载，并关闭等待弹窗。
    /// </summary>
    public void OnClickCancel()
    {
        RewardAdHelper.CancelCurrent();
        CloseStraightly();
    }
    public static async UniTask<AdCause> WaitingAdLoad(UniTaskCompletionSource<AdCause> completionSource)
    {
        WaitingPopup.Create();

        try
        {
            var cause = await completionSource.Task;

            if (cause == AdCause.Canceled)
            {
                // 用户在 loadingDialog 中主动取消，只关闭等待弹窗，不再提示失败
                Debug.Log($"[WaitingPopup] cause: {cause} (canceled by user)");
                return cause;
            }

            if (cause != AdCause.Success)
            {
                SystemTips.Show(LocalizationManager.GetTranslation("U_HurryTips_RewardVideo_failed"));
            }

            Debug.Log($"[WaitingPopup] cause: {cause}");

            return cause;
        }
        finally
        {
            WaitingPopup.CloseStraightly();
        }
    }
}

