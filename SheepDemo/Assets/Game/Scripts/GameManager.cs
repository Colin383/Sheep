using Bear.Logger;
using Bear.UI;
using Game;
using Game.Common;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>, IDebuger
{
    [SerializeField] private Camera StartCamera;

    public PlayCtrl @PlayCtrl { get; private set; }

    public PurchaseManager Purchase { get; private set; }

    // Start is called before the first frame update
    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        DontDestroyOnLoad(gameObject);
    }

    public void Init()
    {
        Purchase = new PurchaseManager();
    }

    public void ReadyToPlay()
    {
        @PlayCtrl = PlayCtrl.Instance;
        @PlayCtrl.Init();
    }

    public void CloseCamera()
    {
        StartCamera.gameObject.SetActive(false);
    }

    public void OpenCamera()
    {
        StartCamera.gameObject.SetActive(true);
    }

    void Update()
    {
        if (@PlayCtrl != null)
            @PlayCtrl.Update();
    }

    private void OnApplicationQuit()
    {
        ClearAll();
    }

    private void ClearAll()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.DestroyAllUI();
        }
    }

    protected override void OnDestroy()
    {
        if (@PlayCtrl != null)
            @PlayCtrl.OnDestroy();
    }
}
