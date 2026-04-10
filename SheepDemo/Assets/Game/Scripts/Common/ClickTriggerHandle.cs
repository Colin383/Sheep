using System;
using Game.Scripts.Common;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TransformClickEvent : UnityEvent<Transform> { }

/// <summary>
/// Handles scene-object click detection by raycasting from pointer position.
/// Supports mouse (Editor/Standalone/WebGL), touch (mobile), and manual code trigger.
/// </summary>
public class ClickTriggerHandle : MonoBehaviour
{
    [Header("Click Settings")]
    [SerializeField] private bool enableClick = true;

    [Tooltip("If empty, uses Camera.main and then FindFirstObjectByType.")]
    [SerializeField] private Camera targetCamera;

    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float raycastDistance = Mathf.Infinity;

    [Tooltip("If enabled, hit child transforms are treated as clicking this object.")]
    [SerializeField] private bool includeChildren = true;

    [Tooltip("When enabled, clicks over UI are ignored.")]
    [SerializeField] private bool ignorePointerOverUI = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onClick = new UnityEvent();
    [SerializeField] private TransformClickEvent onClickTransform = new TransformClickEvent();

    public UnityEvent OnClick => onClick;
    public TransformClickEvent OnClickTransform => onClickTransform;

    public event Action<Transform> ClickHandlers;

    public bool IsClickEnabled => enableClick;
    public Transform LastClickedTransform { get; private set; }

    private Camera _camera;

    private void Awake()
    {
        ResolveCamera();
    }

    private void Update()
    {
        if (!enableClick)
            return;

        if (!TryGetPointerDownScreenPosition(out var screenPosition))
            return;

        if (ignorePointerOverUI && InputUtils.IsPointerOverUI(screenPosition))
            return;

        // Reuse the same raycast pipeline as manual trigger by screen position.
        TryTriggerByScreenPosition(screenPosition);
    }

    public void SetEnableClick(bool enable)
    {
        enableClick = enable;
    }

    public bool TryTriggerByScreenPosition(Vector2 screenPosition)
    {
        if (!ResolveCamera())
            return false;

        // Find first hit in world (3D first, then 2D).
        if (!TryRaycast(screenPosition, out var hitTransform))
            return false;

        if (!IsTargetMatch(hitTransform))
            return false;

        Trigger(hitTransform);
        return true;
    }

    [ContextMenu("Trigger Click")]
    public void Trigger()
    {
        Trigger(transform);
    }

    public void Trigger(Transform clickedTransform)
    {
        var target = clickedTransform != null ? clickedTransform : transform;

        // Keep last clicked target for debugging or follow-up logic.
        LastClickedTransform = target;
        onClick?.Invoke();
        onClickTransform?.Invoke(target);
        ClickHandlers?.Invoke(target);
    }

    private bool TryRaycast(Vector2 screenPosition, out Transform hitTransform)
    {
        hitTransform = null;

        var ray = _camera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out var hit3D, raycastDistance, raycastMask, QueryTriggerInteraction.Collide))
        {
            hitTransform = hit3D.transform;
            return hitTransform != null;
        }

        var hit2D = Physics2D.GetRayIntersection(ray, raycastDistance, raycastMask);
        if (hit2D.collider != null)
        {
            hitTransform = hit2D.transform;
            return hitTransform != null;
        }

        return false;
    }

    private bool IsTargetMatch(Transform hitTransform)
    {
        if (hitTransform == null)
            return false;

        if (hitTransform == transform)
            return true;

        return includeChildren && hitTransform.IsChildOf(transform);
    }

    private bool ResolveCamera()
    {
        if (_camera != null)
            return true;

        // Camera resolve order: explicit -> main -> first available.
        _camera = targetCamera;
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            _camera = FindFirstObjectByType<Camera>();

        return _camera != null;
    }

    private static bool TryGetPointerDownScreenPosition(out Vector2 screenPosition)
    {

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;


#elif UNITY_ANDROID || UNITY_IOS
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began)
                continue;

            screenPosition = touch.position;
            return true;
        }

        screenPosition = default;
        return false;
#endif
    }

    private void OnDestroy()
    {
        onClick?.RemoveAllListeners();
        onClickTransform?.RemoveAllListeners();
        ClickHandlers = null;
    }
}
