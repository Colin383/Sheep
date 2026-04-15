using UnityEngine;

/// <summary>
/// 动物动画参数控制器。
/// 统一封装 Animator 的 walk(bool) 与 knock(trigger) 控制。
/// </summary>
public class AnimAnimtorCtrl : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("为空时自动从自身或子节点查找 Animator。")]
    [SerializeField] private Animator targetAnimator;

    [SerializeField] private bool findInChildrenIfNull = true;

    [Header("Params")]
    [SerializeField] private string walkBoolName = "walk";
    [SerializeField] private string knockTriggerName = "knock";
    [SerializeField] private string jumpTriggerName = "jump";

    private int walkBoolHash;
    private int knockTriggerHash;
    private int jumpTriggerHash;
    private bool hashReady;

    private void Awake()
    {
        ResolveAnimator();
        CacheHashes();
    }

    private void OnValidate()
    {
        CacheHashes();
    }

    /// <summary>
    /// 设置 walk bool 参数。
    /// </summary>
    public bool SetWalk(bool walking)
    {
        if (!TryGetAnimator(out var animator))
            return false;

        if (!hashReady)
            CacheHashes();

        if (walkBoolHash == 0)
            return false;

        animator.SetBool(walkBoolHash, walking);
        return true;
    }

    /// <summary>
    /// 触发 jump 动画。
    /// </summary>
    public bool PlayJump()
    {
        if (!TryGetAnimator(out var animator))
            return false;

        if (!hashReady)
            CacheHashes();

        if (jumpTriggerHash == 0)
            return false;

        animator.SetTrigger(jumpTriggerHash);
        return true;
    }

    /// <summary>
    /// 重置 jump trigger。
    /// </summary>
    public bool ResetJump()
    {
        if (!TryGetAnimator(out var animator))
            return false;

        if (!hashReady)
            CacheHashes();

        if (jumpTriggerHash == 0)
            return false;

        animator.ResetTrigger(jumpTriggerHash);
        return true;
    }

    /// <summary>
    /// 触发 knock 动画。
    /// </summary>
    public bool PlayKnock()
    {
        if (!TryGetAnimator(out var animator))
            return false;

        if (!hashReady)
            CacheHashes();

        if (knockTriggerHash == 0)
            return false;

        animator.SetTrigger(knockTriggerHash);
        return true;
    }

    /// <summary>
    /// 重置 knock trigger。
    /// </summary>
    public bool ResetKnock()
    {
        if (!TryGetAnimator(out var animator))
            return false;

        if (!hashReady)
            CacheHashes();

        if (knockTriggerHash == 0)
            return false;

        animator.ResetTrigger(knockTriggerHash);
        return true;
    }

    private bool TryGetAnimator(out Animator animator)
    {
        animator = ResolveAnimator();
        return animator != null;
    }

    private Animator ResolveAnimator()
    {
        if (targetAnimator != null)
            return targetAnimator;

        targetAnimator = GetComponent<Animator>();
        if (targetAnimator != null)
            return targetAnimator;

        if (findInChildrenIfNull)
            targetAnimator = GetComponentInChildren<Animator>(true);

        return targetAnimator;
    }

    private void CacheHashes()
    {
        walkBoolHash = string.IsNullOrWhiteSpace(walkBoolName) ? 0 : Animator.StringToHash(walkBoolName);
        knockTriggerHash = string.IsNullOrWhiteSpace(knockTriggerName) ? 0 : Animator.StringToHash(knockTriggerName);
        jumpTriggerHash = string.IsNullOrWhiteSpace(jumpTriggerName) ? 0 : Animator.StringToHash(jumpTriggerName);
        hashReady = true;
    }
}
