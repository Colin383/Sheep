using System;
using Bear.EventSystem;
using Bear.Logger;
using Bear.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Events;
using UnityEngine;

public partial class StartPanel : BaseUIView, IDebuger, IEventSender
{
    [SerializeField] private UISpineCtrl spineCtrl;
    public override void OnOpen()
    {
        base.OnOpen();
        PlayBtn.transform.localScale = Vector3.zero;
        PlayBtn.transform.DOScale(Vector3.one, 0.4f).SetDelay(1f).SetEase(Ease.OutBack, 3f);
    }

    public override void OnCreate()
    {
        base.OnCreate();
#if DEBUG_MODE
        this.Log("------ gmbtn1");
        GMPanel.Create();        
#endif
        PlayBtn.OnClick += OnClickPlay;
        SettingBtn.OnClick += ShowSetting;
    }



    void OnDestroy()
    {
        PlayBtn.transform.DOKill();
    }

    /// <summary>
    /// 直接进入游戏
    /// </summary>
    /// <param name="btn"></param>
    private void OnClickPlay(CustomButton btn)
    {
        this.DispatchEvent(Witness<EnterLevelEvent>._, PlayCtrl.Instance.Level.CurrentLevelSort);

        WaitToClose().Forget();
    }

    private async UniTask WaitToClose()
    {
        await UniTask.WaitForSeconds(1f);

        UIManager.Instance.CloseUI(this);
    }

    private void OnShowChoiceLevel(CustomButton btn)
    {
        ClickTransformPanel.Create(() =>
        {
            ChoiceLevelPanel.Create();
        });
    }

    private void ShowSetting(CustomButton btn)
    {
        GameSettingPopup.Create();
    }


    private void TestTips(CustomButton btn)
    {
        SystemTips.Show(transform.parent, "test - luoweiming");
    }

    public static StartPanel Create()
    {
        var panel = UIManager.Instance.OpenUI<StartPanel>($"{typeof(StartPanel).Name}", UILayer.Normal);
        return panel;
    }
}
