using UnityEngine;
using UnityEngine.UI;

public class RayCursorToBoard2D : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;
    public float maxDistance = 10f;
    public LayerMask boardMask = ~0;

    [Header("Board mapping")]
    public Collider boardCollider;

    [Tooltip("Physical size of the board collider in meters, width and height.")]
    public Vector2 boardSizeMeters = new Vector2(0.75f, 0.75f);

    [Header("UI")]
    public RectTransform trackingArea;
    public RectTransform cursorRect;

    public bool OnBoard { get; private set; }
    public Vector2 CursorPosPx { get; private set; }

    void Update()
    {
        OnBoard = false;

        if (rayOrigin == null || boardCollider == null || trackingArea == null || cursorRect == null)
            return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, boardMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == boardCollider)
            {
                OnBoard = true;

                // Convert hit to board-local coordinates
                Vector3 local = transform.InverseTransformPoint(hit.point);

                // Normalize into 0..1 where local x,y range is [-W/2..W/2], [-H/2..H/2]
                float u = (local.x / boardSizeMeters.x) + 0.5f;
                float v = (local.y / boardSizeMeters.y) + 0.5f;

                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);

                Rect r = trackingArea.rect;
                float x = (u - 0.5f) * r.width;
                float y = (v - 0.5f) * r.height;

                Vector2 pos = new Vector2(x, y);
                CursorPosPx = pos;
                cursorRect.anchoredPosition = pos;
            }
        }
    }
}
