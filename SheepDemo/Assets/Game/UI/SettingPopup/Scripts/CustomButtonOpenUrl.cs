using UnityEngine;

/// <summary>
/// 与本物体上的 <see cref="CustomButton"/> 绑定：点击后用 <see cref="Application.OpenURL"/> 打开配置的链接。
/// 适用于设置页隐私政策、条款等外链按钮。
/// </summary>
[RequireComponent(typeof(CustomButton))]
public class CustomButtonOpenUrl : MonoBehaviour
{
    [Tooltip("完整 URL，例如 https://...")]
    [SerializeField]
    private string url;

    private CustomButton _button;

    private void Awake()
    {
        _button = GetComponent<CustomButton>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.OnClick += OnClickOpenUrl;
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.OnClick -= OnClickOpenUrl;
    }

    private void OnClickOpenUrl(CustomButton _)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Application.OpenURL(url.Trim());
    }
}
