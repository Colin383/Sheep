using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Guru.SDK.Framework.Core;
using Guru.SDK.Framework.Core.Spec;
using Guru.SDK.Framework.Core.Ads.Model;
using Guru.SDK.Framework.Core.Analytics.Model;
using Guru.SDK.Framework.Core.Financial.Manifest;
using Guru.SDK.Framework.Core.Financial.Product;
using Guru.SDK.Framework.Core.Firebase.RemoteConfig;
using Guru.SDK.Framework.Utils.Auth;
using Guru.SDK.Framework.Utils.Ads.Data;
using Guru.SDK.Framework.Utils.Product;


internal class BrainPunkAppSpec: AppSpec
{
    private static BrainPunkAppSpec _instance;
    public static BrainPunkAppSpec Instance
    {
        get
        {
            if (_instance == null) _instance = new BrainPunkAppSpec();
            return _instance;
        }
    }
    
    // 生成 [BrainPunkAppSpec] 配置定义，预填充所有配置属性值
    public BrainPunkAppSpec(): 
        base(
        appName: "BrainPunk",
        flavor: "main",
        appDetails: new AppDetails (
			saasAppId: "brainpunk",
			authority: "brain.gurugame.ai",
			storagePrefix: "https://firebasestorage.googleapis.com/v0/b/brainpunk-52a55.firebasestorage.app/o/",
			defaultCdnPrefix: "https://cdn3-brainpunk.fungame.cloud",
			androidGooglePlayUrl: "https://play.google.com/store/apps/details?id=com.brain.tricky.puzzle.games.test.quest",
			iosAppStoreUrl: "https://apps.apple.com/us/app/id6757749388",
			policyUrl: "https://brain.gurugame.ai/policy.html",
			termsUrl: "https://brain.gurugame.ai/termsofservice.html",
			emailUrl: "brain@gurugame.ai",
			packageName: "com.brain.tricky.puzzle.games.test.quest",
			bundleId: "com.brain.tricky.puzzle.games.test.quest",
			facebookAppId: "25486648387670947",
			gameServiceClientId: ""
        ), 
        deployment: new Deployment (
			propertyCacheSize: 256,
			logFileSizeLimit: 10485760,
			logFileCount: 7,
			persistentLogLevel: 1,
			conversionEvents: new HashSet<string> {
				"level_up",
				"level_up_1",
				"tch_ad_rev_roas_001"
			},
			apiConnectTimeout: 15000,
			apiReceiveTimeout: 15000,
			fullscreenAdsMinInterval: 60,
			purchaseEventTrigger: 1,
			bindGameCredentialStrategy: BindCredentialStrategy.IgnoreOnConflict,
			disableUnsoldManifestDeliverInventory: true,
			autoRequestNotificationPermission: true,
			shouldPreloadInterstitialByChecker: false,
			enabledCustomConsents: false,
			adjustEnableAdService: true
        ), 
        adsProfile: new AdsProfile (
			MaxSdkKey: new MaxSdkKey("V5I0i-vOTXkc_HEyqLg0lNm_ivlzp1wPVF3Vs7Jk4ix6WMAwEKGHMpugavZp_7xgss186Frvss23NGSWZXSago", "V5I0i-vOTXkc_HEyqLg0lNm_ivlzp1wPVF3Vs7Jk4ix6WMAwEKGHMpugavZp_7xgss186Frvss23NGSWZXSago"),
			BannerId: new AdUnitId("aab674f7fdecb215", "8c622fd013e9c761"),
			InterstitialId: new AdUnitId("e670774b655e9684", "030626c2321628f7"),
			RewardsId: new AdUnitId("85b76bf4b2bc5630", "bfb041251fb9203e"),
			MrecId: new AdUnitId("", ""),
			AmazonAppId: new AdAppId("fc8f78b2-ac6d-4d25-afd5-943f3f854d0d", "8aa939c5-662e-4a3f-aad7-13336b22ce28"),
			AmazonBannerSlotId: new AdSlotId("9e81ce69-d2ba-4ffe-b85a-e4eb6f84d4f6", "5b58f56f-cc6e-4e98-b10a-d5a9417ec360"),
			null,
			AmazonInterstitialSlotId: new AdSlotId("72f1809e-4f12-49d2-9ff5-c93eabce2835", "a1e0afe7-309b-427d-98cf-ce4f1663fb12"),
			AmazonRewardedSlotId: new AdSlotId("dd3ea773-0ac5-4bd2-9ce8-593a6e80504f", "1014df07-aec5-40e7-885d-86007762f41e"),
			IronsourceAppId: new AdAppId("", ""),
			null,
			null,
			null,
			null,
			TradPlusCreativeKey: new AdAppId("", "")          
        ), 
        productProfile: new ProductProfile (
			oneOffChargeIapIds: BrainPunkProducts.oneOffChargeIapIds,
			subscriptionsIapIds: BrainPunkProducts.subscriptionsIapIds,
			igcIds: BrainPunkProducts.igcIds,
			rewardIds: BrainPunkProducts.rewardIds,
			noAdsCapIds: BrainPunkProducts.noAdsCapIds,
			groupMap: BrainPunkProducts.groupMap,
			manifestBuilders: BrainPunkProducts.manifestBuilders
        ), 
        remoteConfigSpec: new RemoteConfigSpec (
			defaultValues: new Dictionary<string, object>() {
				["analytics_config"] = "{\"cap\":[\"firebase\",\"facebook\",\"guru\"], \"init_delay_s\": 10}",
				["app_rater"] = "{\"entry_enable\": false,\"gap_days\":0,\"last_pop_gap_days\":7,\"validation\":\"2\"}",
				["bads_config"] = "{\"free_s\":0,\"win_count\":1,\"amazon_enable\":false}",
				["iads_config"] = "{\"free_s\":0,\"validation\":\"3\",\"scene\":\"game_start|game_continue|p_continue|p_main|game_win|star_complete\",\"retry_min_s\":10,\"retry_max_s\":600,\"amazon_enable\":false,\"imp_gap_s\":10,\"sp_scene\":\"game_start:60\",\"ignore_scene_check\":true}",
				["rads_config"] = "{\"free_s\":0,\"validation\":\"1\",\"amazon_enable\":false}",
				["level_config"] = "{\"enabled\":false,\"ad_interstitialCD\":30,\"ad_rewardResetInterstitialCD\":60,\"ad_interstitialShowMinLevel\":3,\"ad_interstitialShowIntervalCount\":2,\"ad_interstitialShowFailedCount\":8,\"levelsort\":[10001,10002,10003,10004,10006,10007,10008,10010,10013,10019,10011,10012,10042,10014,10009,10036,10017,10021,10015,10018,10020,10016,10027,10022,10024,10023,10029,10026,10028,10025,10030,10031,10032,10033,10034,10035]}",
			},
			convertedKeys: new Dictionary<string, string>()         
	    ))
    {
        //TODO: Complete rest code
    }

}

/// <summary>
/// 自动生成的全局 BrainPunk Products 定义类，请勿修改
/// </summary>
internal static class BrainPunkProducts 
{
	internal static ProductId NoAds = new ProductId (
		android: "brpk.a.iap.noads1",
		ios: "brpk.i.iap.noads1",
		attr: TransactionAttribute.Asset,
		points: false,
		extras: new Dictionary<string, string>() {
			["ignore_sales"] = "true",
		}
);

	internal static ProductId Hint1 = new ProductId (
		android: "brpk.a.iap.hint1",
		ios: "brpk.i.iap.hint1",
		attr: TransactionAttribute.Consumable,
		points: false);

	internal static ProductId Hint2 = new ProductId (
		android: "brpk.a.iap.hint2",
		ios: "brpk.i.iap.hint2",
		attr: TransactionAttribute.Consumable,
		points: false);

	internal static ProductId Hint99 = new ProductId (
		android: "brpk.a.iap.hint99",
		ios: "brpk.i.iap.hint99",
		attr: TransactionAttribute.Asset,
		points: false);

	internal static Manifest buildNoAdsManifest(TransactionIntent intent)
	{
		if (intent.ProductId != NoAds)
			return Manifest.Empty;

		var extras = new Dictionary<string, object>() {
			{ ExtraReservedField.ContentId, intent.ProductId.Sku },
			{ ExtraReservedField.Scene, intent.Scene },
			{ ExtraReservedField.Rate, intent.Rate },
			{ ExtraReservedField.Sales, intent.Sales },
			{ "ignore_sales", "true" }
		};
		return new Manifest(category: "no_ads", extras: extras);
	}

	internal static Manifest buildHint1Manifest(TransactionIntent intent)
	{
		if (intent.ProductId != Hint1)
			return Manifest.Empty;

		var extras = new Dictionary<string, object>() {
			{ ExtraReservedField.ContentId, intent.ProductId.Sku },
			{ ExtraReservedField.Scene, intent.Scene },
			{ ExtraReservedField.Rate, intent.Rate },
			{ ExtraReservedField.Sales, intent.Sales }
		};
		return new Manifest(category: "hint", extras: extras);
	}

	internal static Manifest buildHint2Manifest(TransactionIntent intent)
	{
		if (intent.ProductId != Hint2)
			return Manifest.Empty;

		var extras = new Dictionary<string, object>() {
			{ ExtraReservedField.ContentId, intent.ProductId.Sku },
			{ ExtraReservedField.Scene, intent.Scene },
			{ ExtraReservedField.Rate, intent.Rate },
			{ ExtraReservedField.Sales, intent.Sales }
		};
		return new Manifest(category: "hint", extras: extras);
	}

	internal static Manifest buildHint99Manifest(TransactionIntent intent)
	{
		if (intent.ProductId != Hint99)
			return Manifest.Empty;

		var extras = new Dictionary<string, object>() {
			{ ExtraReservedField.ContentId, intent.ProductId.Sku },
			{ ExtraReservedField.Scene, intent.Scene },
			{ ExtraReservedField.Rate, intent.Rate },
			{ ExtraReservedField.Sales, intent.Sales }
		};
		return new Manifest(category: "hint", extras: extras);
	}

	internal static HashSet<ProductId> oneOffChargeIapIds = new HashSet<ProductId>() 
	{
		NoAds,
		Hint1,
		Hint2,
		Hint99,
	};

	internal static HashSet<ProductId> subscriptionsIapIds = new HashSet<ProductId>() 
	{
	};

	internal static HashSet<ProductId> igcIds = new HashSet<ProductId>() 
	{
	};

	internal static HashSet<ProductId> rewardIds = new HashSet<ProductId>() 
	{
	};

	internal static HashSet<ProductId> noAdsCapIds = new HashSet<ProductId>() 
	{
		NoAds,
	};

	internal static HashSet<ProductId> pointsIds = new HashSet<ProductId>() 
	{
	};

	internal static Dictionary<string, string> groupMap = new Dictionary<string, string>() 
	{
	};

	internal static List<ManifestBuilder> manifestBuilders = new List<ManifestBuilder>() 
	{
		buildNoAdsManifest,
		buildHint1Manifest,
		buildHint2Manifest,
		buildHint99Manifest,
	};
}


/// <summary>
/// 全局 GuruAppSepc 工厂类定义
/// </summary>
public class GuruSpecFactory
{
    public static AppSpec Create(string flavor)
    {
		if (flavor == "main") {
			return BrainPunkAppSpec.Instance;
		}

        Debug.LogError($"Try to create AppSpec but flavor \"{flavor}\" is invalid");
        return null;
    }
}


/// <summary>
/// 全局 Flavor 定义
/// </summary>
public class Flavors
{
	public const string main = "main";

}


/// <summary>
/// 全局 Products 定义
/// </summary>
public class Products
{
	public static ProductId NoAds {
		get => BrainPunkProducts.NoAds;
	}
	public static ProductId Hint1 {
		get => BrainPunkProducts.Hint1;
	}
	public static ProductId Hint2 {
		get => BrainPunkProducts.Hint2;
	}
	public static ProductId Hint99 {
		get => BrainPunkProducts.Hint99;
	}
	public static class ProductName {
		public const string NoAds = "no_ads";
		public const string Hint1 = "hint1";
		public const string Hint2 = "hint2";
		public const string Hint99 = "hint99";
	}

	private static Dictionary<string, ProductId> _productMap = new Dictionary<string, ProductId>() 
	{
		["no_ads"] = NoAds,
		["hint1"] = Hint1,
		["hint2"] = Hint2,
		["hint99"] = Hint99,
	};

	private static List<string> _allProductNameList = new List<string>() 
	{
		ProductName.NoAds,
		ProductName.Hint1,
		ProductName.Hint2,
		ProductName.Hint99,
	};

	/// <summary>
	/// 获取所有iap prudentName
	/// </summary>
	public static List<string> AllProductNameList => _allProductNameList;

	/// <summary>
	/// 获取 Product ID
	/// </summary>
	/// <param name="productName"></param>
	/// <returns></returns>
	public static ProductId GetProductId(string productName)
	{
		if (_productMap.TryGetValue(productName, out var productId))
		{
			return productId;
		}
		return null;
	}

	/// <summary>
	/// 获取 Product Name
	/// </summary>
	/// <param name="product"></param>
	/// <returns></returns>
	public static string GetProductName(ProductId product)
	{
		var findProduct = _productMap.FirstOrDefault(c => c.Value == product);
		return findProduct.Key;
	}


}
