using Bear.UI;
using I2.Loc;
using UnityEngine;

public partial class Announce : BaseUIView
{
    [SerializeField] private Animator anim;
    [Header("自动关闭时间（秒）")]
    [SerializeField] private float duration = 2f;

    private float remainingTime;
    private bool isTiming;

    public override void OnOpen()
    {
        base.OnOpen();
        StartAutoCloseTimer();
    }

    private void SetLevel(int level)
    {
        LevelTxt.text = string.Format(LocalizationManager.GetTranslation("U_Annouce_des_01"), level);
    }

    private void OnDisable()
    {
        StopAutoCloseTimer();
    }

    private void Update()
    {
        if (!isTiming)
        {
            return;
        }

        // Keep realtime behavior like WaitForSecondsRealtime.
        remainingTime -= Time.unscaledDeltaTime;
        if (remainingTime <= 0f)
        {
            isTiming = false;
            UIManager.Instance.CloseUI(this);
        }
    }

    private void StartAutoCloseTimer()
    {
        if (duration <= 0f)
        {
            return;
        }

        StopAutoCloseTimer();
        remainingTime = duration;
        isTiming = true;
    }

    private void StopAutoCloseTimer()
    {
        isTiming = false;
        remainingTime = 0f;
    }

    public static Announce Create(int level, float duration = -1f)
    {
        var panel = UIManager.Instance.OpenUI<Announce>(nameof(Announce), UILayer.Top, false);
        if (duration > 0f)
        {
            panel.duration = duration;
        }

        panel.SetLevel(level);
        // panel.anim.Play("");

        return panel;
    }

    public static void CloseStraightly()
    {
        UIManager.Instance.CloseUI<Announce>();
    }
}

