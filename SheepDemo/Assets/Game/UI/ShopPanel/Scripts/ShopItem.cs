using System.Linq;
using Config;
using Game.Scripts.Common;
using Guru.SDK.Framework.Core.Financial.Product;
using I2.Loc;
using TMPro;

public partial class ShopItem : BaseAutoUIBind
{
    public TextMeshProUGUI Name => ItemNameTxt;
    public TextMeshProUGUI Content => ItemContentTxt;

    public ProductId Data { get; private set; }

    public Shop ShopData { get; private set; }

    public string ProductName { get; private set; }

    public CommonYellowBtn PurchaseBtn;

    public override void Init()
    {

    }

    /// <summary>
    /// 绑定参数
    /// </summary>
    /// <param name="data"></param>
    /// <param name="shopData"></param>
    /// <param name="productName"></param>
    public void SetData(ProductId data, Shop shopData, string productName)
    {
        Data = data;
        ShopData = shopData;
        ProductName = productName;

        Refresh();
    }

    /// <summary>
    /// 刷新文字
    /// </summary>
    public void Refresh()
    {
        int rewardValue = ShopData.Rewards.First().Value;
        string name = LocalizationManager.GetTranslation(ShopData.GoodsName);
        Name.text = name.IsValidStrFormat() ? string.Format(name, rewardValue) : name;

        string des = LocalizationManager.GetTranslation(ShopData.GoodsDes);
        Content.text = des.IsValidStrFormat() ? string.Format(des, rewardValue) : des;
    }
}
