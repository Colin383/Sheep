using System;
using System.Diagnostics;
using System.Globalization;
using I2.Loc;

public static class LocalizatioinUtils
{
    private const string DefaultLanguageKeyCode = "en";

    /// <summary>
    /// 根据系统 UI 语言获取 LocalizationManager 支持的语言 keyCode。
    /// 默认使用 CultureInfo.CurrentUICulture.TwoLetterISOLanguageName，经 LocalizationManager.GetLanguageFromCode 解析；
    /// 若不支持则返回 "en"。
    /// </summary>
    /// <param name="systemCode">系统语言代码，如 "en","zh"；为 null 时使用 CurrentUICulture.TwoLetterISOLanguageName。</param>
    /// <returns>LocalizationManager 使用的语言 keyCode。</returns>
    public static string GetCodeFromSystemCode(string systemCode = null)
    {
        string code = systemCode ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (string.IsNullOrEmpty(code))
            return DefaultLanguageKeyCode;

        code = code.ToLowerInvariant();

        // 中文用完整区域名区分简体/繁体
        if (code == "zh")
        {
            string name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase))
                code = "zh-TW";
            else
                code = "zh-CN";
        }

        return code;
        // string language = LocalizationManager.GetLanguageFromCode(code);
        // return string.IsNullOrEmpty(language) ? DefaultLanguageKeyCode : language;
    }
}
