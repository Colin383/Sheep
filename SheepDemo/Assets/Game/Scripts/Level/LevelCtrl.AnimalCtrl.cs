using System.Collections.Generic;
using System.Linq;
using Bear.EventSystem;
using Game.Events;
using Game.Play;
using UnityEngine;

public partial class LevelCtrl
{
    [SerializeField] private ClickTriggerHandle clickHandle;

    private readonly List<Chick> chicks = new();
    private EventSubscriber _animalSubscriber;
    private SkillType _currentSkillType;
    private int _hintUsedCount;

    #region Click Trigger 

    private void OnEnable()
    {
        EventsUtils.ResetEvents(ref _animalSubscriber);
        _animalSubscriber.Subscribe<SwitchGameStateEvent>(OnSwitchState);
        _animalSubscriber.Subscribe<EnterSkillEvent>(OnEnterSkill);

        if (PlayCtrl.Instance != null && PlayCtrl.Instance.CheckState(GamePlayStateName.PLAYING))
            BindClickHandle();
    }

    private void OnDisable()
    {
        EventsUtils.ResetEvents(ref _animalSubscriber);
        UnbindClickHandle();
        UnbindSkillClickHandle();
    }

    private void OnSwitchState(SwitchGameStateEvent evt)
    {
        if (evt.NewState == GamePlayStateName.PLAYING)
        {
            UnbindSkillClickHandle();
            BindClickHandle();
        }
        else
        {
            UnbindClickHandle();
        }
    }

    private void OnEnterSkill(EnterSkillEvent evt)
    {
        _currentSkillType = evt.SkillType;
        _hintUsedCount = 0;

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
            return;

        clickHandle.OnClickTransform.RemoveListener(OnClickAnimal);
        clickHandle.OnClickTransform.AddListener(OnClickAnimal);
    }

    private void UnbindClickHandle()
    {
        if (clickHandle == null)
            return;

        clickHandle.OnClickTransform.RemoveListener(OnClickAnimal);
    }

    private void BindSkillClickHandle()
    {
        if (clickHandle == null)
            return;

        clickHandle.OnClickTransform.RemoveListener(OnSkillClickAnimal);
        clickHandle.OnClickTransform.AddListener(OnSkillClickAnimal);
    }

    private void UnbindSkillClickHandle()
    {
        if (clickHandle == null)
            return;

        clickHandle.OnClickTransform.RemoveListener(OnSkillClickAnimal);
    }

    private void ExitSkillMode()
    {
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PLAYING);
    }

    private void ExecuteRandomRotate5()
    {
        var candidates = spawned
            .Where(a => a != null && !(a is Chick))
            .ToList();

        int count = Mathf.Min(5, candidates.Count);
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
            animal.SetFacingDirection(animal.FacingDirection.TurnLeft().TurnLeft());
        }
    }

    private void OnSkillClickAnimal(Transform transform)
    {
        if (transform == null)
            return;

        var animal = transform.GetComponent<BaseAnimal>();
        if (animal == null)
            animal = transform.GetComponentInParent<BaseAnimal>();

        if (animal == null)
            return;

        switch (_currentSkillType)
        {
            case SkillType.Rotate:
                animal.SetFacingDirection(animal.FacingDirection.TurnLeft().TurnLeft());
                ExitSkillMode();
                break;

            case SkillType.Hint:
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
            animal.OnClickTrigger();

        for (int i = 0; i < chicks.Count; i++)
        {
            var chick = chicks[i];
            if (chick == null)
                continue;

            chick.TryMoving();
        }
    }

    #endregion
}
