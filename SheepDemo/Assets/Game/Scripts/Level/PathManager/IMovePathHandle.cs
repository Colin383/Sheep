using UnityEngine;


/// <summary>
/// 用于控制物体沿路径位移的接口
/// </summary>
public interface IMovePathHandle
{
    /// <summary>
    /// 移动速度（单位/秒）
    /// </summary>
    float MoveSpeed { get; }
    
    /// <summary>
    /// 转向速度（度/秒）
    /// </summary>
    float RotateSpeed { get; }
    
    /// <summary>
    /// 路径移动完成回调
    /// </summary>
    void OnComplete();
}
