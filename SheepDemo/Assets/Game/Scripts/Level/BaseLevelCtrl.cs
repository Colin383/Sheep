using Bear.EventSystem;
using Bear.Logger;
using Cysharp.Threading.Tasks;
using Game.Events;
using Game.Play;
using Game.Scripts.Common;
using UnityEngine;

namespace Game.Level
{

    /// <summary>
    /// 关卡控制类， 用于控制场景中的基本内容，可以用于拓展
    /// 需要绑定在 Level prefab 上
    /// </summary>
    public class BaseLevelCtrl : MonoBehaviour, IDebuger, IEventSender
    {
        // [SerializeField] private LayerMask SuccessLayer;
        [SerializeField] private LayerMask FailLayer;
        // Success
        // [SerializeField] private OnTrigger2DHandle onTrigger2DHandle;


        // [SerializeField] private SuccessAnimCtrl successAnim;

        /// <summary>
        /// 每个 Level 专门配置不同的 gamepanel
        /// </summary>
        [SerializeField] private string gamePlayPanelName = "GamePlayPanel_000";

        public string GamePlayPanelName => gamePlayPanelName;

        // 事件订阅器
        private EventSubscriber _subscriber;

        public bool IsActorEnterDoor { get; set; }

        private bool isFailed = false;
        protected bool isPause = false;
        protected bool isFinished = false;

        /// <summary>
        /// 用于控制 ActorCtrl 移动操作开关
        /// </summary>
        public bool ActorCtrlable { get; private set; }

        private void Awake()
        {
        }

        private void Start()
        {
            AddListener();
            GameResume();
        }

        /// <summary>
        /// 添加事件监听
        /// </summary>
        public virtual void AddListener()
        {
            EventsUtils.ResetEvents(ref _subscriber);

            _subscriber.Subscribe<GamePauseEvent>(OnGamePause);
            _subscriber.Subscribe<GameResumeEvent>(OnGameResume);
        }


        protected void OnGamePause(GamePauseEvent evt)
        {
            isPause = true;
        }
        protected void OnGameResume(GameResumeEvent evt)
        {
            GameResume();
        }

        private void GameResume()
        {
            isPause = false;
        }



        #region Success  or Failed


        // 当前关卡操作结束
        private void FinishedLevel()
        {
            isFinished = true;
        }
        // 23 关需求
        public void SuccessStrightly()
        {
            IsActorEnterDoor = true;
            CheckFinished();
        }

        // enter door trigger
        public void OnActorTrigger2D(Collider2D collider)
        {
            var layerName = LayerMask.LayerToName(collider.gameObject.layer);
            this.Log("trigger layer: " + layerName);
            switch (layerName)
            {
                // success
                case "Actor":
                    IsActorEnterDoor = true;
                    break;
                default:
                    this.Log("trigger layer: " + layerName);
                    break;
            }
        }

        private async UniTask WaitingFail()
        {   
            if (isFinished)
                return;

            isFinished = true;
            AudioManager.PlaySound("failed");

            // Actor.Die();
            await UniTask.WaitForSeconds(0.2f, cancellationToken: this.GetCancellationTokenOnDestroy());

            isFailed = true;
            CheckFinished();
        }

        // collision trap

        public virtual bool IsSuccess()
        {
            return IsActorEnterDoor;
        }

        public virtual bool IsFail()
        {
            return isFailed;
            // return Actor.CheckCollisionLayer(FailLayer);
        }

        public virtual void CheckFinished()
        {
            if (IsSuccess())
            {
                FinishedLevel();
                PlaySuccessAnim();

                return;
            }

            if (IsFail())
            {
                FinishedLevel();
                TryToRestart();

                return;
            }
        }

        private void PlaySuccessAnim()
        {/* 
            if (successAnim)
            {
                successAnim.Play(() =>
                {
                    this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
                });
            }
            else
            {
                this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
            } */
        }

        private async void TryToRestart()
        {
            await UniTask.WaitForSeconds(0.5f, cancellationToken: this.GetCancellationTokenOnDestroy());
            this.DispatchEvent(Witness<GameResetEvent>._, GameResetType.Failed);
        }

        #endregion

        /// <summary>
        /// 销毁关卡时清理
        /// </summary>
        public virtual void DestroyLevel()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            EventsUtils.ResetEvents(ref _subscriber);
        }
    }
}
