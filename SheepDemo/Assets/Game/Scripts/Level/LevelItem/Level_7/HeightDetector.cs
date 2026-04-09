using UnityEngine;
using Game.ItemEvent;

/// <summary>
/// 高度检测执行器：绑定物体，设置检测高度，超过高度后松手下落，碰撞到指定 layer 时触发 Execute 并重置状态
/// </summary>
public class HeightDetector : BaseItemExecutor
{
    [Header("绑定设置")]
    [Tooltip("要检测的目标物体（为空则检测自身）")]
    [SerializeField] private Transform targetObject;
    
    [Header("检测设置")]
    [Tooltip("检测高度阈值（世界坐标Y值），超过此高度后松手才会触发检测")]
    [SerializeField] private float heightThreshold = 5.0f;
    
    [Tooltip("碰撞检测的 Layer 遮罩")]
    [SerializeField] private LayerMask collisionLayerMask = 1 << 0; // 默认 Default 层
    
    [Header("调试")]
    [Tooltip("是否显示调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Transform targetTransform;
    private DragableItem dragableItem;
    private bool hasExceededHeight = false;
    private bool wasDragging = false;
    private bool isWaitingForCollision = false;
    
    void Awake()
    {
        // 确定检测目标
        if (targetObject != null)
        {
            targetTransform = targetObject;
        }
        else
        {
            targetTransform = transform;
        }
        
        // 获取 DragableItem 组件（用于检测松手状态）
        dragableItem = targetTransform.GetComponent<DragableItem>();
        if (dragableItem == null)
        {
            dragableItem = targetTransform.GetComponentInParent<DragableItem>();
        }
    }
    
    void Update()
    {
        if (targetTransform == null)
            return;
        
        CheckHeightAndRelease();

        OnUpdate();
    }
    
    private void CheckHeightAndRelease()
    {
        float currentHeight = targetTransform.position.y;
        bool isDragging = dragableItem != null && dragableItem.IsDragging();
        
        // 检测是否超过高度阈值
        if (!hasExceededHeight && currentHeight > heightThreshold)
        {
            hasExceededHeight = true;
            if (showDebugInfo)
            {
                Debug.Log($"[HeightDetector] {targetTransform.name} 超过高度阈值 {heightThreshold:F2}");
            }
        }
        
        // 检测松手状态
        if (hasExceededHeight)
        {
            // 如果之前正在拖拽，现在松手了
            if (wasDragging && !isDragging)
            {
                isWaitingForCollision = true;
                if (showDebugInfo)
                {
                    Debug.Log($"[HeightDetector] {targetTransform.name} 超过高度后松手，开始等待碰撞");
                }
            }
            
            wasDragging = isDragging;
        }
        else
        {
            wasDragging = isDragging;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 只有在超过高度、松手下落、等待碰撞的状态下才检测
        if (!hasExceededHeight || !isWaitingForCollision)
            return;
        
        // 检查是否碰撞到指定的 Layer
        if ((collisionLayerMask.value & (1 << collision.gameObject.layer)) != 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[HeightDetector] {targetTransform.name} 碰撞到指定 Layer: {collision.gameObject.name}，触发 Execute");
            }
            
            // 触发 Execute
            Execute();
            
            // 重置状态，允许再次触发
            ResetState();
        }
    }

    
    /// <summary>
    /// 重置状态（允许再次触发）
    /// </summary>
    private void ResetState()
    {
        hasExceededHeight = false;
        isWaitingForCollision = false;
        wasDragging = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"[HeightDetector] {targetTransform.name} 状态已重置");
        }
    }
    
    /// <summary>
    /// 手动重置状态
    /// </summary>
    public void ResetTrigger()
    {
        ResetState();
    }
    
    /// <summary>
    /// 设置高度阈值
    /// </summary>
    public void SetHeightThreshold(float height)
    {
        heightThreshold = height;
    }
    
    /// <summary>
    /// 获取当前高度
    /// </summary>
    public float GetCurrentHeight()
    {
        if (targetTransform == null)
            return 0f;
        return targetTransform.position.y;
    }
    
    /// <summary>
    /// 设置检测目标
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetObject = target;
        if (targetObject != null)
        {
            targetTransform = targetObject;
        }
        else
        {
            targetTransform = transform;
        }
        
        // 重新获取 DragableItem
        dragableItem = targetTransform.GetComponent<DragableItem>();
        if (dragableItem == null)
        {
            dragableItem = targetTransform.GetComponentInParent<DragableItem>();
        }
    }
    
    // 在编辑器中可视化高度阈值
    void OnDrawGizmosSelected()
    {
        Transform drawTarget = targetObject != null ? targetObject : transform;
        
        if (drawTarget == null)
            return;
        
        Vector3 thresholdPos = new Vector3(drawTarget.position.x, heightThreshold, drawTarget.position.z);
        
        // 绘制高度阈值线
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(drawTarget.position.x - 1f, heightThreshold, drawTarget.position.z),
            new Vector3(drawTarget.position.x + 1f, heightThreshold, drawTarget.position.z)
        );
        
        // 绘制从物体到高度阈值的线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(drawTarget.position, thresholdPos);
    }
}
