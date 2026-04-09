using System;
using System.Collections.Generic;
using Bear.EventSystem;
using Bear.Logger;
using Config.Game;
using Game.Events;
using GameCommon;
using Cysharp.Threading.Tasks;
using Guru.SDK.Framework.Core.Financial.Iap;
using Guru.SDK.Framework.Core.Financial.Product;
using Guru.SDK.Framework.Utils.Financial.Iap;
using Guru.SDK.Framework.Core.Financial.Asset;
using R3;
using Game.ConfigModule;
using System.Linq;
using I2.Loc;
using Game.Scripts.Common;
using System.Diagnostics;

/// <summary>
/// 统一处理购买事件，通过 PurchaseEvent 发放奖励。
/// 由外部代码创建实例并保持生命周期。
/// 依托于 guru 生成代码 Products
/// </summary>
public class PurchaseManager : IEventSender, IDebuger
{
    private const string DefaultIntentScene = "legacy";

    private EventSubscriber _subscriber;
    private readonly List<IDisposable> _iapSubscriptions = new();
    private readonly List<string> _restoredProductNames = new();

    /// <summary>
    /// IAP 是否初始化成功。
    /// 用于外部快速判断购买/取价 API 是否可用。
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 已经尝试过初始化，不管成不成功
    /// </summary>
    public bool HasTryInit { get; private set; }

    private Dictionary<ProductId, ProductDetails>? _productDetails;
    private AssetStore<Asset>? _assetStore;

    /// <summary>
    /// 商品 Id 表，关联一下奖励表。主要用于 guru_spec 的数据填充用的，依托 guru 自动成成代码
    /// </summary>
    private List<Config.Products> ProducstIDs { get; set; }

    /// <summary>
    /// 商店奖励内容，早期表
    /// </summary>
    public List<Config.Shop> Shops { get; private set; }
    public IReadOnlyList<string> RestoredProductNames => _restoredProductNames;

    public PurchaseManager()
    {
        InitCallback();
    }

    /// <summary>
    /// 监听
    /// </summary>
    private void InitCallback()
    {
        TrySubscribeIap();
    }

    public void InitConfig()
    {
        ProducstIDs = ConfigManager.Instance.Tables.TbProducts.DataList.ToList();
        Shops = ConfigManager.Instance.Tables.TbShop.DataList.ToList();
    }

    private void TrySubscribeIap()
    {
        try
        {
            _iapSubscriptions.Add(IapManager.Instance.ObservableAvailable.Subscribe(OnIapInit));
            _iapSubscriptions.Add(IapManager.Instance.ObservableProductDetails.Subscribe(OnProductDetailsChanged));
            _iapSubscriptions.Add(IapManager.Instance.ObservableAssetStore.Subscribe(OnRestored));
        }
        catch (Exception e)
        {
            this.LogError($"Subscribe IAP callbacks failed: {e}");
        }
    }

    private void OnIapInit(bool success)
    {
        IsInitialized = success;
        HasTryInit = true;
        this.Log($"Iap init: {success}");
    }

    /// <summary>
    /// 发起“恢复购买”（通常仅 Android 需要显式调用）。
    /// </summary>
    /// <returns>是否发起/完成成功（由 SDK 返回）。</returns>
    public async UniTask<bool> Restore()
    {
        this.Log("Restore start.");

        bool result = await IapManager.Instance.RestorePurchases();
        this.Log($"Restore sdk return. result:{result}");
        if (!result) this.LogWarning("Restore sdk returned false.");

        return result;
    }

    private void OnProductDetailsChanged(Dictionary<ProductId, ProductDetails>? data)
    {
        if (data == null || data.Count == 0)
            return;

        _productDetails = data;
        // TODO: 有需要的话可在这里抛事件通知 UI 刷新价格信息
    }

    private void OnRestored(AssetStore<Asset>? assetStore)
    {
        if (assetStore == null)
        {
            this.LogWarning("OnRestored assetStore is null.");
            return;
        }

        _assetStore = assetStore;
        var dataCount = assetStore.Data?.Count ?? 0;
        this.Log($"OnRestored enter. dataCount:{dataCount}");

        var productNames = new List<string>();
        var rawSkus = new List<string>();
        var unresolvedSkus = new List<string>();
        if (dataCount > 0)
        {
            foreach (var item in assetStore.Data)
            {
                var productId = item.Key;
                var sku = productId?.Sku ?? "null";
                rawSkus.Add(sku);
                var productName = Products.GetProductName(productId);
                if (!string.IsNullOrEmpty(productName))
                {
                    productNames.Add(productName);
                    this.Log($"OnRestored map success. sku:{sku}, productName:{productName}");
                }
                else
                {
                    unresolvedSkus.Add(sku);
                    this.LogWarning($"OnRestored map failed. sku:{sku}");
                }
            }
        }
        else
        {
            return;
        }

        this.Log(
            $"OnRestored parse done. rawCount:{rawSkus.Count}, resolvedCount:{productNames.Count}, unresolvedCount:{unresolvedSkus.Count}");

        _restoredProductNames.Clear();
        _restoredProductNames.AddRange(productNames.Distinct());

        // 只记录恢复结果，手动调用 SyncRestore 时再执行补发。
        this.Log($"OnRestored saved productNames: [{string.Join(", ", _restoredProductNames)}]");
        
        SyncRestore();
      /*   SystemTips.Show(LocalizationManager.GetTranslation("U_Shop_Restore_Tips_01"));
        AudioManager.PlaySound("shopGetReward");
        this.Log($"OnRestored finish. restoredCount:{productNames.Count}"); */
    }

    /// <summary>
    /// 手动执行恢复补发。基于最近一次 OnRestored 解析并保存的 productNames。
    /// </summary>
    /// <param name="showTips">是否展示成功提示。</param>
    /// <returns>是否存在可执行的恢复商品。</returns>
    public bool SyncRestore(bool showTips = true)
    {
        if (_restoredProductNames.Count == 0)
        {
            this.LogWarning("SyncRestore skipped. no saved restored product names.");
            return false;
        }

        var successCount = 0;
        foreach (var productName in _restoredProductNames)
        {
            try
            {
                if (DB.GameData.PurchaseCache != null &&
                    DB.GameData.PurchaseCache.TryGetValue(productName, out var purchaseCount) &&
                    purchaseCount > 0)
                {
                    this.Log($"SyncRestore skip. product already granted. productName:{productName}, purchaseCount:{purchaseCount}");
                    continue;
                }

                this.Log($"SyncRestore apply reward. productName:{productName}");
                OnPurchaseSuccess(productName, false);
                successCount++;
            }
            catch (Exception e)
            {
                this.LogError($"SyncRestore apply reward failed. productName:{productName}, error:{e}");
            }
        }

        if (showTips && successCount > 0)
        {
            SystemTips.Show(LocalizationManager.GetTranslation("U_Shop_Restore_Tips_01"));
            AudioManager.PlaySound("shopGetReward");
        }

        // 内购恢复刷新
        if(successCount > 0)
            this.DispatchEvent(Witness<OnRestoreSuccessEvent>._);

        this.Log($"SyncRestore finish. successCount:{successCount}, savedCount:{_restoredProductNames.Count}");
        return successCount > 0;
    }

    /// <summary>
    /// 是否已购买（或已拥有）指定商品。
    /// </summary>
    /// <param name="productName">项目内部商品名（可通过配置/约定映射到 ProductId）。</param>
    public bool IsPurchased(string productName)
    {
        if (!TryGetProductId(productName, out var productId))
            return false;

        return IapManager.Instance.AssetStore.IsOwned(productId);
    }

    /// <summary>
    /// 获取商品信息（价格、币种等）。
    /// </summary>
    /// <param name="productName">项目内部商品名。</param>
    /// <param name="intentScene">购买意图场景（用于埋点/归因），默认 "legacy"。</param>
    /// <returns>商品信息；若未找到映射或未拉到商品信息则返回 null。</returns>
    public IapProduct? GetProductInfo(string productName, string intentScene = DefaultIntentScene)
    {
        if (!TryGetProductId(productName, out var productId))
            return null;

        var product = GetProductByIntent(productId, intentScene);
        this.Log(
            $"GetProductInfo productName:{productName}, Price:{product?.Details.Price}, CurrencyCode:{product?.Details.CurrencyCode} sku:{product?.ProductId.Sku}");
        return product;
    }

    /// <summary>
    /// 获取商品价格展示字符串（例如 "$0.99"）。
    /// 优先使用已缓存的商品详情；若未缓存则尝试即时构建查询。
    /// </summary>
    /// <param name="productName">项目内部商品名。</param>
    /// <param name="intentScene">购买意图场景（用于埋点/归因），默认 "legacy"。</param>
    public string GetPrice(string productName, string intentScene = DefaultIntentScene)
    {
        if (!TryGetProductId(productName, out var productId))
            return LocalizationManager.GetTranslation("U_Shop_Unavailable");

        if (_productDetails != null && _productDetails.TryGetValue(productId, out var details) && !string.IsNullOrEmpty(details.Price))
            return details.Price;

        var priceString = GetProductInfo(productName, intentScene)?.Details.Price;
        if (string.IsNullOrEmpty(priceString))
        {
            this.Log($"GetPrice item:{productName}, price is empty.");
            return LocalizationManager.GetTranslation("U_Shop_Unavailable");
        }

        return priceString;
    }

    /// <summary>
    /// 发起购买请求。
    /// </summary>
    /// <param name="productName">项目内部商品名。</param>
    /// <param name="category">购买场景/来源（用于埋点/归因）。</param>
    /// <param name="purchaseCallback">
    /// 购买结果回调。回调会通过 <see cref="RunOnMainThread{T1,T2}"/> 投递到主线程执行。
    /// </param>
    public void Purchase(string productName, string category = "store", Action<string, bool>? purchaseCallback = null)
    {
        if (!TryGetProductId(productName, out var productId))
        {
            RunOnMainThread(purchaseCallback, productName, false);
            this.LogWarning($"Purchase productName:{productName} productId is null");
            return;
        }

        var product = GetProductByIntent(productId, category);
        if (product == null)
        {
            RunOnMainThread(purchaseCallback, productName, false);
            this.LogWarning($"Purchase productName:{productName} product is null");
            return;
        }

        this.Log($"Purchase productName:{productName},Price:{product.Details.Price} request purchase.");

        /// 编辑器模式：弹窗 + 1s 模拟等待，便于联调 UI 流程
#if UNITY_EDITOR
        SimulateEditorPurchase(productName, purchaseCallback).Forget();
        return;
#endif

        WaitingPopup.Create(false);

        IapManager.Instance.Request(product).ContinueWith(result =>
        {
            UniTask.Post(() =>
            {
                WaitingPopup.CloseStraightly();
                purchaseCallback?.Invoke(productName, result);
                this.Log("Purchase result: " + result);
            });
        }).Forget();
    }

#if UNITY_EDITOR
    private async UniTaskVoid SimulateEditorPurchase(string productName, Action<string, bool>? purchaseCallback)
    {
        WaitingPopup.Create(false);
        try
        {
            await UniTask.Delay(1000, DelayType.UnscaledDeltaTime);
        }
        finally
        {
            WaitingPopup.CloseStraightly();
        }

        RunOnMainThread(purchaseCallback, productName, true);
        this.Log("Purchase result (editor simulate): True");
    }
#endif

    private static bool TryGetProductId(string productName, out ProductId productId)
    {
        productId = Products.GetProductId(productName);
        return productId != null;
    }

    private static IapProduct? GetProductByIntent(ProductId productId, string intentScene)
    {
        var productStore = IapManager.Instance.BuildProducts(new HashSet<TransactionIntent>
        {
            productId.CreateIntent(intentScene)
        });

        return productStore.GetProduct(productId);
    }

    /// <summary>
    /// 将回调投递到 Unity 主线程执行，避免在后台线程触发 Unity API 调用。
    /// </summary>
    public static void RunOnMainThread<T1, T2>(Action<T1, T2>? callback, T1 param1, T2 param2)
    {
        UniTask.Post(() =>
        {
            callback?.Invoke(param1, param2);
        });
    }

    /// <summary>
    /// 释放事件订阅与 IAP 订阅，避免重复回调与内存泄漏。
    /// </summary>
    public void Dispose()
    {
        EventsUtils.ResetEvents(ref _subscriber);

        foreach (var d in _iapSubscriptions)
        {
            try
            {
                d.Dispose();
            }
            catch (Exception e)
            {
                this.LogError($"Dispose iap subscription failed: {e}");
            }
        }

        _iapSubscriptions.Clear();
    }

    /// <summary>
    /// 获取真实奖励对象通过 productName
    /// </summary>
    /// <param name="productName"></param>
    /// <returns></returns>
    public Config.Shop GetRewardByProductName(string productName)
    {
        var data = ProducstIDs.Find(p => p.Id.Equals(productName));
        if (data == null)
            this.LogError($"Product lost: {productName}");

        var shop = Shops.Find(shop => shop.Id.Equals(data.RewardId));
        return shop;
    }

    // 内购成功，通用方法
    public void OnPurchaseSuccess(string productName, bool showTips = true)
    {
        // 通过事件交给 PurchaseManager 统一处理奖励发放
        var shopData = GetRewardByProductName(productName);
        var rewardsCopy = new Dictionary<GameProps, int>(shopData.Rewards);

        OnPurchase(productName, rewardsCopy);
        if (showTips)
        {
            SystemTips.Show(LocalizationManager.GetTranslation("U_HurryTips_Shop_succeed"));
            AudioManager.PlaySound("shopGetReward");
        }

    }

    // 购买失败
    public void OnPurchaseFailed()
    {
        SystemTips.Show(LocalizationManager.GetTranslation("U_HurryTips_Shop_failed"));
    }

    private void OnPurchase(string productName, Dictionary<GameProps, int> rewards)
    {
        if (rewards == null || rewards.Count == 0)
            return;

        this.Log("内购成功");

        // 用于记录购买次数，内购回调之前统一验证
        if (!DB.GameData.PurchaseCache.TryAdd(productName, 1))
            DB.GameData.PurchaseCache[productName]++;

        foreach (KeyValuePair<GameProps, int> reward in rewards)
        {
            PlayCtrl.Instance.Bag.AddTool(reward.Key, reward.Value, RewardType.Purchase);
        }
    }

}

