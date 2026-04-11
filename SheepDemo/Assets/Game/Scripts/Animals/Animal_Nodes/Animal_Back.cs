using Bear.Fsm;
using UnityEngine;

/// <summary>
/// Animal back state node. 处理返回农场的动画和销毁。
/// 当 animal 被 PathManager 控制时，由 PathManager 驱动移动和销毁。
/// 当没有 PathManager 时，按 backDuration 延迟销毁。
/// </summary>
[StateMachineNode(typeof(BaseAnimal), AnimalStateName.BACK, false)]
public class Animal_Back : StateNode
{
    private BaseAnimal owner;
    private float delayTimer;
    private bool isPathManaged;

    [Header("Back State")]
    [Tooltip("Back 状态持续时间（秒），动画播放完成后销毁。<=0 表示不自动销毁，需外部控制（如 PathManager）。警告：如使用 PathManager，请保持为 0！")]
    [SerializeField] private float backDuration = 0f;

    public override void OnEnter()
    {
        owner = _owner as BaseAnimal;
        delayTimer = 0f;
        isPathManaged = false;

        // 检查是否被 PathManager 控制
        if (owner != null && owner.Level != null)
        {
            // 如果 backDuration <= 0，表示由外部（PathManager）控制销毁
            isPathManaged = backDuration <= 0f;
        }

        Debug.Log($"[Animal_Back] OnEnter: {owner?.name}, isPathManaged={isPathManaged}, backDuration={backDuration}");
    }

    public override void OnUpdate()
    {
        // 如果被 PathManager 控制，不处理自动销毁
        if (isPathManaged)
            return;

        // 自动销毁模式
        if (backDuration <= 0f)
            return;

        delayTimer += Time.deltaTime;
        if (delayTimer >= backDuration)
        {
            DestroyAnimal();
        }
    }

    public override void OnExit()
    {
        owner = null;
    }

    /// <summary>
    /// 标记为被 PathManager 控制，禁止自动销毁
    /// </summary>
    public void SetPathManaged(bool managed)
    {
        isPathManaged = managed;
    }

    /// <summary>
    /// 销毁 animal，运行态用 Destroy，编辑态用 DestroyImmediate。
    /// </summary>
    private void DestroyAnimal()
    {
        if (owner == null)
            return;

        var go = owner.gameObject;
        owner = null;

        if (Application.isPlaying)
            Object.Destroy(go);
        else
            Object.DestroyImmediate(go);
    }

    /// <summary>
    /// 外部调用销毁 animal（如 PathManager 到达终点后调用）
    /// </summary>
    public void DestroyByExternal()
    {
        DestroyAnimal();
    }
}
