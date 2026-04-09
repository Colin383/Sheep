using Game.Scripts.Common;
using UnityEngine;

public class Level27Ctrl : MonoBehaviour
{
    private Transform currentMask;
    [SerializeField] private BoxCollider2D enemyCollider;

    [SerializeField] private Transform startDoor;

    public void SwitchMask(Transform target)
    {
        if (currentMask != null && currentMask == target)
            return;

        if (currentMask)
        {
            currentMask.GetComponent<FollowTarget>().enabled = false;
            currentMask.GetComponent<FollowTargetRotate>().enabled = false;
            currentMask.GetComponent<FollowTargetScale>().enabled = false;
            currentMask.GetComponent<ItemJumpOutExecutor>().Play();
            currentMask.GetComponent<SpriteRenderer>().sortingOrder = 101;
        }

        currentMask = target;
        currentMask.GetComponent<FollowTarget>().enabled = true;
        currentMask.GetComponent<FollowTargetRotate>().enabled = true;
        currentMask.GetComponent<FollowTargetScale>().enabled = true;
        currentMask.GetComponent<SpriteRenderer>().sortingOrder = 201;

        enemyCollider.enabled = currentMask != startDoor;

        AudioManager.PlaySound("button");
    }
}
