using UnityEngine;

public class RightHandRay : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;
    public LineRenderer lineRenderer;
    public float maxDistance = 30f;
    public LayerMask hitMask = ~0;

    [Header("Target")]
    public Transform targetTransform;

    [Tooltip("Optional. If assigned, this will be toggled when the ray is on target.")]
    public TargetHighlighter targetHighlighter;

    public bool IsOnTarget { get; private set; }
    public Vector3 RayOrigin { get; private set; }
    public Vector3 RayDirection { get; private set; }
    public Vector3 TargetPosition => targetTransform != null ? targetTransform.position : Vector3.zero;

    void Update()
    {
        if (rayOrigin == null) return;

        RayOrigin = rayOrigin.position;
        RayDirection = rayOrigin.forward;

        Ray ray = new Ray(RayOrigin, RayDirection);

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxDistance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        bool newOnTarget = false;
        Vector3 endPos = RayOrigin + RayDirection * maxDistance;

        if (hitSomething)
        {
            endPos = hit.point;

            if (targetTransform != null)
            {
                // Important: treat child collider hits as target hits too
                Transform ht = hit.transform;
                newOnTarget = (ht == targetTransform) || ht.IsChildOf(targetTransform);
            }
        }

        // Update line
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, RayOrigin);
            lineRenderer.SetPosition(1, endPos);
        }

        // Toggle highlight only when state changes
        if (newOnTarget != IsOnTarget)
        {
            IsOnTarget = newOnTarget;

            if (targetHighlighter != null)
                targetHighlighter.SetHighlighted(IsOnTarget);
        }
    }
}
