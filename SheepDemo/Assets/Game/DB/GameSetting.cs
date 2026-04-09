using System.Collections.Generic;
using UnityEngine;

namespace Bear.SaveModule
{
    // [CreateAssetMenu(fileName = "GameData", menuName = "Save Data/GameData")]
    public partial class GameSetting : BaseSaveDataSO
    {
        public static StorageType StorageType = StorageType.PlayerPrefs;

        // 背景音开关
        [SerializeField] private bool musicOn = true;
        // 音效开关
        [SerializeField] private bool sfxOn = true;
        // 振动开关
        [SerializeField] private bool vibrationOn = true;

        // 当前语言
        [SerializeField] private string currentLanguageKeyCode = "en";
    }
}
