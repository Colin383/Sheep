using Bear.Logger;
using UnityEngine;

namespace Game.Turntable
{
    public class SceneCommonButton : PressButtonBase, IDebuger
    {
        [SerializeField] private float waitingDuration = 0f;

        protected float Delay => waitingDuration;

        private bool isWaiting = false;
        private Collider2D _pendingCollider;
        private float _waitTimer;

        protected override void Update()
        {
            base.Update();

            if (isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                // this.Log("isWaiting" + _waitTimer);
                if (_waitTimer <= 0f)
                {
                    isWaiting = false;
                    var other = _pendingCollider;
                    _pendingCollider = null;
                    base.OnTriggerExit2D(other);
                }
            }
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            // this.Log("isWaiting" + isWaiting);
            // if (isWaiting)
            //     return;

            base.OnTriggerEnter2D(other);
        }

        protected override void OnTriggerExit2D(Collider2D other)
        {
            if (isWaiting)
                return;

            if (Delay <= 0f)
            {
                base.OnTriggerExit2D(other);
                return;
            }

            isWaiting = true;
            _pendingCollider = other;
            _waitTimer = Delay;
        }
    }
}
