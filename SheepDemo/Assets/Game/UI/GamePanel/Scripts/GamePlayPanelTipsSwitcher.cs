using Bear.EventSystem;
using Game.Events;
using Game.ItemEvent;
using UnityEngine;

public class GamePlayPanelTipsSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject target;

    private EventSubscriber _subscriber;

    private void Awake()
    {
        EventsUtils.ResetEvents(ref _subscriber);
        _subscriber.Subscribe<GamePlayPanelSwitchTipsEvent>(OnSwitchTips);

        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private void OnSwitchTips(GamePlayPanelSwitchTipsEvent evt)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(evt.IsShow);
        Debug.Log("Tips Warnging : " + evt.IsShow);
        if (evt.IsShow)
        {
            target.GetComponent<RotateFloatHandle>().ResetToStart();
        }
    }

    private void OnDestroy()
    {
        EventsUtils.ResetEvents(ref _subscriber);
    }
}

