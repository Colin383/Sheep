using Bear.EventSystem;
using Bear.Logger;
using Bear.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Events;
using Game.Play;
using UnityEngine;
using UnityEngine.UI;

public partial class GamePlayPanel : BaseUIView, IDebuger, IEventSender
{
    #region System
    private bool isPause = false;

    private EventSubscriber _subscriber;

    #endregion 

    #region Player ctrl

    private bool isRightDown = false;

    private bool isLeftDown = false;

    [SerializeField] private Image blackMask;
    [SerializeField] private GameObject clickBlock;

    [SerializeField] private CustomButton useBtn;

    // [SerializeField] private Animator anim;

    #endregion

    [Header("Show Panel Anim")]
    [SerializeField] private SequentialScaleAnim sequentialScaleAnim;

    public override void OnCreate()
    {
        base.OnCreate();

        InitBtns();

        sequentialScaleAnim.Completed += () =>
        {
            Debug.Log("All Complete");
            clickBlock.SetActive(false);
        };
    }

    public override void OnOpen()
    {
        base.OnOpen();
        isPause = false;
        isRightDown = false;
        isLeftDown = false;
        clickBlock.SetActive(false);

        AddListener();
    }

    private void AddListener()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<GameResetEvent>(OnGameReset);
        _subscriber.Subscribe<GameResumeEvent>(OnGamePanelResume);
        _subscriber.Subscribe<SwitchGameStateEvent>(OnGameStateChanged);
        _subscriber.Subscribe<GamePlayPanelFadeInEvent>(OnGamePlayFadeIn);
        _subscriber.Subscribe<GamePlayPanelSwitchBlockEvent>(OnSwichEventBlock);

        // _subscriber.Subscribe<GameShowBannerEvent>(OnSDKShowBannerEvent);
    }

    /*     private void OnSDKShowBannerEvent(GameShowBannerEvent @event)
        {
            // TODO: 完善 Banner 展示的时候 UI 相关的变化
            var root = transform.Find("Root");
            if (root != null && root is RectTransform rectTransform)
            {
                var offsetMin = rectTransform.offsetMin;
                offsetMin.y = @event.bannerHeight;
                rectTransform.offsetMin = offsetMin;
            }
        } */

    private void OnSwichEventBlock(GamePlayPanelSwitchBlockEvent @event)
    {
        clickBlock.SetActive(@event.IsShow);
    }

    #region Buttons
    private void InitBtns()
    {
        ResetBtn.OnClick += OnClickReset;
        PauseBtn.OnClick += OnClickSetting;
        TipsBtn.OnClick += OnClickTips;
        ShopBtn.OnClick += OnClickShop;

        JumpBtn.OnClickDown += OnClickJump;
        RightMoveBtn.OnClickEnter += OnClickDownRight;
        LeftMoveBtn.OnClickEnter += OnClickDownLeft;

        RightMoveBtn.OnClickUp += OnClickUpRight;
        LeftMoveBtn.OnClickUp += OnClickUpLeft;
        RightMoveBtn.OnClickExit += OnClickUpRight;
        LeftMoveBtn.OnClickExit += OnClickUpLeft;
    }


    private void OnClickDownRight(CustomButton btn)
    {
        this.Log("Right Down");
        isRightDown = true;
    }

    private void OnClickDownLeft(CustomButton btn)
    {
        this.Log("Left Down");
        isLeftDown = true;
    }

    private void OnClickUpRight(CustomButton btn)
    {
        this.Log("Right Up");
        isRightDown = false;
        this.DispatchEvent(Witness<PlayerMoveCancelEvent>._);
    }

    private void OnClickUpLeft(CustomButton btn)
    {
        this.Log("Left Up");
        isLeftDown = false;
        this.DispatchEvent(Witness<PlayerMoveCancelEvent>._);
    }

    private void OnClickJump(CustomButton btn)
    {
        if (isPause)
            return;

        this.Log("Jump");
        this.DispatchEvent(Witness<PlayerJumpEvent>._);
    }
    #endregion
    void Update()
    {
        if (isPause)
            return;

        if (isRightDown)
            this.DispatchEvent(Witness<PlayerRightMoveEvent>._);
        else if (isLeftDown)
            this.DispatchEvent(Witness<PlayerLeftMoveEvent>._);
    }

    private void OnClickReset(CustomButton btn)
    {
        this.Log("Play Game");
        isPause = true;
        this.DispatchEvent(Witness<GameResetEvent>._, GameResetType.Manually);
        // PlayResetAnim();
    }

    private void OnClickSetting(CustomButton btn)
    {
        this.Log("Pause Game");
        isPause = true;
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PAUSE);
        GameSettingPopup.Create(true);
    }

    private void OnClickShop(CustomButton btn)
    {
        isPause = true;
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PAUSE);
        ShopPanel.Create(true, GetType().Name);
    }


    private void OnClickTips(CustomButton btn)
    {
        this.Log("Tips Panel");
        isPause = true;
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PAUSE);
        this.DispatchEvent(Witness<GamePlayPanelSwitchTipsEvent>._, false);
        GameTipsPopup.Create();
    }

    private void OnGameReset(GameResetEvent evt)
    {
        PlayResetAnim();
    }

    private void PlayResetAnim()
    {
        blackMask.gameObject.SetActive(true);
        blackMask.color = new Color(0, 0, 0, 0);
        blackMask.DOFade(1f, 0.4f);
    }

    private void OnGamePlayFadeIn(GamePlayPanelFadeInEvent @event)
    {
        blackMask.gameObject.SetActive(true);
        blackMask.color = new Color(0, 0, 0, 1f);
        blackMask.DOFade(0f, .4f).SetUpdate(true).OnComplete(() =>
        {
            blackMask.gameObject.SetActive(false);
        });
    }

    private void OnGamePanelResume(GameResumeEvent evt)
    {
        isPause = false;
    }

    private void OnGameStateChanged(SwitchGameStateEvent evt)
    {
        isPause = !evt.NewState.Equals(GamePlayStateName.PLAYING);
    }

    public override void OnClose()
    {
        base.OnClose();
        EventsUtils.ResetEvents(ref _subscriber);
    }

    public void SetData(int level)
    {
        LevelTxt.text = level.ToString();
    }

    public void HideAllBtns()
    {
        var btns = GetComponentsInChildren<CustomButton>();
        for (int i = 0; i < btns.Length; i++)
        {
            btns[i].transform.localScale = Vector3.zero;
        }
    }

    public async UniTask PlayShowPanelAnim()
    {
        HideAllBtns();

        clickBlock.SetActive(true);
        await UniTask.WaitForSeconds(1f, true);

        if (sequentialScaleAnim == null)
            sequentialScaleAnim = GetComponent<SequentialScaleAnim>();

        if (sequentialScaleAnim != null)
        {
            var btns = GetComponentsInChildren<CustomButton>();
            var transforms = new Transform[btns.Length];
            for (int i = 0; i < btns.Length; i++)
            {
                transforms[i] = btns[i] != null ? btns[i].transform : null;

                /*         btns[i].transform.localScale = Vector3.zero;
                        btns[i].transform.DOScale(Vector3.one, 0.2f).SetDelay(0.06f * i).SetUpdate(UpdateType.Normal, true).SetEase(Ease.Linear); */
            }

            sequentialScaleAnim.SetTargets(transforms);
            sequentialScaleAnim.Play();
        }
    }

    public static GamePlayPanel Create(string panelName = "")
    {
        if (string.IsNullOrEmpty(panelName))
            panelName = $"{typeof(GamePlayPanel).Name}";

        var panel = UIManager.Instance.OpenUI<GamePlayPanel>(panelName, UILayer.Normal);
        return panel;
    }

    void OnDestroy()
    {
        if (blackMask != null)
            blackMask.DOKill();
    }
}
