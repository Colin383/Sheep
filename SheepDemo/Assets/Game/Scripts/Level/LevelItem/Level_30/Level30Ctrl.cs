using DG.Tweening;
using UnityEngine;

public class Level30Ctrl : MonoBehaviour
{
    [SerializeField] Transform key1;

    [SerializeField] Transform key2;

    [SerializeField] PingPongMoveHandle move1;

    [SerializeField] PingPongMoveHandle move2;

    [SerializeField] Collider2D doorCollider;

    [SerializeField] Collider2D lockCollider;

    private Transform currentWing;

    private FollowTarget lockFollower;
    private bool hasWingBindLock = false;

    public void OnWingStartDrag(Transform target)
    {
        var follower = key1.GetComponent<FollowTarget>();
        if (follower.Target == target)
        {
            currentWing = target;
            follower.enabled = false;

            move1.StopMove();
            key1.DOMoveY(-1.8f, .8f).SetEase(Ease.InQuart);
            // key1.GetComponent<BoxCollider2D>().enabled = true;
        }

        follower = key2.GetComponent<FollowTarget>();
        if (follower.Target == target)
        {
            currentWing = target;
            follower.enabled = false;
            move2.StopMove();
            key2.DOMoveY(-1.8f, .8f).SetEase(Ease.InQuart);
            // key2.GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    public void OnWingEndDrag(Transform target)
    {
        if (target == null || lockCollider == null)
        {
            target.DOMoveY(10, 1f).OnComplete(() =>
            {
                Destroy(target.gameObject);
            });
            return;
        }

        Collider2D targetCollider = target.GetComponent<Collider2D>();
        if (targetCollider == null)
            targetCollider = target.GetComponentInChildren<Collider2D>();

        bool isIntersecting = false;
        if (targetCollider != null)
        {
            isIntersecting = targetCollider.bounds.Intersects(lockCollider.bounds);
        }

        if (isIntersecting)
        {
            BindLockToWing(lockCollider);
        }

        target.DOMoveY(10, 1f).OnComplete(() =>
        {
            Destroy(target.gameObject);
        });
    }

    public void BindLockToWing(Collider2D collider)
    {
        if (currentWing == null || hasWingBindLock)
            return;

        hasWingBindLock = true;
        lockFollower = collider.transform.GetComponent<FollowTarget>();
        lockFollower.enabled = true;
        lockFollower.SetTarget(currentWing);

        doorCollider.enabled = true;

        key1.GetComponent<BoxCollider2D>().enabled = false;
        key2.GetComponent<BoxCollider2D>().enabled = false;
    }
}
