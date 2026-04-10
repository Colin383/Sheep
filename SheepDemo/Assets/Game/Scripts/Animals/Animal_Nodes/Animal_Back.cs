using Bear.Fsm;
using UnityEngine;

/// <summary>
/// Animal back state node. 处理返回农场的动画和销毁。
/// </summary>
[StateMachineNode(typeof(BaseAnimal), AnimalStateName.BACK, false)]
public class Animal_Back : StateNode
{
    private BaseAnimal owner;
    private float delayTimer;

    [Header("Back State")]
    [Tooltip("Back 状态持续时间（秒），动画播放完成后销毁。<=0 表示不自动销毁，需外部控制。")]
    [SerializeField] private float backDuration = 0.5f;

    public override void OnEnter()
    {
        owner = _owner as BaseAnimal;
        delayTimer = 0f;
    }

    public override void OnUpdate()
    {
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
}
