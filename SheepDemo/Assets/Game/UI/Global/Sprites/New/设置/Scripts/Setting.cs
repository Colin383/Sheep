using Bear.UI;
using UnityEngine;

public partial class Setting : BaseUIView
{
    public override void OnOpen()
    {
        base.OnOpen();
    }

    public override void OnCreate()
    {
        base.OnCreate();
        // TODO: 注册按钮等（示例见 PictureVerifticationPopup）
    }

    public override void OnClose()
    {
        base.OnClose();
    }

    public static Setting Create()
    {
        var panel = UIManager.Instance.OpenUI<Setting>($"{typeof(Setting).Name}", UILayer.Normal);
        return panel;
    }
}
