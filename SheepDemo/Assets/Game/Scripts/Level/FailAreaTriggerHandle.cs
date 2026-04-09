using Bear.EventSystem;
using Game.Events;
using Game.Scripts.Common;
using UnityEngine;

public class FailAreaTriggerHandle : MonoBehaviour, IEventSender
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            var actor = collision.gameObject.GetComponent<ActorCtrl>();
            if (actor != null && !actor.IsDied)
            {
                this.DispatchEvent(Witness<OnTriggerFailAreaEvent>._);
            }
        }
    }
}
