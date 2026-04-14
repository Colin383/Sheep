using Bear.EventSystem;
using Bear.UI;
using Game.Events;
using Game.Play;
using UnityEngine;

public partial class RevivePopup : BaseUIView, IEventSender
{
    public override void OnOpen()
    {
        base.OnOpen();
    }

    public override void OnCreate()
    {
        base.OnCreate();
        BindButtons();
    }

    public override void OnClose()
    {
        base.OnClose();
    }

    private void BindButtons()
    {
        CloseBtn.OnClick += OnClickClose;
        ReviveBtn.OnClick += OnClickRevive;
    }

    private void OnClickClose(CustomButton btn)
    {
        UIManager.Instance.CloseUI(this);
        // 二级界面
        GameFailedPanel.Create();
    }

    private void OnClickRevive(CustomButton btn)
    {
        UIManager.Instance.CloseUI(this);
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PLAYING);
    }

    public static RevivePopup Create()
    {
        var panel = UIManager.Instance.OpenUI<RevivePopup>($"{typeof(RevivePopup).Name}", UILayer.Popup);
        return panel;
    }
}
