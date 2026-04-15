using System.Collections.Generic;
using System.Linq;
using Bear.EventSystem;
using Bear.Logger;
using BPGame;
using Game.Common;
using Game.Events;
using Game.Play;
using UnityEngine;

public partial class LevelCtrl : IDebuger
{
    [SerializeField] private ClickTriggerHandle clickHandle;

    // 被撞击
    [SerializeField] private ParticleSystem Stun;
    
    // 爆炸
    [SerializeField] private ParticleSystem Explosion;

    // 跑步
    
    [SerializeField] private ParticleSystem Smoke;

    private readonly List<Chick> chicks = new();
    private readonly List<CdSheepAnimal> cdSheeps = new();
    private EventSubscriber _animalSubscriber;
    private SkillType _currentSkillType;
    private int _hintUsedCount;
    private bool _cdStarted;

    private bool _stunPoolRegistered;
    private bool _smokePoolRegistered;

    #region Click Trigger 

    private void OnEnable()
    {
        EventsUtils.ResetEvents(ref _animalSubscriber);
        _animalSubscriber.Subscribe<EnterPlayingEvent>(OnEnterPlaying);
        _animalSubscriber.Subscribe<ExitPlayingEvent>(OnExitPlaying);
        _animalSubscriber.Subscribe<EnterSkillEvent>(OnEnterSkill);
    }

    private void OnDisable()
    {
        EventsUtils.ResetEvents(ref _animalSubscriber);
        UnbindClickHandle();
        UnbindSkillClickHandle();
    }

    private void OnEnterPlaying(EnterPlayingEvent evt)
    {
        this.Log("[AnimalCtrl] OnEnterPlaying");
        UnbindSkillClickHandle();
        BindClickHandle();
    }

    private void OnExitPlaying(ExitPlayingEvent evt)
    {
        this.Log("[AnimalCtrl] OnExitPlaying");
        UnbindClickHandle();
    }

    private void OnEnterSkill(EnterSkillEvent evt)
    {
        _currentSkillType = evt.SkillType;
        _hintUsedCount = 0;

        this.Log($"[AnimalCtrl] EnterSkill: {_currentSkillType}");
        UnbindClickHandle();

        switch (_currentSkillType)
        {
            case SkillType.RandomRotate5:
                ExecuteRandomRotate5();
                ExitSkillMode();
                break;
            case SkillType.Rotate:
            case SkillType.Hint:
                BindSkillClickHandle();
                break;
        }
    }

    private void BindClickHandle()
    {
        if (clickHandle == null)
        {
            this.LogWarning("[AnimalCtrl] BindClickHandle failed: clickHandle is null");
            return;
        }

        clickHandle.OnClickTransform.RemoveListener(OnClickAnimal);
        clickHandle.OnClickTransform.AddListener(OnClickAnimal);
        this.Log("[AnimalCtrl] BindClickHandle: normal click bound");
    }

    private void UnbindClickHandle()
    {
        if (clickHandle == null)
            return;

        clickHandle.OnClickTransform.RemoveListener(OnClickAnimal);
        this.Log("[AnimalCtrl] UnbindClickHandle: normal click unbound");
    }

    private void BindSkillClickHandle()
    {
        if (clickHandle == null)
        {
            this.LogWarning("[AnimalCtrl] BindSkillClickHandle failed: clickHandle is null");
            return;
        }

        clickHandle.OnClickTransform.RemoveListener(OnSkillClickAnimal);
        clickHandle.OnClickTransform.AddListener(OnSkillClickAnimal);
        this.Log($"[AnimalCtrl] BindSkillClickHandle: skill click bound ({_currentSkillType})");
    }

    private void UnbindSkillClickHandle()
    {
        if (clickHandle == null)
            return;

        clickHandle.OnClickTransform.RemoveListener(OnSkillClickAnimal);
        this.Log("[AnimalCtrl] UnbindSkillClickHandle: skill click unbound");
    }

    private void ExitSkillMode()
    {
        this.Log("[AnimalCtrl] ExitSkillMode -> PLAYING");
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PLAYING);
        // this.DispatchEvent(Witness<ExitSkillEvent>._);
    }

    private void ExecuteRandomRotate5()
    {
        var candidates = spawned
            .Where(a => a != null && !(a is Chick))
            .ToList();

        int count = Mathf.Min(5, candidates.Count);
        this.Log($"[AnimalCtrl] ExecuteRandomRotate5: candidates={candidates.Count}, rotate={count}");
        if (count <= 0)
            return;

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        for (int i = 0; i < count; i++)
        {
            var animal = candidates[i];
            RotateAnimal(animal);
            this.Log($"[AnimalCtrl] RandomRotate5 rotated animal id={animal.Id}, type={animal.Type}");
        }
    }

    /// <summary>
    /// 将 animal 180° 转向并同步移动 pivot，使 footprint 几何中心保持不变。
    /// </summary>
    private void RotateAnimal(BaseAnimal animal)
    {
        if (animal == null)
            return;

        var oldFacing = animal.FacingDirection;
        var newFacing = oldFacing.TurnLeft().TurnLeft();

        var oldCenter = CalculateFootprintCenterOffset(animal, oldFacing);
        var newCenter = CalculateFootprintCenterOffset(animal, newFacing);
        var delta = oldCenter - newCenter;

        if (delta.sqrMagnitude > 1e-6f)
        {
            int newRow = animal.CurrentPos.y + Mathf.RoundToInt(delta.y);
            int newCol = animal.CurrentPos.x + Mathf.RoundToInt(delta.x);
            animal.SetCurrentGridPos(newRow, newCol);

            if (TryGetConfigDimensions(out var gridW, out var gridH))
            {
                animal.transform.position = GridToWorld(newRow, newCol, gridW, gridH);
            }
            else
            {
                this.LogWarning($"[AnimalCtrl] RotateAnimal: failed to get grid dimensions for id={animal.Id}");
            }
        }

        animal.SetFacingDirection(newFacing);
    }

    private Vector2 CalculateFootprintCenterOffset(BaseAnimal animal, DirectionEnum facing)
    {
        var occupied = new List<Vector2Int> { Vector2Int.zero };
        var extras = animal.FootprintSizeCells;
        if (extras != null)
        {
            for (int i = 0; i < extras.Count; i++)
            {
                var o = extras[i];
                if (o == Vector2Int.zero)
                    continue;
                occupied.Add(RotateOffset(o, facing));
            }
        }

        int minX = occupied[0].x;
        int maxX = occupied[0].x;
        int minY = occupied[0].y;
        int maxY = occupied[0].y;

        for (int i = 1; i < occupied.Count; i++)
        {
            var o = occupied[i];
            minX = Mathf.Min(minX, o.x);
            maxX = Mathf.Max(maxX, o.x);
            minY = Mathf.Min(minY, o.y);
            maxY = Mathf.Max(maxY, o.y);
        }

        return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    public void PlayStunEffect(Transform parent)
    {
        if (Stun == null || parent == null) return;
        EnsureStunPool();
        var instance = ObjectPoolManager.Instance.Get<StunParticle>();
        if (instance == null) return;
        instance.transform.position = parent.position;
        instance.transform.rotation = parent.rotation;
    }

    public SmokeParticle PlaySmokeEffect(Transform parent)
    {
        if (Smoke == null || parent == null) return null;
        EnsureSmokePool();
        var instance = ObjectPoolManager.Instance.Get<SmokeParticle>();
        if (instance == null) return null;
        instance.transform.parent = parent;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.rotation = parent.rotation;
        return instance;
    }

    public void PlayExplosionEffect(Transform parent)
    {
        if (Explosion == null || parent == null) return;
        var instance = Instantiate(Explosion, instancesRoot);
        instance.transform.position = parent.position;
        instance.Play();
        // Destroy(instance.gameObject, instance.main.duration);
    }

    private void EnsureStunPool()
    {
        if (_stunPoolRegistered) return;
        _stunPoolRegistered = true;
        RegisterParticlePool<StunParticle>(Stun);
    }

    private void EnsureSmokePool()
    {
        if (_smokePoolRegistered) return;
        _smokePoolRegistered = true;
        RegisterParticlePool<SmokeParticle>(Smoke);
    }

    private void RegisterParticlePool<T>(ParticleSystem prefab) where T : ParticleRecycle
    {
        if (prefab == null) return;
        var go = prefab.gameObject;
        if (go.GetComponent<T>() == null)
            go.AddComponent<T>();

        if (!ObjectPoolManager.Instance.IsPoolRegistered<T>())
            ObjectPoolManager.Instance.RegisterPool<T>(() => Instantiate(go).GetComponent<T>(), 2, 0);
    }

    private void OnSkillClickAnimal(Transform transform)
    {
        if (transform == null)
            return;

        var animal = transform.GetComponent<BaseAnimal>();
        if (animal == null)
            animal = transform.GetComponentInParent<BaseAnimal>();

        if (animal == null)
        {
            this.LogWarning("[AnimalCtrl] OnSkillClickAnimal: no BaseAnimal found");
            return;
        }

        switch (_currentSkillType)
        {
            case SkillType.Rotate:
                this.Log($"[AnimalCtrl] Skill Rotate: rotate animal id={animal.Id}, type={animal.Type}");
                RotateAnimal(animal);
                ExitSkillMode();
                break;

            case SkillType.Hint:
                this.Log($"[AnimalCtrl] Skill Hint: destroy animal id={animal.Id}, type={animal.Type}, used={_hintUsedCount + 1}/2");
                DestroyAnimal(animal);
                _hintUsedCount++;
                if (_hintUsedCount >= 2)
                    ExitSkillMode();
                break;
        }
    }

    /// <summary>
    /// Click Trigger 触发时，获取 transform BaseAnimal，触发 OnClickTrigger 事件
    /// </summary>
    /// <param name="transform"></param>
    public void OnClickAnimal(Transform transform)
    {
        if (transform == null)
            return;

        var animal = transform.GetComponent<BaseAnimal>();
        if (animal == null)
            animal = transform.GetComponentInParent<BaseAnimal>();

        if (animal != null)
        {
            this.Log($"[AnimalCtrl] OnClickAnimal: id={animal.Id}, type={animal.Type}");
            animal.Bark();
            animal.OnClickTrigger();

            if (!_cdStarted)
            {
                _cdStarted = true;
                for (int i = 0; i < cdSheeps.Count; i++)
                {
                    cdSheeps[i]?.StartCD();
                }
            }
        }

        for (int i = 0; i < chicks.Count; i++)
        {
            var chick = chicks[i];
            if (chick == null)
                continue;

            chick.TryMoving();
        }

        VibrationManager.Instance.Vibrate();
    }

    #endregion
}
