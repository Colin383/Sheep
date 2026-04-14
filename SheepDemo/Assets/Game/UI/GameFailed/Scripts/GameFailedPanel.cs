using Bear.EventSystem;
using Bear.UI;
using Game.Events;
using UnityEngine;

public partial class GameFailedPanel : BaseUIView, IEventSender
{
    public override void OnOpen()
    {
        base.OnOpen();
    }

    public override void OnCreate()
    {
        base.OnCreate();
        RestartBtn.OnClick += RestartGame;
    }

    public override void OnClose()
    {
        base.OnClose();
    }

    void RestartGame(CustomButton btn) 
    {
        UIManager.Instance.CloseUI(this);
        this.DispatchEvent(Witness<GameResetEvent>._, GameResetType.Manually);
    }

    public static GameFailedPanel Create()
    {
        var panel = UIManager.Instance.OpenUI<GameFailedPanel>($"{typeof(GameFailedPanel).Name}", UILayer.Popup);
        return panel;
    }
}
