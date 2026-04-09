using Bear.EventSystem;
using Cysharp.Threading.Tasks;
using Game.Events;
using Game.Scripts.Common;
using UnityEngine;

/// <summary>
/// 用于陷阱触发
/// </summary>
public class OnTriggerTrapHandle : MonoBehaviour, IEventSender
{
    [SerializeField] private ActorCtrl actor;
    [SerializeField] private ParticleSystem knock;
    private bool isDead = false;

    void OnTriggerEnter2D(Collider2D collider)
    {
        var tag = collider.gameObject.tag;
        var layerName = LayerMask.LayerToName(collider.gameObject.layer);
        if (layerName.Equals("Actor") && tag.Equals("Player"))
        {
            PlayKnock(collider);
        }
            // this.DispatchEvent(Witness<OnTriggerTrapEvent>._);
    }

    public void PlayKnock(Collider2D collider)
    {
        if (isDead)
            return;

        isDead = true;
        knock.transform.position = actor.transform.position;
        knock.Play();

        actor.Die();
        actor.Knockback(Vector2.up, 10f).Forget();

        AudioManager.PlaySound("trap");

        TriggerTrapFailed().Forget();
    }

    private async UniTaskVoid TriggerTrapFailed()
    {
        await UniTask.WaitForSeconds(0.5f);
        this.DispatchEvent(Witness<OnTriggerTrapEvent>._);
    }
}
