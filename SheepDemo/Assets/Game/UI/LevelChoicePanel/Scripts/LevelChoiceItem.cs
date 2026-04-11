using Bear.EventSystem;
using Config;
using DG.Tweening;
using Game.Events;
using I2.Loc;
using UnityEngine;

/// <summary>
/// 关卡状态，未解锁关闭，广告解锁，已解锁，已通关
/// </summary>
public partial class LevelChoiceItem : BaseAutoUIBind, IEventSender
{
    private enum State
    {
        // 可以进入
        Normal,
        // 最大关卡等级
        Max,
        // 关卡等级不够
        LevelLock,
        // 广告锁定
        AdLock,
        // 已经通关
        Passed
    }

    public Color[] LevelStateColor;

    private State _state;
    private LevelSort _levelSort;
    private ChoiceLevelPanel _owner;

    public CanvasGroup canvasGroup;

    private Tween _canvasGroupAlphaTween;

    public void Init(ChoiceLevelPanel panel)
    {
        _owner = panel;
    }

    public void SetCanvasGroupAlpha(float value, float duration = 0f, float delay = 0f)
    {
        value = Mathf.Clamp01(value);
        delay = Mathf.Max(0f, delay);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            return;

        _canvasGroupAlphaTween?.Kill();
        _canvasGroupAlphaTween = null;

        if (duration <= 0f)
        {
            if (delay <= 0f)
            {
                canvasGroup.alpha = value;
                return;
            }

            _canvasGroupAlphaTween = DOVirtual.DelayedCall(delay, () =>
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = value;
            }).SetUpdate(true);
            return;
        }

        _canvasGroupAlphaTween = canvasGroup.DOFade(value, duration).SetDelay(delay).SetUpdate(true);
    }

    public void SetData(LevelSort levelSort)
    {
    }

    private void BtnClearEvents()
    {
        ClickAreaBtn.OnClick -= EnterLevel;
        ClickAreaBtn.OnClick -= ShowRewardAd;
    }

    private void RefreshState()
    {
       
    }

    private void ShowRewardAd(CustomButton btn)
    {
       
    }

    private void OnRewardAdCallback(string placement, bool isSuc)
    {
        if (isSuc)
        {
            PlayCtrl.Instance.Level.UnlockLevel(_levelSort.Id);
            _owner.EnterLevel(_levelSort);
        }
    }

    private void EnterLevel(CustomButton btn)
    {
        _owner.EnterLevel(_levelSort);
    }

    private void OnDestroy()
    {
        _canvasGroupAlphaTween?.Kill();
        _canvasGroupAlphaTween = null;

        BtnClearEvents();
    }

    public override void Init()
    {
        // throw new System.NotImplementedException();
    }
}
