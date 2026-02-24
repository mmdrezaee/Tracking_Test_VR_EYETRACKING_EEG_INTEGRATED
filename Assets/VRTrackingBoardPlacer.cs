using UnityEngine;

public class VRTrackingBoardPlacer : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;

    [Header("Placement")]
    public float placeDelaySeconds = 0.35f;
    public float distanceMeters = 2.0f;
    public float heightOffsetMeters = 0.0f;
    public bool yawOnly = true;

    private bool _placed;

    void Start()
    {
        _placed = false;
        Invoke(nameof(PlaceNow), placeDelaySeconds);
    }

    public void PlaceNow()
    {
        if (_placed) return;
        if (headTransform == null) return;

        Vector3 forward = headTransform.forward;
        if (yawOnly)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f) forward = headTransform.forward.normalized;
        }

        Vector3 pos = headTransform.position + forward * distanceMeters + Vector3.up * heightOffsetMeters;
        transform.position = pos;

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = rot;

        // Safety: ensure board is in front of head
        Vector3 toBoard = transform.position - headTransform.position;
        if (Vector3.Dot(headTransform.forward, toBoard) < 0f)
        {
            transform.position = headTransform.position - toBoard;
        }

        _placed = true;
    }
}