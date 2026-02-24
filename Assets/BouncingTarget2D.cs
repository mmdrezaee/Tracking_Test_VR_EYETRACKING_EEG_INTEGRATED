using UnityEngine;

public class BouncingTarget2D : MonoBehaviour
{
    [Header("References")]
    public RectTransform trackingArea;
    public RectTransform targetRect;

    [Header("Target shape")]
    public float targetRadiusPx = 15f;

    [Header("Motion")]
    public float speedPxPerSec = 250f;

    [Tooltip("Minimum incidence angle away from wall tangent, in degrees. 18 degrees matches your spec.")]
    public float minAngleDeg = 18f;

    [Tooltip("Small random direction change applied after each bounce to keep trajectories pseudo-random.")]
    public float bounceJitterDeg = 10f;

    private Vector2 _vel;

    void Reset()
    {
        targetRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (targetRect == null) targetRect = GetComponent<RectTransform>();
        InitRandomVelocity();
    }

    void Update()
    {
        if (trackingArea == null || targetRect == null) return;

        Rect r = trackingArea.rect;
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;

        Vector2 pos = targetRect.anchoredPosition;
        Vector2 next = pos + _vel * Time.deltaTime;

        float minX = -halfW + targetRadiusPx;
        float maxX =  halfW - targetRadiusPx;
        float minY = -halfH + targetRadiusPx;
        float maxY =  halfH - targetRadiusPx;

        bool bounced = false;

        // Left/right boundaries
        if (next.x < minX)
        {
            next.x = minX + (minX - next.x); // reflect position
            _vel.x = Mathf.Abs(_vel.x);      // reflect velocity
            EnforceMinAngle(new Vector2(1f, 0f));
            bounced = true;
        }
        else if (next.x > maxX)
        {
            next.x = maxX - (next.x - maxX);
            _vel.x = -Mathf.Abs(_vel.x);
            EnforceMinAngle(new Vector2(-1f, 0f));
            bounced = true;
        }

        // Bottom/top boundaries
        if (next.y < minY)
        {
            next.y = minY + (minY - next.y);
            _vel.y = Mathf.Abs(_vel.y);
            EnforceMinAngle(new Vector2(0f, 1f));
            bounced = true;
        }
        else if (next.y > maxY)
        {
            next.y = maxY - (next.y - maxY);
            _vel.y = -Mathf.Abs(_vel.y);
            EnforceMinAngle(new Vector2(0f, -1f));
            bounced = true;
        }

        if (bounced && bounceJitterDeg > 0.01f)
        {
            float jitter = Random.Range(-bounceJitterDeg, bounceJitterDeg);
            _vel = Rotate(_vel, jitter);
            _vel = _vel.normalized * speedPxPerSec;
        }

        targetRect.anchoredPosition = next;
    }

    private void InitRandomVelocity()
    {
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized;
        _vel = dir * speedPxPerSec;
    }

    // Prevent very shallow angles that lead to highly predictable edge-gliding.
    // Equivalent to requiring abs(dot(vHat, normal)) >= cos(72deg) ~ 0.309
    private void EnforceMinAngle(Vector2 inwardNormal)
    {
        Vector2 vHat = _vel.normalized;
        Vector2 n = inwardNormal.normalized;

        float minDot = Mathf.Cos((90f - minAngleDeg) * Mathf.Deg2Rad); // cos(72deg) for 18deg
        float dotAbs = Mathf.Abs(Vector2.Dot(vHat, n));

        if (dotAbs >= minDot) return;

        // Push direction slightly toward the inward normal until constraint is met
        for (int i = 0; i < 25; i++)
        {
            float k = 0.20f + 0.05f * i;
            Vector2 adjusted = (vHat + n * k).normalized;
            float d = Mathf.Abs(Vector2.Dot(adjusted, n));
            if (d >= minDot)
            {
                _vel = adjusted * speedPxPerSec;
                return;
            }
        }

        // Fallback if somehow not satisfied
        _vel = (vHat + n * 0.8f).normalized * speedPxPerSec;
    }

    private Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }

    public Vector2 CurrentTargetPosPx()
    {
        return targetRect != null ? targetRect.anchoredPosition : Vector2.zero;
    }
}
