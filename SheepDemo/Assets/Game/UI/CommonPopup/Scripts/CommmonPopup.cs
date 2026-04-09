using System;
using Bear.UI;
using UnityEngine;

public partial class CommmonPopup : BaseUIView
{
    [SerializeField] private CustomButton YesBtn;

    [SerializeField] private CustomButton NoBtn;

    // 回调委托
    private Action _onYesCallback;
    private Action _onNoCallback;

    // 文本内容
    private string _titleText;
    private string _contentText;

    public override void OnOpen()
    {
        base.OnOpen();
    }

    void Start()
    {
        // 更新文本显示
        UpdateText();
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // 注册按钮事件
        BindButtons();
    }

    public override void OnClose()
    {
        base.OnClose();

        // 清理回调
        _onYesCallback = null;
        _onNoCallback = null;
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtons()
    {
        // 关闭按钮
        if (CloseBtn != null)
        {
            CloseBtn.OnClick += OnCloseBtnClick;
        }

        // Yes 按钮
        if (YesBtn != null)
        {
            YesBtn.OnClick += OnYesBtnClick;
        }

        // No 按钮
        if (NoBtn != null)
        {
            NoBtn.OnClick += OnNoBtnClick;
        }
    }

    /// <summary>
    /// 更新文本显示
    /// </summary>
    private void UpdateText()
    {
        // 更新标题
        if (UQuitTitle01Txt != null && !string.IsNullOrEmpty(_titleText))
        {
            UQuitTitle01Txt.text = _titleText;
        }

        // 更新内容
        if (UQuitDes01Txt != null && !string.IsNullOrEmpty(_contentText))
        {
            UQuitDes01Txt.text = _contentText;
        }
    }

    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseBtnClick(CustomButton btn)
    {
        // 关闭弹窗，不触发任何回调
        UIManager.Instance.DestroyUI(this);
    }

    /// <summary>
    /// Yes 按钮点击
    /// </summary>
    private void OnYesBtnClick(CustomButton btn)
    {
        _onYesCallback?.Invoke();
        UIManager.Instance.CloseUI(this);
    }

    /// <summary>
    /// No 按钮点击
    /// </summary>
    private void OnNoBtnClick(CustomButton btn)
    {
        _onNoCallback?.Invoke();
        UIManager.Instance.CloseUI(this);
    }

    /// <summary>
    /// 创建通用弹窗
    /// </summary>
    /// <param name="title">标题文本</param>
    /// <param name="content">内容文本</param>
    /// <param name="onYes">Yes 按钮回调</param>
    /// <param name="onNo">No 按钮回调</param>
    /// <returns>弹窗实例</returns>
    public static CommmonPopup Create(
        string title = null,
        string content = null,
        Action onYes = null,
        Action onNo = null)
    {
        var panel = UIManager.Instance.OpenUI<CommmonPopup>(
            $"{typeof(CommmonPopup).Name}",
            UILayer.Popup);

        // 设置文本和回调
        panel._titleText = title;
        panel._contentText = content;
        panel._onYesCallback = onYes;
        panel._onNoCallback = onNo;

        return panel;
    }

    /// <summary>
    /// 创建确认弹窗（只有确定按钮）
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="onConfirm">确定回调</param>
    /// <returns>弹窗实例</returns>
    public static CommmonPopup CreateConfirm(
        string title,
        string content,
        Action onConfirm)
    {
        var panel = Create(title, content, onConfirm, null);

        // 隐藏 No 按钮
        if (panel.NoBtn != null)
        {
            panel.NoBtn.gameObject.SetActive(false);
        }

        return panel;
    }

    /// <summary>
    /// 创建提示弹窗（没有按钮，只有关闭）
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <returns>弹窗实例</returns>
    public static CommmonPopup CreateTip(
        string title,
        string content)
    {
        var panel = Create(title, content, null, null);

        // 隐藏 Yes 和 No 按钮
        if (panel.YesBtn != null)
        {
            panel.YesBtn.gameObject.SetActive(false);
        }
        if (panel.NoBtn != null)
        {
            panel.NoBtn.gameObject.SetActive(false);
        }

        return panel;
    }
}
