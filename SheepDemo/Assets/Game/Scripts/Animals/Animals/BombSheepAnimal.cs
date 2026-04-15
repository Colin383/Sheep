using System;
using UnityEngine;
using TMPro;
using Bear.EventSystem;
using Game.Events;
using Game.Scripts.Common;

/// <summary>
/// "sheep" implementation placeholder.
/// Add sheep-specific behavior here later.
/// </summary>
public class BombSheepAnimal : BaseAnimal, IEventSender
{
    [SerializeField] private TextMeshProUGUI leftFailCount;
    public override AnimalType Type => AnimalType.BombSheep;

    public const int MaxFailCount = 3;

    private int _currentFailCount = 0;
    private int _maxFailCount = MaxFailCount;

    public override void Init(int id, int row, int col, string direction, string param = null)
    {
        base.Init(id, row, col, direction, param);
        _currentFailCount = _maxFailCount;
        SyncFailCountUI();
    }

    protected override void ParseParam(string param)
    {
        if (!string.IsNullOrWhiteSpace(param) && int.TryParse(param, out var parsedCount) && parsedCount > 0)
        {
            _maxFailCount = parsedCount;
        }
        else
        {
            _maxFailCount = MaxFailCount;
        }
    }

    protected override void FailToMove()
    {
        _currentFailCount--;
        SyncFailCountUI();

        if (_currentFailCount <= 0)
        {
            Level?.DestroyAnimal(this);
            this.DispatchEvent(Witness<GameFailedEvent>._, GameFailedType.Bomb);

            AudioManager.PlaySound("bomb");
        }
    }

    private void SyncFailCountUI()
    {
        if (leftFailCount != null)
            leftFailCount.text = _currentFailCount.ToString();
    }
}

