using Bear.EventSystem;
using Bear.Logger;
using Cysharp.Threading.Tasks;
using Game.Events;
using Game.Play;
using Game.Scripts.Common;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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

        [SerializeField] private ActorCtrl actor;

        [SerializeField] private SuccessAnimCtrl successAnim;

        public ActorCtrl Actor => actor;

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
            // 确保 Actor 已赋值
            if (actor == null)
            {
                actor = FindFirstObjectByType<ActorCtrl>();
            }

            ActorCtrlable = true;
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
            _subscriber.Subscribe<PlayerRightMoveEvent>(OnPlayerRightMove);
            _subscriber.Subscribe<PlayerLeftMoveEvent>(OnPlayerLeftMove);
            _subscriber.Subscribe<PlayerMoveCancelEvent>(OnPlayerMoveCancel);
            _subscriber.Subscribe<PlayerJumpEvent>(OnPlayerJump);

            _subscriber.Subscribe<GamePauseEvent>(OnGamePause);
            _subscriber.Subscribe<GameResumeEvent>(OnGameResume);

            _subscriber.Subscribe<OnTriggerTrapEvent>(OnTriggerTrap);
            _subscriber.Subscribe<OnTriggerFailAreaEvent>(OnTriggerFailArea);
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

        #region Movement

        /// <summary>
        /// 开关，用于管理用户输入输出是否生效
        /// </summary>
        /// <param name="isOpen"></param>
        public void SetCtrlable(bool isOpen)
        {
            ActorCtrlable = isOpen;
        }

        /// <summary>
        /// 玩家向右移动事件处理
        /// </summary>
        private void OnPlayerRightMove(PlayerRightMoveEvent evt)
        {
            if (!ActorCtrlable)
                return;
            if (actor != null)
            {
                actor.SetMoveInput(1f);
                // this.Log("Right Moving ----------------- ");
            }
        }

        /// <summary>
        /// 玩家向左移动事件处理
        /// </summary>
        private void OnPlayerLeftMove(PlayerLeftMoveEvent evt)
        {
            if (!ActorCtrlable)
                return;
            if (actor != null)
            {
                actor.SetMoveInput(-1f);
                // this.Log("Left Moving ----------------- ");
            }
        }

        /// <summary>
        /// 取消移动事件处理
        /// </summary>
        private void OnPlayerMoveCancel(PlayerMoveCancelEvent evt)
        {
            if (!ActorCtrlable)
                return;

            StopMove();
        }

        private void StopMove()
        {
            if (actor != null)
            {
                actor.SetMoveInput(0f);
                actor.UpdateAnimation();

                // this.Log("------------ Level StopMove");
            }
        }

        /// <summary>
        /// 玩家跳跃事件处理
        /// </summary>
        private void OnPlayerJump(PlayerJumpEvent evt)
        {
            if (actor != null)
            {
                actor.TriggerJump();
            }
        }

        #endregion


        void Update()
        {
            if (isFinished || isPause)
                return;

            CheckFinished();
            Actor.OnUpdate();
        }

        #region Success  or Failed


        // 当前关卡操作结束
        private void FinishedLevel()
        {
            StopMove();
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

        // 玩家触发陷阱
        private void OnTriggerTrap(OnTriggerTrapEvent evt)
        {
            WaitPlayDie(1.5f).Forget();
        }


        private void OnTriggerFailArea(OnTriggerFailAreaEvent evt)
        {
            WaitingFail().Forget();
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

        private async UniTask WaitPlayDie(float waitingTime)
        {
            isFinished = true;

            Actor.Die();
            if (waitingTime > 0)
                await UniTask.WaitForSeconds(waitingTime, cancellationToken: this.GetCancellationTokenOnDestroy());

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

            if (IsFail() || Actor.IsDied)
            {
                FinishedLevel();
                TryToRestart();

                return;
            }
        }

        private void PlaySuccessAnim()
        {
            if (successAnim)
            {
                actor.StopMoving();
                successAnim.Play(() =>
                {
                    this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
                });
            }
            else
            {
                this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
            }
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
            if (actor != null)
            {
                actor.SetMoveInput(0f);
            }
        }

#if UNITY_EDITOR
        // 用于自动化绑定一些内容，懒狗必备
        [Button("Auto assigned level")]
        private void AutoAssigned()
        {
            if (actor == null)
            {
                actor = transform.Find("Actor")?.GetComponent<ActorCtrl>();
                actor.GetComponent<SortingGroup>().sortingOrder = 200;
            }

            successAnim = transform.Find("door_0")?.GetComponent<SuccessAnimCtrl>();
            if (successAnim)
            {
                var animHandle = successAnim.transform.GetComponent<ActorDestroyAnimHandle>();
                animHandle.SetTarget(actor.transform);
            }

            // 资源遗留问题
            var mask = transform.Find("Mask");
            if (mask != null)
            {
                DestroyImmediate(mask.gameObject);
            }

            var bg = transform.Find("CommonBg");
            if (bg == null)
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Level/Background/CommonBg.prefab");
                Debug.Log("Auto assigned --------" + obj);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Level/Background/CommonBg.prefab"), transform);
            }
        }

#endif
    }
}
