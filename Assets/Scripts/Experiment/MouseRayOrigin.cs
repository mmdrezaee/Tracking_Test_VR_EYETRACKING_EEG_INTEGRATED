using UnityEngine;

/// <summary>
/// Desktop simulator: drives the ray origin transform from the mouse position
/// so you can use the mouse as a \"right hand\" controller in the Editor or a
/// non-VR build. Works with RightHandRay and RayCursorToBoard2D as long as they
/// reference the same rayOrigin transform.
/// </summary>
public class MouseRayOrigin : MonoBehaviour
{
    [Header("Ray origin to drive")]
    [Tooltip("Transform used as ray origin by RightHandRay / RayCursorToBoard2D.")]
    public Transform rayOrigin;

    [Header("Camera")]
    [Tooltip("Camera used to convert mouse position into a world-space ray. If empty, Camera.main is used.")]
    public Camera referenceCamera;

    [Header("When to run")]
    [Tooltip("If true, only active in the Unity Editor (recommended).")]
    public bool editorOnly = true;

    void Awake()
    {
        if (referenceCamera == null)
            referenceCamera = Camera.main;
    }

    void Update()
    {
        if (editorOnly && !Application.isEditor)
            return;

        if (rayOrigin == null || referenceCamera == null)
            return;

        // Build a ray from the camera through the current mouse position.
        Ray ray = referenceCamera.ScreenPointToRay(Input.mousePosition);

        // Place the \"controller\" at the camera and point it along the mouse ray.
        rayOrigin.position = ray.origin;
        rayOrigin.rotation = Quaternion.LookRotation(ray.direction, referenceCamera.transform.up);
    }
}

