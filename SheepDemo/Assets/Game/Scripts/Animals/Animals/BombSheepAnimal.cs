using System;
using UnityEngine;
using TMPro;
using Bear.EventSystem;
using Game.Events;

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

    public override void Init(int id, int row, int col, string direction)
    {
        base.Init(id, row, col, direction);
        _currentFailCount = MaxFailCount;
        SyncFailCountUI();
    }

    protected override void FailToMove()
    {
        _currentFailCount--;
        SyncFailCountUI();

        if (_currentFailCount <= 0)
        {
            this.DispatchEvent(Witness<GameFailedEvent>._);
        }
    }

    private void SyncFailCountUI()
    {
        if (leftFailCount != null)
            leftFailCount.text = _currentFailCount.ToString();
    }
}

