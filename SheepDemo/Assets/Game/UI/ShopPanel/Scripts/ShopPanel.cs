
using System.Collections.Generic;
using System.Threading.Tasks;
using Bear.EventSystem;
using Bear.Logger;
using Bear.UI;
using Cysharp.Threading.Tasks;
using Game.Events;
using Game.Play;
using UnityEngine;
using UnityEngine.UI;
using static Products;

public partial class ShopPanel : BaseUIView, IDebuger, IEventSender
{
    [SerializeField] private List<ShopItem> items;

    [SerializeField] private List<GameObject> soldOut;

    private bool needResumeGame = false;
    private string lastUIName = "";

    private EventSubscriber _subscriber;


    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.OnClick += Close;

#if UNITY_IOS

        RestoreBtn.gameObject.SetActive(true);
        RestoreBtn.OnClick += TryToRestore;
#else

        RestoreBtn.gameObject.SetActive(false);
#endif

        InitItems();
    }

    public override void OnShow()
    {
        base.OnShow();
        // GameManager.Instance.Purchase.SyncRestore(false);
        RefreshItems();
    }

    private void TryToPurchase(CustomButton btn)
    {
        var shopItem = btn.GetComponentInParent<ShopItem>();
        // 商品页尝试购买点击 ==================================== 
        GameSDKService.Instance.IAP_CLK(shopItem.Data.Sku);
        GameManager.Instance.Purchase.Purchase(shopItem.ProductName, purchaseCallback: OnPurchaseCallback);
    }

    private void OnPurchaseCallback(string productName, bool isSuc)
    {
        if (isSuc)
        {
            GameManager.Instance.Purchase.OnPurchaseSuccess(productName);
            RefreshItems();
        }
        else
        {
            GameManager.Instance.Purchase.OnPurchaseFailed();
        }
    }

    private void TryToRestore(CustomButton btn)
    {
        IosRestoreAsync().Forget();
    }

    private async UniTask IosRestoreAsync()
    {
        WaitingPopup.Create(false);
        await GameManager.Instance.Purchase.Restore();
        WaitingPopup.CloseStraightly();
    }

    /// <summary>
    /// 获取具体商品名称
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private string GetProductNameByIndex(int index)
    {
        var name = index switch
        {
            0 => ProductName.NoAds,
            1 => ProductName.Hint1,
            2 => ProductName.Hint2,
            3 => ProductName.Hint99,
            _ => ""
        };

        return name;
    }

    private void InitItems()
    {
        Config.Shop shop;
        string productName = "";

        if (items.Count != soldOut.Count)
        {
            this.LogError($"ShopPanel InitItems list count not match. items:{items.Count}, soldOut:{soldOut.Count}");
        }

        for (int i = 0; i < items.Count; i++)
        {
            productName = GetProductNameByIndex(i);
            var productId = Products.GetProductId(productName);
            if (productId == null)
            {
                this.LogWarning($"ShopPanel InitItems productId is null, index:{i}, productName:{productName}");
                continue;
            }

            shop = GameManager.Instance.Purchase.GetRewardByProductName(productName);
            if (shop == null)
                continue;

            if (!IsAvailable(productName, shop) || IsNoNeedByHints(i))
            {
                items[i].gameObject.SetActive(false);
                soldOut[i].gameObject.SetActive(true);
                soldOut[i].GetComponent<ShopSoldOutAnim>().Play();
                continue;
            }

            items[i].GetComponent<ShopItemAnim>().Play();
            items[i].SetData(productId, shop, productName);
            items[i].PurchaseBtn.Btn.OnClick += TryToPurchase;
            items[i].PurchaseBtn.Content.text = GameManager.Instance.Purchase.GetPrice(productName);
        }

        RefreshItems();
    }

    private bool IsAvailable(string productName, Config.Shop shop)
    {
        DB.GameData.PurchaseCache.TryGetValue(productName, out int count);

        return shop.BuyTimes >= 0 && count < shop.BuyTimes || shop.BuyTimes < 0;
    }

    /// <summary>
    /// 特殊处理，商品 1，2 在 3 购买的情况下，不需要购买
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool IsNoNeedByHints(int index)
    {
        return (index == 1 || index == 2 || index == 3) && IsUnlimitHints();
    }

    private bool IsUnlimitHints()
    {
        DB.GameData.PurchaseCache.TryGetValue(ProductName.Hint99, out int count);
        return count > 0;
    }

    private void RefreshItems()
    {
        Config.Shop shop;
        string productName = "";

        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i].gameObject.activeSelf)
            {
                soldOut[i].GetComponent<ShopSoldOutAnim>().Play();
                continue;
            }

            productName = items[i].ProductName;
            shop = GameManager.Instance.Purchase.GetRewardByProductName(productName);
            if (shop == null)
                continue;
            if (!IsAvailable(productName, shop) || IsNoNeedByHints(i))
            {
                items[i].gameObject.SetActive(false);
                soldOut[i].gameObject.SetActive(true);

                soldOut[i].GetComponent<ShopSoldOutAnim>().Play();
                continue;
            }

            items[i].Refresh();
            items[i].GetComponent<ShopItemAnim>().Play();
            items[i].PurchaseBtn.Content.text = GameManager.Instance.Purchase.GetPrice(productName);
        }

        this.Log("Refresh items");
    }

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshItems();
        AddListener();
    }

    private void AddListener()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<OnRestoreSuccessEvent>(OnRestoreSuccess);
    }

    private void OnRestoreSuccess(OnRestoreSuccessEvent evt)
    {
        RefreshItems();
    }

    public override void OnClose()
    {
        base.OnClose();
        EventsUtils.ResetEvents(ref _subscriber);
    }

    private void Close(CustomButton btn)
    {
        // 不在主界面
        if (needResumeGame)
            this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PLAYING);

        UIManager.Instance.CloseUI(this);

        // 商品页关闭打点 ==================================== 
        GameSDKService.Instance.IAP_CLOSE(lastUIName);
    }

    public static ShopPanel Create(bool resumeGame = false, string lastUIName = "")
    {
        var panel = UIManager.Instance.OpenUI<ShopPanel>($"{typeof(ShopPanel).Name}", UILayer.Popup);
        panel.needResumeGame = resumeGame;
        panel.lastUIName = lastUIName;
        // 商品页展示打点 ==================================== 
        GameSDKService.Instance.IAP_IMP(lastUIName);

        return panel;
    }
}
