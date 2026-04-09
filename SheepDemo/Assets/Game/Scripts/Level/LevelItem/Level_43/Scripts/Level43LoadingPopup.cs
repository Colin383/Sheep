using System;
using Bear.UI;
using TMPro;
using UnityEngine;

/// <summary>
/// 提供一个定时器，默认时间为 72 小时, 定时器在 Update 中逐渐减少时间。格式为 00:00:00
/// SpeedUP 点击一次可以减少 1s.
/// 检测 customSlider 角度，超过指定角度， cost speed * Multiple 倍
/// </summary>
public partial class Level43LoadingPopup : BaseUIView
{
    [SerializeField] private CustomButton SpeedUpBtn;
    [SerializeField] private UICustomSlider customSlider;

    [Header("加速配置")]
    [Tooltip("点击一次减少的时间（秒）")]
    [SerializeField] private float clickCostTime = 1f;

    [Header("角度检测配置")]
    [Tooltip("用于角度检测的滑块（带旋转的）")]
    [SerializeField] private UIDragRotate angleSlider;

    [Tooltip("角度阈值（超过此角度触发倍率）")]
    [SerializeField] private float angleThreshold = 45f;

    [Tooltip("超过阈值时的倍率")]
    [SerializeField] private float speedUpMultiple = 2f;

    [Tooltip("是否使用绝对角度")]
    [SerializeField] private bool useAbsoluteAngle = true;

    // 默认 72 小时 = 72 * 3600 秒
    private const float DEFAULT_TOTAL_TIME = 72f * 3600f;
    private float currentTime;
    private float totalTime;

    private bool isStart = false;
    private bool isFinished = false;

    private Action OnComplete;

    public override void OnOpen()
    {
        base.OnOpen();
        ResetTimer();
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // 注册加速按钮点击事件
        if (SpeedUpBtn != null)
        {
            SpeedUpBtn.OnClick += OnSpeedUpClick;
        }
    }

    public override void OnClose()
    {
        base.OnClose();

        // 移除按钮监听
        if (SpeedUpBtn != null)
        {
            SpeedUpBtn.OnClick -= OnSpeedUpClick;
        }
    }

    void Update()
    {
        if (isFinished || !isStart)
            return;

        // 倒计时逻辑
        if (currentTime > 0)
        {
            // 检测角度，如果超过阈值，自然消耗也应用倍率
            float deltaTime = Time.deltaTime;
            if (angleSlider != null && IsAngleExceeded())
            {
                deltaTime *= speedUpMultiple;
            }

            currentTime -= deltaTime;
            if (currentTime < 0)
            {
                currentTime = 0;
            }
            UpdateUI();
        }
        else
        {
            isFinished = true;
            OnComplete?.Invoke();
            OnComplete = null;
        }
    }

    /// <summary>
    /// 重置计时器为默认值
    /// </summary>
    private void ResetTimer()
    {
        totalTime = DEFAULT_TOTAL_TIME;
        currentTime = totalTime;
        isStart = true;
        UpdateUI();
    }

    /// <summary>
    /// 更新 UI 显示
    /// </summary>
    private void UpdateUI()
    {
        // 更新时间文本
        if (UiJiazai4Txt != null)
        {
            UiJiazai4Txt.text = FormatTime(currentTime);
        }

        // 更新进度条
        if (customSlider != null)
        {
            float progress = 1f - (currentTime / totalTime);
            customSlider.SetProcess(progress);
        }
    }

    /// <summary>
    /// 将秒数格式化为 00:00:00
    /// </summary>
    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;
        return $"{hours:D2}:{minutes:D2}:{secs:D2}";
    }

    /// <summary>
    /// 加速按钮点击回调
    /// </summary>
    private void OnSpeedUpClick(CustomButton btn)
    {
        // 减少点击配置的固定时间
        currentTime -= clickCostTime;
        if (currentTime < 0)
        {
            currentTime = 0;
        }
        UpdateUI();
    }

    /// <summary>
    /// 检测角度是否超过阈值
    /// </summary>
    private bool IsAngleExceeded()
    {
        if (angleSlider == null) return false;

        float currentAngle = angleSlider.GetCurrentAngle();

        // 根据是否使用绝对角度来判断
        if (useAbsoluteAngle)
        {
            return Mathf.Abs(currentAngle) > angleThreshold;
        }
        else
        {
            return currentAngle > angleThreshold;
        }
    }

    /// <summary>
    /// 设置剩余时间（用于保存/恢复进度）
    /// </summary>
    public void SetRemainTime(float remainSeconds)
    {
        currentTime = Mathf.Max(0, remainSeconds);
        UpdateUI();
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public float GetRemainTime()
    {
        return currentTime;
    }

    /// <summary>
    /// 设置完成回调
    /// </summary>
    public void SetCompleteCallback(Action complete)
    {
        OnComplete = complete;
    }

    /// <summary>
    /// 添加完成回调
    /// </summary>
    public void AddCompleteCallback(Action complete)
    {
        OnComplete += complete;
    }

    /// <summary>
    /// 设置角度检测参数
    /// </summary>
    public void SetAngleCheckParams(float threshold, float multiple, bool absolute = true)
    {
        angleThreshold = threshold;
        speedUpMultiple = multiple;
        useAbsoluteAngle = absolute;
    }

    public static Level43LoadingPopup Create(Action complete = null)
    {
        var panel = UIManager.Instance.OpenUI<Level43LoadingPopup>($"{typeof(Level43LoadingPopup).Name}", UILayer.Normal);
        panel.OnComplete = complete;
        return panel;
    }
}
