using System;
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

    [SerializeField] private Image blackMask;
    [SerializeField] private GameObject clickBlock;

    [SerializeField] private GameObject SkillBtns;
    [SerializeField] private GameObject SkillDesc;

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
        clickBlock.SetActive(false);

        AddListener();
        ResetSkillMode();
    }

    private void AddListener()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<GameResetEvent>(OnGameReset);
        _subscriber.Subscribe<GameResumeEvent>(OnGamePanelResume);
        _subscriber.Subscribe<SwitchGameStateEvent>(OnGameStateChanged);
        _subscriber.Subscribe<GamePlayPanelFadeInEvent>(OnGamePlayFadeIn);
        _subscriber.Subscribe<GamePlayPanelSwitchBlockEvent>(OnSwichEventBlock);
        _subscriber.Subscribe<ExitSkillEvent>(ExitSkillMode);
    }

    private void OnSwichEventBlock(GamePlayPanelSwitchBlockEvent @event)
    {
        clickBlock.SetActive(@event.IsShow);
    }

    #region Buttons
    private void InitBtns()
    {
        ResetBtn.OnClick += OnClickReset;
        PauseBtn.OnClick += OnClickSetting;
        ShopBtn.OnClick += OnClickShop;

        Skill1Btn.OnClick += ShowSkill1;
        Skill2Btn.OnClick += ShowSkill2;
        Skill3Btn.OnClick += ShowSkill3;
    }

    private void ShowSkill1(CustomButton btn)
    {
        EnterSkillMode(SkillType.Hint);
    }

    private void ShowSkill2(CustomButton btn)
    {
        EnterSkillMode(SkillType.RandomRotate5);
    }

    private void ShowSkill3(CustomButton btn)
    {
        EnterSkillMode(SkillType.Rotate);
    }

    private void EnterSkillMode(SkillType type)
    {
        this.DispatchEvent(Witness<EnterSkillEvent>._, type);

        if (SkillContentTxt != null)
        {
            SkillContentTxt.text = type switch
            {
                SkillType.Hint => "提示模式：高亮可移动的动物",
                SkillType.RandomRotate5 => "随机变换：随机变换5只动物方向",
                SkillType.Rotate => "变换模式：选择一只动物变换方向",
                _ => string.Empty
            };
        }

        if (type == SkillType.RandomRotate5)
            return;
            
        SkillDesc.SetActive(true);
        SkillBtns.SetActive(false);
    }

    private void ExitSkillMode(ExitSkillEvent evt)
    {
        ResetSkillMode();
    }

    private void ResetSkillMode()
    {
        SkillDesc.SetActive(false);
        SkillBtns.SetActive(true);
    }

    #endregion
    void Update()
    {
        if (isPause)
            return;

        /*         if (isRightDown)
                    this.DispatchEvent(Witness<PlayerRightMoveEvent>._);
                else if (isLeftDown)
                    this.DispatchEvent(Witness<PlayerLeftMoveEvent>._); */
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
        LevelTxt.text = string.Format("Level {0}", level.ToString());
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
