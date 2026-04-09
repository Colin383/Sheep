using Game.Scripts.Common;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Level7Ctrl : MonoBehaviour
{
    public MMF_Player camerShake;

    public Rigidbody2D boxRigibody;

    private bool canPlaySound = true;
    private bool isInGround = true;

    public void EnterGround()
    {
        if (isInGround || camerShake.IsPlaying)
            return;

        isInGround = true;
        camerShake.PlayFeedbacks();

        if (canPlaySound)
        {
            canPlaySound = false;
            AudioManager.PlaySound("crash");
        }

    }

    public void ExitLayer()
    {
        isInGround = false;
    }

    public void StartDrag()
    {
        boxRigibody.gravityScale = 0;
    }

    public void EndDrag()
    {
        boxRigibody.gravityScale = 2;

        if (boxRigibody.transform.position.y > -.1f)
        {
            canPlaySound = true;
        }
    }
}
