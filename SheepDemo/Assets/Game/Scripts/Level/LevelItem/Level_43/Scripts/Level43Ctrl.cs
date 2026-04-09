using Bear.UI;
using UnityEngine;

/// <summary>
/// Level43 关卡控制器，负责创建和管理 Level43LoadingPopup 窗口
/// 倒计时结束后显示钥匙，玩家获取钥匙通关
/// </summary>
public class Level43Ctrl : MonoBehaviour
{
    [SerializeField] private GameObject[] stuffs;
    private Level43LoadingPopup loadingPopup;

    void Start()
    {
        SwitchAllStuffs(false);
        // 创建加载弹窗
        InitLoadingPopup();
    }

    /// <summary>
    /// 显示加载弹窗并开始倒计时
    /// </summary>
    public void InitLoadingPopup()
    {
        loadingPopup = PlayCtrl.Instance.CurrentGamePlayPanel.GetComponentInChildren<Level43LoadingPopup>();
        loadingPopup.SetCompleteCallback(OnCountdownComplete);
    }

    /// <summary>
    /// 关闭加载弹窗
    /// </summary>
    public void CloseLoadingPopup()
    {
        if (loadingPopup != null)
        {
            UIManager.Instance.DestroyUI(loadingPopup);
            loadingPopup = null;
        }
    }

    /// <summary>
    /// 倒计时结束回调
    /// </summary>
    public void OnCountdownComplete()
    {
        SwitchAllStuffs(true);
        // 关闭弹窗
        CloseLoadingPopup();
    }

    void SwitchAllStuffs(bool isShow)
    {
        for (int i = 0; i < stuffs.Length; i++)
        {
            stuffs[i].SetActive(isShow);
        }
    }

    void OnDestroy()
    {
        // 确保清理
        CloseLoadingPopup();
    }
}
