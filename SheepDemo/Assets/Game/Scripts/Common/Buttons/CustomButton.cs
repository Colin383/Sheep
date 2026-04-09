using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public delegate void CustomButtonClick(CustomButton btn);

public sealed class CustomButton : MonoBehaviour,
IPointerClickHandler,
IPointerDownHandler,
IPointerUpHandler,
IPointerEnterHandler,
IPointerExitHandler,
ICancelHandler
{
    // 是否可以交互
    [SerializeField] private bool _interactable = true;
    [SerializeField] private bool _hasAim = true;
    [SerializeField] private bool _hasDelay = true;

    [SerializeField] private bool isListenEnter = false;

    [SerializeField] private bool isListenExit = false;

    // [ShowIf("_hasDelay")]
    public float DelayTime = 1f;

    private bool isTrigger = false;

    private float _waitingTime = 0;

    private Graphic _graphic;
    private Collider2D _collider2D;
    // Interactable switch handle
    private delegate void WhenInteractableSwitch(bool isOn);
    private event WhenInteractableSwitch InteractableHandle;

    private Action<bool> OnButtonDownPerformer;
    private Action<bool> OnButtonUpPerformer;

    // 临时用于 Button 不要触发 Click 事件     
    public bool IsBlockClick { get; set; }

    public Graphic BGraphic => _graphic;

    public bool Interactable
    {
        get => _interactable;
        set
        {
            _interactable = value;
            if (_collider2D != null)
                _collider2D.enabled = value;

            if (_graphic != null)
                _graphic.raycastTarget = value;

            InteractableHandle?.Invoke(value);
        }
    }

    public event CustomButtonClick OnClick;

    public event CustomButtonClick OnClickDown;
    public event CustomButtonClick OnClickUp;
    public event CustomButtonClick OnClickEnter;
    public event CustomButtonClick OnClickExit;



    void Awake()
    {
        _collider2D = GetComponent<Collider2D>();
        _graphic = GetComponent<Graphic>();

        var interactableComps = GetComponentsInChildren<ButtonInteractableBase>();
        foreach (var comp in interactableComps)
            InteractableHandle += comp.OnSwitchInteractable;

        var clickTriggerComps = GetComponentsInChildren<ButtonClickTrigger>();
        foreach (var comp in clickTriggerComps)
        {
            OnButtonDownPerformer += comp.OnButtonDown;
            OnButtonUpPerformer += comp.OnButtonUp;
        }
    }

    void OnDisable()
    {
        isTrigger = false;
    }

    void Update()
    {
        if (!isTrigger)
            return;

        _waitingTime += Time.unscaledDeltaTime;

        if (_waitingTime >= DelayTime)
        {
            _waitingTime = 0;
            isTrigger = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTrigger || IsBlockClick)
            return;

        isTrigger = true;
        OnClick?.Invoke(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnButtonDownPerformer?.Invoke(_hasAim);

        OnClickDown?.Invoke(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnButtonUpPerformer?.Invoke(_hasAim);

        OnClickUp?.Invoke(this);
    }

    public void OnCancel(BaseEventData eventData)
    {
        this.OnPointerUp((PointerEventData)eventData);
    }

    private void OnDestroy()
    {
        InteractableHandle = null;
        OnButtonDownPerformer = null;
        OnButtonUpPerformer = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_interactable || !isListenEnter)
            return;
        OnButtonDownPerformer?.Invoke(_hasAim);
        OnClickEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_interactable || !isListenExit)
            return;

        OnButtonUpPerformer?.Invoke(_hasAim);
        OnClickExit?.Invoke(this);
    }
}
