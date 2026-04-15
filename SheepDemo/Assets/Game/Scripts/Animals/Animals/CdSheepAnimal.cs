using System;
using UnityEngine;
using TMPro;
using Bear.EventSystem;
using Game.Events;
using Game.Scripts.Common;

/// <summary>
/// 倒计时羊：生成后开始倒计时，时间到则发送游戏失败消息。
/// 成功到达终点后停止计时。
/// </summary>
public class CdSheepAnimal : BaseAnimal, IEventSender
{
    [SerializeField] private TextMeshProUGUI cdText;
    public override AnimalType Type => AnimalType.CdSheep;

    [Tooltip("默认等待时间（秒），当 param 为空或解析失败时使用此值。")]
    [SerializeField] private float defaultWaitingTime = 10f;

    private float _currentTime;
    private float _targetWaitingTime;
    private bool _isTiming;

    public override void Init(int id, int row, int col, string direction, string param = null)
    {
        base.Init(id, row, col, direction, param);
        _currentTime = _targetWaitingTime;
        // _isTiming = true;
        SyncCdUI();
    }

    protected override void ParseParam(string param)
    {
        if (!string.IsNullOrWhiteSpace(param) && float.TryParse(param, out var parsedTime) && parsedTime > 0f)
        {
            _targetWaitingTime = parsedTime;
        }
        else
        {
            _targetWaitingTime = defaultWaitingTime;
        }
    }

    public void StartCD()
    {
        _isTiming = true;
    }

    protected override void Update()
    {
        base.Update();

        if (!_isTiming)
            return;

        _currentTime -= Time.deltaTime;
        SyncCdUI();

        if (_currentTime <= 0f)
        {
            _currentTime = 0f;
            _isTiming = false;
            Level?.DestroyAnimal(this);
            this.DispatchEvent(Witness<GameFailedEvent>._, GameFailedType.Bomb);

            AudioManager.PlaySound("bomb");
        }
    }

    public override void OnComplete()
    {
        base.OnComplete();
        _isTiming = false;
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        _isTiming = false;
        _currentTime = _targetWaitingTime;
        SyncCdUI();
    }

    private void SyncCdUI()
    {
        if (cdText != null)
            cdText.text = Mathf.CeilToInt(_currentTime).ToString();
    }
}
