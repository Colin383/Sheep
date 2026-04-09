using UnityEngine;

public class ClickScaleAnim : ButtonClickTrigger
{
    [SerializeField]
    private float Scale = 0.8f;
    [Tooltip("插值开始前额外等待（缩放不变）；真正缩放时长见 Duration")]
    [SerializeField]
    private float Delay = 0f;
    [Tooltip("从当前缩放到目标缩放的插值时长（不含 Delay）")]
    private float Duration = 0.1f;

    [SerializeField]
    private bool ignoreScaleTime = true;

    private Vector3 _targetScale = Vector3.one;
    private Vector3 _startScale = Vector3.one;
    private float _timer = 0f;
    private bool _isAnimating = false;
    private float _currentDelay = 0f;
    /// <summary> true = 正在缩放到按下态，需播完；false = 回弹，可被新的按下打断 </summary>
    private bool _isPressPhase = false;
    /// <summary> 按下动画未结束时已收到抬起，在按下结束后再开始抬起动画 </summary>
    private bool _releasePending = false;

    public override void OnButtonDown(bool hasAnim)
    {
        _releasePending = false;
        BeginPressAnim();
    }

    public override void OnButtonUp(bool hasAnim)
    {
        if (_isAnimating && _isPressPhase)
        {
            _releasePending = true;
            return;
        }

        BeginReleaseAnim();
    }

    private void BeginPressAnim()
    {
        _startScale = transform.localScale;
        _targetScale = Vector3.one * Scale;
        _timer = 0f;
        _currentDelay = Delay;
        _isPressPhase = true;
        _isAnimating = true;
    }

    private void BeginReleaseAnim()
    {
        _releasePending = false;
        _startScale = transform.localScale;
        _targetScale = Vector3.one;
        _timer = 0f;
        _currentDelay = Delay;
        _isPressPhase = false;
        _isAnimating = true;
    }

    private void LateUpdate()
    {
        if (!_isAnimating) return;

        float deltaTime = ignoreScaleTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 延迟耗尽时，本帧剩余 delta 要并进插值，否则会多白等一整帧、总时长偏长
        if (_currentDelay > 0f)
        {
            _currentDelay -= deltaTime;
            if (_currentDelay > 0f)
                return;
            deltaTime = -_currentDelay;
            _currentDelay = 0f;
        }

        if (Duration <= 0f)
        {
            transform.localScale = _targetScale;
            OnAnimSegmentComplete();
            return;
        }

        _timer += deltaTime;
        float t = Mathf.Clamp01(_timer / Duration);

        transform.localScale = Vector3.LerpUnclamped(_startScale, _targetScale, t);

        if (t >= 1f)
        {
            transform.localScale = _targetScale;
            OnAnimSegmentComplete();
        }
    }

    private void OnAnimSegmentComplete()
    {
        if (_isPressPhase && _releasePending)
        {
            _releasePending = false;
            BeginReleaseAnim();
            return;
        }

        _isAnimating = false;
        _isPressPhase = false;
    }
}
