using UnityEngine;
using UnityEngine.XR;

public class ViveThumbstickResponseInput : MonoBehaviour
{
    public SimpleExperimentVR experiment;

    [Header("Controller")]
    public XRNode controllerNode = XRNode.RightHand;

    [Header("Swipe settings")]
    [Tooltip("Minimum swipe distance on the 2D axis (-1..1) to count as a gesture.")]
    public float minSwipeDistance = 0.40f;

    [Tooltip("Maximum time allowed for a swipe gesture (seconds). Longer gestures are ignored.")]
    public float maxSwipeDurationSeconds = 1.0f;

    [Tooltip("If true, only the dominant axis decides (horizontal vs vertical). Recommended.")]
    public bool requireDominantAxis = true;

    [Tooltip("If true, downward swipes are ignored (only Up counts for Both).")]
    public bool ignoreDownSwipes = true;

    [Header("Touch detection fallback")]
    [Tooltip("If primary2DAxisTouch isn't available, treat primary2DAxisClick as 'touch'.")]
    public bool useClickAsTouchFallback = true;

    [Header("Debug")]
    public bool debugLogs = false;

    private bool _prevTouch = false;
    private Vector2 _touchStartAxis;
    private float _touchStartTime;   // uses experiment audio time for consistency
    private Vector2 _lastAxis;

    void Start()
    {
        if (experiment == null)
            Debug.LogError("ViveThumbstickResponseInput: experiment reference is missing.");
    }

    void Update()
    {
        if (experiment == null) return;

        // Only accept input during response window
        if (!experiment.IsRunning || !experiment.IsAwaitingResponse)
        {
            ResetGestureState();
            return;
        }

        InputDevice dev = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (!dev.isValid) return;

        // Read axis
        if (!dev.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
            return;

        _lastAxis = axis;

        // Read touch
        bool touch;
        bool touchOk = dev.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out touch);

        if (!touchOk && useClickAsTouchFallback)
        {
            // Some runtimes do not expose touch; use click as fallback gesture gate
            bool click;
            if (dev.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out click))
            {
                touch = click;
                touchOk = true;
            }
        }

        if (!touchOk)
        {
            // Cannot detect touch/click state on this device profile
            // No reliable swipe start/end events available.
            return;
        }

        // Touch began
        if (touch && !_prevTouch)
        {
            _touchStartAxis = axis;
            _touchStartTime = experiment.AudioTime;

            if (debugLogs)
                Debug.Log($"[Swipe] TouchStart axis={axis} t={_touchStartTime:F3}");
        }

        // Touch ended
        if (!touch && _prevTouch)
        {
            float endTime = experiment.AudioTime;
            float duration = endTime - _touchStartTime;

            Vector2 endAxis = _lastAxis;
            Vector2 delta = endAxis - _touchStartAxis;

            if (debugLogs)
                Debug.Log($"[Swipe] TouchEnd axis={endAxis} t={endTime:F3} delta={delta} dt={duration:F3}");

            if (duration <= maxSwipeDurationSeconds)
            {
                var dir = ClassifySwipe(delta);
                if (dir != SimpleExperimentVR.AlertDirection.Unknown)
                {
                    experiment.SubmitResponse(dir);

                    if (debugLogs)
                        Debug.Log($"[Swipe] Submitted {dir}");
                }
            }
        }

        _prevTouch = touch;
    }

    private void ResetGestureState()
    {
        _prevTouch = false;
        _touchStartAxis = Vector2.zero;
        _touchStartTime = 0f;
        _lastAxis = Vector2.zero;
    }

    private SimpleExperimentVR.AlertDirection ClassifySwipe(Vector2 delta)
    {
        float dx = delta.x;
        float dy = delta.y;

        // Too small
        if (Mathf.Abs(dx) < minSwipeDistance && Mathf.Abs(dy) < minSwipeDistance)
            return SimpleExperimentVR.AlertDirection.Unknown;

        if (requireDominantAxis)
        {
            // Horizontal dominates
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                if (dx >= minSwipeDistance) return SimpleExperimentVR.AlertDirection.Right;
                if (dx <= -minSwipeDistance) return SimpleExperimentVR.AlertDirection.Left;
                return SimpleExperimentVR.AlertDirection.Unknown;
            }

            // Vertical dominates
            if (dy >= minSwipeDistance) return SimpleExperimentVR.AlertDirection.Both;
            if (!ignoreDownSwipes && dy <= -minSwipeDistance) return SimpleExperimentVR.AlertDirection.Unknown;
            return SimpleExperimentVR.AlertDirection.Unknown;
        }
        else
        {
            // Not requiring dominance: allow any strong enough component
            if (dy >= minSwipeDistance) return SimpleExperimentVR.AlertDirection.Both;
            if (dx >= minSwipeDistance) return SimpleExperimentVR.AlertDirection.Right;
            if (dx <= -minSwipeDistance) return SimpleExperimentVR.AlertDirection.Left;
            return SimpleExperimentVR.AlertDirection.Unknown;
        }
    }
}
