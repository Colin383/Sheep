using System;
using Bear.EventSystem;
using Bear.Logger;
using Bear.UI;
using GameCommon;
using UnityEngine;
using static Products;

public partial class NoAdsPopup : BaseUIView, IDebuger, IEventSender
{
    [SerializeField] private UISpineCtrl spineCtrl;
    [SerializeField] private CommonYellowBtn buyBtn;

    private string lastUIName = "";
    private const string NoAdProductName = ProductName.NoAds;

    public override void OnCreate()
    {
        base.OnCreate();

        buyBtn.Content.text = GameManager.Instance.Purchase.GetPrice(NoAdProductName);
        buyBtn.Btn.OnClick += BuyNoAd;
        CloseBtn.OnClick += ClosePanel;
    }

    public override void OnOpen()
    {
        base.OnOpen();

        if (spineCtrl)
        {
            spineCtrl.PlayAnimation("in", false).Complete += (track) =>
            {
                spineCtrl.PlayAnimation("idle", true);
            };
        }
    }

    public void Show()
    {
        GameSDKService.Instance.IAP_IMP(lastUIName);
    }

    private void BuyNoAd(CustomButton btn)
    {
        var data = Products.GetProductId(NoAdProductName);
        GameSDKService.Instance.IAP_CLK(data.Sku);
        GameManager.Instance.Purchase.Purchase(NoAdProductName, purchaseCallback: OnPurchaseCallback);
    }

    private void OnPurchaseCallback(string productName, bool isSuc)
    {
        if (isSuc)
        {
            GameManager.Instance.Purchase.OnPurchaseSuccess(productName);
            UIManager.Instance.CloseUI(this);
            GameSDKService.Instance.IAP_CLOSE(lastUIName);
        }
        else
        {
            GameManager.Instance.Purchase.OnPurchaseFailed();
        }
    }

    private void ClosePanel(CustomButton btn)
    {
        UIManager.Instance.CloseUI(this);
        GameSDKService.Instance.IAP_CLOSE(lastUIName);
    }

    public static NoAdsPopup Create(string lastUIName = "ChoiceLevelPanel")
    {
        var panel = UIManager.Instance.OpenUI<NoAdsPopup>($"{typeof(NoAdsPopup).Name}", UILayer.Popup);
        panel.lastUIName = lastUIName;
        panel.Show();
        return panel;
    }
}
