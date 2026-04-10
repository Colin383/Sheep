using UnityEngine;

/// <summary>
/// 与关卡 <c>*.game.json</c> 中 <c>direction</c> 字段对应：「up」/「right」/「down」/「left」。
/// 世界含义（2D 约定）：up = +Y，down = -Y，right = +X，left = -X（Z 用作深度/排序）。
/// </summary>
public enum DirectionEnum
{
    Up,
    Right,
    Down,
    Left,
}

/// <summary>
/// 与 json 字符串互转、世界旋转等工具方法。
/// </summary>
public static class DirectionEnumUtility
{
    /// <summary>配置缺省、无法解析时使用：朝下（-Z）。</summary>
    public const DirectionEnum Default = DirectionEnum.Down;

    public static DirectionEnum ParseOrDefault(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Default;

        switch (value.Trim().ToLowerInvariant())
        {
            case "up":
                return DirectionEnum.Up;
            case "right":
                return DirectionEnum.Right;
            case "down":
                return DirectionEnum.Down;
            case "left":
                return DirectionEnum.Left;
            default:
                return Default;
        }
    }

    public static string ToJsonString(DirectionEnum direction)
    {
        switch (direction)
        {
            case DirectionEnum.Up:
                return "up";
            case DirectionEnum.Right:
                return "right";
            case DirectionEnum.Down:
                return "down";
            case DirectionEnum.Left:
                return "left";
            default:
                return "down";
        }
    }

    public static Quaternion ToWorldRotation(DirectionEnum direction)
    {
        Vector3 forward;
        switch (direction)
        {
            case DirectionEnum.Up:
                forward = Vector3.forward;
                break;
            case DirectionEnum.Down:
                forward = Vector3.back;
                break;
            case DirectionEnum.Right:
                forward = Vector3.right;
                break;
            case DirectionEnum.Left:
                forward = Vector3.left;
                break;
            default:
                forward = Vector3.back;
                break;
        }

        return Quaternion.LookRotation(forward, Vector3.up);
    }
}
