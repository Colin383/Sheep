using System;
using Bear.EventSystem;
using Bear.Logger;
using Bear.SaveModule;
using Bear.UI;
using Game.ConfigModule;
using I2.Loc;
using UnityEngine;

public partial class LocalizationPopup : BaseUIView, IEventSender, IDebuger
{
    [SerializeField] private LanguageItem item;
    [SerializeField] private RectTransform content;

    private Action<string> onSelectLanguage;

    public override void OnCreate()
    {
        base.OnCreate();

        CloseBtn.OnClick += OnClickClose;
        InitLanguageItems();
    }

    private void InitLanguageItems()
    {
        if (item == null || content == null)
        {
            Debug.LogWarning("[LocalizationPopup] Language item prefab or content is not assigned.");
            return;
        }

        // 清空旧项
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        var languages = ConfigManager.Instance.Tables.TbLanguage.DataList;
        if (languages == null || languages.Count == 0)
        {
            Debug.LogWarning("[LocalizationPopup] No languages found from LocalizationManager.");
            return;
        }

        string currentLanguage = DB.GameSetting.CurrentLanguageKeyCode;

        this.Log($"------language: {currentLanguage}");

        foreach (var lang in languages)
        {
            if (!lang.IsOpen)
                return;
            var go = Instantiate(item, content);
            bool isSelected = string.Equals(lang.CorrespondingKey, currentLanguage, StringComparison.OrdinalIgnoreCase);
            go.Init(lang, isSelected, OnClickLanguageItem);
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();

        // 打开时根据当前语言刷新一次
        InitLanguageItems();
    }

    public static LocalizationPopup Create()
    {
        return Create(null);
    }

    public static LocalizationPopup Create(Action<string> onSelectLanguage)
    {
        var panel = UIManager.Instance.OpenUI<LocalizationPopup>($"{typeof(LocalizationPopup).Name}", UILayer.Popup);
        panel.onSelectLanguage = onSelectLanguage;
        return panel;
    }

    // 示例：关闭事件（生成字段后再解注释并绑定）
    private void OnClickClose(CustomButton btn)
    {
        // UIManager.Instance.CloseUI(this);
        UIManager.Instance.DestroyUI(this);
    }

    private void OnClickLanguageItem(LanguageItem itemView)
    {
        if (itemView == null)
            return;

        string key = itemView.GetLanguageKeyCode();
        if (string.IsNullOrEmpty(key))
            return;

        LocalizationManager.CurrentLanguageCode = key;
        onSelectLanguage?.Invoke(key);

        DB.GameSetting.CurrentLanguageKeyCode = key;
        DB.GameSetting.Save();

        Debug.Log("----------- Current LanguageCode: " + key);

        // 更新选中状态
        for (int i = 0; i < content.childCount; i++)
        {
            var childItem = content.GetChild(i).GetComponent<LanguageItem>();
            if (childItem != null)
            {
                childItem.SetSelected(childItem == itemView);
            }
        }
    }
}

