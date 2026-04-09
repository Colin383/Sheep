using System;
using Bear.EventSystem;
using Bear.Logger;
using Config.Game;
using DG.Tweening;
using Game.Events;
using UnityEngine;


public partial class PropBlock : BaseAutoUIBind, IDebuger
{
    [SerializeField] private GameProps Prop;
    [SerializeField] private bool IsAutoUpdate = true;

    private EventSubscriber _subscriber;

    void Awake()
    {
        if (IsAutoUpdate)
        {
            EventsUtils.ResetEvents(ref _subscriber);
            _subscriber.Subscribe<UpdatePropEvent>(OnPropUpdate);
        }

        Init();
    }

    void OnEnable()
    {
        BindData(PlayCtrl.Instance.Bag.GetToolCount(Prop));
    }

    private void OnPropUpdate(UpdatePropEvent evt)
    {
        if (Prop == GameProps.Tips)
        {
            if (PlayCtrl.Instance.Bag.HasTool(GameProps.UnlimitTips))
            {
                CountTxt.text = "∞";
                return;
            }
        }

        if (Prop != evt.Prop)
            return;

        CountTxt.DOCounter(evt.OldCount, evt.NewCount, 0.4f).SetUpdate(true);
    }

    public override void Init()
    {
        ShopBtn.OnClick += ShowShop;
    }

    public void BindData(int oldCount)
    {
        if (Prop == GameProps.Tips)
        {
            if (PlayCtrl.Instance.Bag.HasTool(GameProps.UnlimitTips))
            {
                CountTxt.text = "∞";
                return;
            }
        }

        CountTxt.text = oldCount.ToString();
    }

    private void ShowShop(CustomButton btn)
    {
        this.Log("Show Shop");
        ShopPanel.Create(false, "GameTipsPopup");
    }

    void OnDestroy()
    {
        CountTxt.DOKill();
    }
}
