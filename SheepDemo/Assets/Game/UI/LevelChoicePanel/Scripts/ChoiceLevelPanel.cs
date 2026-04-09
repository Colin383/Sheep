using System;
using System.Collections.Generic;
using System.Linq;
using Bear.EventSystem;
using Bear.UI;
using Config;
using Cysharp.Threading.Tasks;
using Game.Scripts.Common;
using Game.Events;
using Config.Game;
using UnityEngine;
using Bear.Logger;
using System.Threading.Tasks;
using DG.Tweening;

public partial class ChoiceLevelPanel : BaseUIView, IEventSender, IDebuger
{
    [SerializeField] private int MaxCount;
    [SerializeField] private RectTransform Content;
    [SerializeField] private LevelChoiceItem itemPrefab;
    [SerializeField] private RowFirstLayoutGroup gridLayout;

    [SerializeField] private UISpineCtrl bgCtrl;
    [SerializeField] private UISpineCtrl boxCtrl;

    private List<LevelSort> sortDatas;
    private EventSubscriber _subscriber;
    private int pageIndex = 0;
    private List<LevelChoiceItem> _itemCache;
    private bool _isPageAnimating;

    public override void OnCreate()
    {
        base.OnCreate();

        sortDatas = PlayCtrl.Instance.Level.LevelSorts;

        InitItems();
        InitButtons();
    }

    private async Task PlayOpenAnim()
    {
        bgCtrl.gameObject.SetActive(false);
        boxCtrl.gameObject.SetActive(false);

        for (int i = 0; i < _itemCache.Count; i++)
        {
            _itemCache[i].SetCanvasGroupAlpha(0);
        }

        await UniTask.WaitForSeconds(.5f);

        bgCtrl.gameObject.SetActive(true);
        boxCtrl.gameObject.SetActive(true);

        for (int i = 0; i < _itemCache.Count; i++)
        {
            _itemCache[i].transform.DOPunchScale(Vector3.up * 0.3f, 0.3f, 1, 1).SetDelay(i * 0.05f);
            _itemCache[i].SetCanvasGroupAlpha(1, 0.2f, delay: i * 0.05f);
        }

        bgCtrl.PlayAnimation("book_in", false).Complete += (entry) =>
        {
            bgCtrl.PlayAnimation("book_idle", true);
        };

        boxCtrl.PlayAnimation("box_in", false).Complete += (entry) =>
        {
            boxCtrl.PlayAnimation("box_idle", true);
        };
    }

    void Start()
    {
        gridLayout.RebuildLayout();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshBtn();
        RefreshItems();

        AddListener();

        if (!AudioManager.IsCurrentMusicTag("musicOutGame"))
        {
            AudioManager.PlayMusic("musicOutGame", fadeInSeconds: 8f);
        }
    }

    private void AddListener()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<UpdatePropEvent>(OnPropUpdate);
        _subscriber.Subscribe<OnRestoreSuccessEvent>(OnRestoreSuccess);
    }

    private void OnPropUpdate(UpdatePropEvent evt)
    {
        if (evt.Prop != GameProps.NoAds)
            return;

        RefreshBtn();
    }

    private void OnRestoreSuccess(OnRestoreSuccessEvent evt)
    {
        RefreshBtn();
        RefreshItems();
        gridLayout.RebuildLayout();
    }

    private void InitItems()
    {
        if (_itemCache == null)
            _itemCache = new List<LevelChoiceItem>();

        if (_itemCache.Count <= MaxCount)
        {
            for (int i = _itemCache.Count; i < MaxCount; i++)
            {
                var item = Instantiate(itemPrefab, Content);
                item.Init(this);
                _itemCache.Add(item);
            }
        }

        int maxIndex = sortDatas.Count / MaxCount;
        if (sortDatas.Count % MaxCount == 0)
            maxIndex -= 1;

        pageIndex = Math.Clamp(DB.GameData.ChioceLevelPanelPageIndex, 0, maxIndex);
        RefreshItems();
    }

    private void InitButtons()
    {
        LastBtn.OnClick += LastPage;
        NextBtn.OnClick += NextPage;

        SettingBtn.OnClick += ShowSetting;
        ShopBtn.OnClick += ShowShop;
        NoAdBtn.OnClick += ShowNoAd;

        RefreshBtn();
    }

    private void ShowNoAd(CustomButton btn)
    {
        NoAdsPopup.Create();
    }

    private void ShowShop(CustomButton btn)
    {
        ShopPanel.Create(false, GetType().Name);
    }

    private void ShowSetting(CustomButton btn)
    {
        GameSettingPopup.Create();
    }

    private void LastPage(CustomButton btn)
    {
        if (_isPageAnimating)
            return;

        var newPageIndex = Math.Max(pageIndex - 1, 0);
        if (newPageIndex == pageIndex)
            return;

        _isPageAnimating = true;
        pageIndex = newPageIndex;

        for (int i = 0; i < _itemCache.Count; i++)
        {
            _itemCache[i].SetCanvasGroupAlpha(0, 0.2f);
        }

        AudioManager.PlaySound("FlipPage");

        bgCtrl.PlayAnimation("book_turn-the-page_reverse", false).Complete += (entry) =>
        {
            bgCtrl.PlayAnimation("book_idle", true);

            RefreshBtn();
            RefreshItems();
            for (int i = 0; i < _itemCache.Count; i++)
            {
                _itemCache[i].SetCanvasGroupAlpha(1, 0.2f, i * 0.05f);
            }

            _isPageAnimating = false;
        };
    }
    private void NextPage(CustomButton btn)
    {
        if (_isPageAnimating)
            return;

        int dt = sortDatas.Count % MaxCount > 0 ? 1 : 0;
        var newPageIndex = Math.Min(pageIndex + 1, sortDatas.Count / MaxCount + dt);
        if (newPageIndex == pageIndex)
            return;

        _isPageAnimating = true;
        pageIndex = newPageIndex;

        for (int i = 0; i < _itemCache.Count; i++)
        {
            _itemCache[i].SetCanvasGroupAlpha(0, 0.2f);
        }

        AudioManager.PlaySound("FlipPage");

        bgCtrl.PlayAnimation("book_turn-the-page", false).Complete += (entry) =>
        {
            RefreshBtn();
            RefreshItems();
            bgCtrl.PlayAnimation("book_idle", true);

            for (int i = 0; i < _itemCache.Count; i++)
            {
                _itemCache[i].SetCanvasGroupAlpha(1, 0.2f, i * 0.05f);
            }

            _isPageAnimating = false;
        };
    }

    private void RefreshBtn()
    {
        LastBtn.gameObject.SetActive(pageIndex > 0);
        NextBtn.gameObject.SetActive((pageIndex + 1) * MaxCount < sortDatas.Count);

        // noAd 按钮隐藏
        if (PlayCtrl.Instance.Bag.GetToolCount(Config.Game.GameProps.NoAds) > 0)
        {
            NoAdBtn.gameObject.SetActive(false);
        }
        else
        {
            NoAdBtn.gameObject.SetActive(true);
        }
    }

    private void RefreshItems()
    {
        if (_itemCache.Count <= 0)
            return;

        int index = -1;
        for (int i = 0; i < _itemCache.Count; i++)
        {
            index = pageIndex * MaxCount + i;

            var sortData = index >= sortDatas.Count ? null : sortDatas[index];
            _itemCache[i].SetData(sortData);
        }
    }

    public void EnterLevel(LevelSort sort)
    {
        this.Log("--------------------- enter level");

        this.DispatchEvent(Witness<Game.Events.EnterLevelEvent>._, sort);
        WaitToClose().Forget();

        DB.GameData.ChioceLevelPanelPageIndex = pageIndex;
        DB.GameData.Save();
    }

    private async UniTask WaitToClose()
    {
        await UniTask.WaitForSeconds(1f);

        UIManager.Instance.CloseUI(this);
    }

    public static ChoiceLevelPanel Create()
    {
        var panel = UIManager.Instance.OpenUI<ChoiceLevelPanel>($"{typeof(ChoiceLevelPanel).Name}", UILayer.Normal);
        _ = panel.PlayOpenAnim();
        return panel;
    }

    public override void OnClose()
    {
        base.OnClose();
        EventsUtils.ResetEvents(ref _subscriber);
    }
}
