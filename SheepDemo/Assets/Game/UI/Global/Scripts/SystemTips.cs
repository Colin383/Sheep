using Bear.Logger;
using Bear.UI;
using Config;
using DG.Tweening;
using Game.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Global 信息提示
/// </summary>
public class SystemTips : MonoBehaviour, IRecycle, IDebuger
{
    // move dotween
    Sequence sq;
    private RectTransform Rt { get; set; }
    [SerializeField] private RectTransform Target;

    [SerializeField] private TextMeshProUGUI Content;

    bool isSpawn = false;

    public static SystemTips Create()
    {
        var prefab = Resources.Load<SystemTips>("SystemTips");
        var tips = Instantiate(prefab);
        return tips;
    }

    public static SystemTips Show(string content)
    {
        var topLayer = UIManager.Instance.transform.Find("UIRoot/Top");
        return Show(topLayer, content);
    }

    /// <summary>
    /// 展示
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static SystemTips Show(Transform parent, string content)
    {
        var tips = ObjectPoolManager.Instance.Get<SystemTips>();
        if (tips == null)
        {
            return null;
        }
        tips.transform.SetParent(parent);

        tips.Rt = (tips.transform as RectTransform);
        tips.Rt.anchoredPosition = Vector2.zero + new Vector2(0, 100f);

        var y = tips.Rt.sizeDelta.y / 2f + 100f;
        tips.Target.anchoredPosition = new Vector2(0, y);

        tips.Content.text = content;
        tips.Play();

        return tips;
    }

    // 播放动画
    private void Play()
    {
        var y = -Rt.sizeDelta.y / 2f;

        var img = Target.GetComponent<Image>();
        var color = img.color;
        color.a = 0f;
        img.color = color;

        sq = DOTween.Sequence();
        sq.Append(Target.DOAnchorPosY(100f, 0.3f).SetEase(Ease.OutQuad));
        sq.Join(img.DOFade(1, 0.5f));
        sq.AppendInterval(1f);
        sq.Append(Target.DOAnchorPosY(y + 100f, 0.5f).SetEase(Ease.InQuad));
        sq.Join(img.DOFade(0, 0.5f));

        sq.OnComplete(OnComplete);

        sq.SetUpdate(true);
    }

    public void OnRecycle()
    {
        isSpawn = false;
        this.Log("Recycle back");
    }

    public void OnSpawn()
    {
        isSpawn = true;
    }

    private void OnDestroy()
    {
        OnComplete();
    }
    private void OnComplete()
    {
        if (this == null || !isSpawn)
            return;
        ObjectPoolManager.Instance.Recycle<SystemTips>(this);

        if (sq != null)
        {
            sq.Kill();
            sq = null;
        }
    }
}
