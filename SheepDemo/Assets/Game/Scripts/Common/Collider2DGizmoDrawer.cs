using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 2D碰撞盒Gizmos绘制器 - 在编辑模式下显示2D碰撞盒的大小
/// 将此脚本附加到有Collider2D组件的GameObject上即可使用
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collider2DGizmoDrawer : MonoBehaviour
{
    [Header("Gizmos设置")]
    [Tooltip("Gizmos颜色")]
    [SerializeField] private Color gizmoColor = Color.green;
    
    [Tooltip("Gizmos线条粗细")]
    [Range(1f, 10f)]
    [SerializeField] private float lineWidth = 2f;
    
    [Tooltip("是否填充碰撞盒")]
    [SerializeField] private bool fillCollider = false;
    
    [Tooltip("填充透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float fillAlpha = 0.2f;
    
    private Collider2D collider2D;
    
    void OnValidate()
    {
        if (collider2D == null)
            collider2D = GetComponent<Collider2D>();
    }
    
    void OnDrawGizmos()
    {
        if (collider2D == null)
            collider2D = GetComponent<Collider2D>();
        
        if (collider2D == null)
            return;
        
        // 获取碰撞盒的边界
        Bounds bounds = collider2D.bounds;
        
        // 计算四个角的世界坐标
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        
        Vector3 topLeft = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f);
        Vector3 topRight = center + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
        Vector3 bottomLeft = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0f);
        Vector3 bottomRight = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, 0f);
        
        // 绘制边框
        Handles.color = gizmoColor;
        Handles.DrawLine(topLeft, topRight, lineWidth);
        Handles.DrawLine(topRight, bottomRight, lineWidth);
        Handles.DrawLine(bottomRight, bottomLeft, lineWidth);
        Handles.DrawLine(bottomLeft, topLeft, lineWidth);
        
        // 绘制填充
        if (fillCollider)
        {
            Color fillColor = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, fillAlpha);
            Handles.color = fillColor;
            
            Vector3[] corners = new Vector3[]
            {
                topLeft,
                topRight,
                bottomRight,
                bottomLeft
            };
            
            Handles.DrawAAConvexPolygon(corners);
        }
        
        // 绘制中心点
        Handles.color = gizmoColor;
        float centerSize = Mathf.Min(size.x, size.y) * 0.05f;
        Handles.DrawLine(center + Vector3.up * centerSize, center + Vector3.down * centerSize, lineWidth);
        Handles.DrawLine(center + Vector3.left * centerSize, center + Vector3.right * centerSize, lineWidth);
    }
    
    void OnDrawGizmosSelected()
    {
        // 选中时也可以显示，使用相同的绘制逻辑
        OnDrawGizmos();
    }
}
#endif
