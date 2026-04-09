using Game.Level;
using Game.Scripts.Common;
using UnityEngine;

public class Level23Ctrl : MonoBehaviour
{
    [SerializeField] private BaseLevelCtrl levelCtrl;

    // 通关
    public void ClickLevelSuccess()
    {
        AudioManager.PlaySound("success");
        levelCtrl.SuccessStrightly();
    }
}
