using UnityEngine;
using Bear.EventSystem;
using Bear.UI;
using Config;
using Game.Events;
using I2.Loc;
using Bear.Logger;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public partial class GameVictoryPanel : BaseUIView, IEventSender, IDebuger
{
    // [SerializeField] private TextMeshProUGUI title;
    // [SerializeField] private TextMeshProUGUI tips;
    [SerializeField] private CanvasGroup nextBtn;

    // [SerializeField] private UISpineCtrl spineCtrl;
    private string _tipsKey;

    public override void OnCreate()
    {
        base.OnCreate();
        NextBtn.OnClick += OnNextLevel;
    }

    public override void OnOpen()
    {
        base.OnOpen();

        // NextBtn.Interactable = false;
        // OpenAnimation();
    }

/*     private void OpenAnimation()
    {
        var dc = new Color(1, 1, 1, 1);
        title.color = dc;
        tips.text = "";
        nextBtn.alpha = 0;

        var track = spineCtrl != null ? spineCtrl.PlayAnimation("in", false) : null;

        var state = spineCtrl != null ? spineCtrl.GetAnimationState() : null;
        if (state != null)
        {
            state.Event += OnSpineEvent;
            state.Complete += (track) =>
            {
                RefreshTips();
                spineCtrl.PlayAnimation("idle", true);
            };
        }
    }
 */
    private void OnSpineEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {
        if (trackEntry == null || trackEntry.Animation == null || e == null)
            return;

        if (!string.Equals(trackEntry.Animation.Name, "in", System.StringComparison.OrdinalIgnoreCase))
            return;

        // 如果需要按事件名区分不同事件帧，可以在这里判断 e.Data.Name
        // 例如：if (e.Data.Name == "ShowNextBtn") { ... }
        if (e.Data.Name == "Congratulations")
        {
            // title.DOFade(1, 0.2f);
        }
        else if (e.Data.Name == "NextLevel")
        {
            nextBtn.DOFade(1, 0.2f).SetDelay(0.15f).SetUpdate(true);
            Debug.Log("Text ----------- ");
        }
    }

    /// <summary>
    /// 根据 data.CongraktTips 依赖 localization 组件获取 Tips 文本并写入 TipsTxt。
    /// </summary>
    public void RefreshTips()
    {
        var key = _tipsKey;
        if (string.IsNullOrEmpty(key))
        {
            this.LogWarning("[GameVictoryPanel] RefreshTips: CongraktTips key is null or empty.");
            return;
        }

        var translated = LocalizationManager.GetTranslation(key, false);
        if (string.IsNullOrEmpty(translated))
        {
            this.LogError($"Translate key: {key} lost!");
            NextBtn.Interactable = true;
            return;
        }

       /*  TipsTxt.DOText(translated, 1f).SetUpdate(true).OnComplete(() =>
        {
            NextBtn.Interactable = true;
        }); */

        Debug.Log($"[GameVictoryPanel] RefreshTips: key={key}, text=\"{translated}\"");
    }

    private void OnNextLevel(CustomButton btn)
    {
        UIManager.Instance.CloseUI(this);
        this.DispatchEvent(Witness<EnterNextLevelEvent>._);
    }

    public static GameVictoryPanel Create()
    {
        var panel = UIManager.Instance.OpenUI<GameVictoryPanel>($"{typeof(GameVictoryPanel).Name}", UILayer.Popup);
        return panel;
    }
}
