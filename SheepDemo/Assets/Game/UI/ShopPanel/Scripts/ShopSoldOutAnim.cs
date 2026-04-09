using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 售罄标识简单入场动画。
/// 独立脚本，方便在预制上复用与管理。
/// </summary>
public class ShopSoldOutAnim : MonoBehaviour
{
    [SerializeField] private Image soldOutImage;

    private bool hasPlay = false;

    private Sequence _sequence;

    private void Awake()
    {
        if (soldOutImage == null)
            soldOutImage = GetComponent<Image>();
    }

    /// <summary>
    /// 播放售罄动画。
    /// </summary>
    public void Play()
    {
        if (soldOutImage == null || hasPlay)
            return;

        // hasPlay = true;
        _sequence?.Kill();

        _sequence = DOTween.Sequence();
        soldOutImage.DOFade(0f, 0f).SetUpdate(true);

        var rt = soldOutImage.rectTransform;

        var pos = rt.anchoredPosition;
        pos.y += 300f;
        rt.anchoredPosition = pos;

        _sequence.Append(rt.DOAnchorPosY(0f, .3f).SetUpdate(true));
        _sequence.JoinCallback(() =>
        {
            soldOutImage.DOFade(1f, 0.5f).SetUpdate(true);
        });
        _sequence.Join(rt.DOPunchRotation(Vector3.forward * 8f, .3f, 3, 2).SetDelay(.1f).SetUpdate(true));

        _sequence.SetUpdate(true);
    }

    private void OnDestroy()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
            _sequence = null;
        }
    }
}

