using Bear.EventSystem;
using Cysharp.Threading.Tasks;
using Game.Events;
using UnityEngine;

/// <summary>
/// 刷新 BoxCollider2D 的 size 和 offset。
/// 优先根据 MeshRenderer.localBounds（本地边界）计算；无则用 SpriteRenderer；否则用手动数值。
/// 监听 SwitchLanguageEvent 时会在切换语言后自动刷新。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BoxCollider2DRefresh : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;
    [Tooltip("优先使用：用其 localBounds 计算 BoxCollider2D 的 size/offset（取 XY）")]
    [SerializeField] private MeshRenderer meshRenderer;
    [Tooltip("无 MeshRenderer 时使用：用 sprite 边界计算")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("刷新方式")]
    [Tooltip("为 true 时按 MeshRenderer 或 SpriteRenderer 边界刷新；为 false 时使用下面填写的 Size/Offset")]
    [SerializeField] private bool fitToRenderer = true;

    [Header("手动数值（fitToRenderer 为 false 时使用）")]
    [SerializeField] private Vector2 size = new Vector2(1f, 1f);
    [SerializeField] private Vector2 offset = Vector2.zero;

    private EventSubscriber _subscriber;

    private void Reset()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        meshRenderer = GetComponent<MeshRenderer>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

    }

    private void Start()
    {
        AddListener();
        WaitingRefresh().Forget();
    }

    async UniTask WaitingRefresh()
    {
        await UniTask.WaitForEndOfFrame();

        Refresh();
    }

    private void OnDestroy()
    {
        EventsUtils.ResetEvents(ref _subscriber);
    }

    private void AddListener()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<SwitchLanguageEvent>(OnSwitchLanguage);
    }

    private void OnSwitchLanguage(SwitchLanguageEvent evt)
    {
        Refresh();
    }

    [ContextMenu("Refresh Size And Offset")]
    /// <summary>
    /// 刷新 BoxCollider2D 的 size 和 offset。
    /// 优先按 MeshRenderer.localBounds（XY）；无则按 SpriteRenderer；否则用手动 size/offset。
    /// </summary>
    public void Refresh()
    {
        if (boxCollider == null)
            return;

        if (fitToRenderer && TryGetBoundsFromRenderers(out Vector2 boundsSize, out Vector2 boundsCenter))
        {
            boxCollider.size = boundsSize;
            boxCollider.offset = boundsCenter;
        }
        else
        {
            boxCollider.size = size;
            boxCollider.offset = offset;
        }
    }

    /// <summary>
    /// 优先从 MeshRenderer.localBounds 取本地边界 XY；无则从 SpriteRenderer.sprite.bounds。
    /// </summary>
    private bool TryGetBoundsFromRenderers(out Vector2 boundsSize, out Vector2 boundsCenter)
    {
        boundsSize = Vector2.zero;
        boundsCenter = Vector2.zero;

        if (meshRenderer != null)
        {
            Bounds b = meshRenderer.localBounds;
            boundsSize = new Vector2(b.size.x, b.size.y);
            boundsCenter = new Vector2(b.center.x, b.center.y);
            return true;
        }

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds b = spriteRenderer.sprite.bounds;
            boundsSize = new Vector2(b.size.x, b.size.y);
            boundsCenter = new Vector2(b.center.x, b.center.y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 使用指定 size 和 offset 刷新（不修改序列化字段）
    /// </summary>
    public void SetSizeAndOffset(Vector2 newSize, Vector2 newOffset)
    {
        if (boxCollider == null)
            return;

        boxCollider.size = newSize;
        boxCollider.offset = newOffset;
    }
}
