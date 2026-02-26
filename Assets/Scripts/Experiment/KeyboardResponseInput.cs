using UnityEngine;

/// <summary>
/// Desktop simulator: submit sound-direction responses with the keyboard so you can test
/// the experiment without a VR headset or controllers. Use in Editor or in a non-VR build.
/// </summary>
public class KeyboardResponseInput : MonoBehaviour
{
    public SimpleExperimentVR experiment;

    [Header("Keys (only used when experiment is awaiting response)")]
    public KeyCode leftKey = KeyCode.Alpha1;
    public KeyCode bothKey = KeyCode.Alpha2;
    public KeyCode rightKey = KeyCode.Alpha3;

    [Header("Alternate keys (optional)")]
    public KeyCode leftKeyAlt = KeyCode.LeftArrow;
    public KeyCode bothKeyAlt = KeyCode.UpArrow;
    public KeyCode rightKeyAlt = KeyCode.RightArrow;

    [Header("Debug")]
    public bool debugLogs = false;

    void Update()
    {
        if (experiment == null) return;
        if (!experiment.IsRunning || !experiment.IsAwaitingResponse) return;

        if (GetKey(leftKey) || GetKey(leftKeyAlt))
        {
            experiment.SubmitResponse(SimpleExperimentVR.AlertDirection.Left);
            if (debugLogs) Debug.Log("[KeyboardResponseInput] Submitted Left");
            return;
        }
        if (GetKey(bothKey) || GetKey(bothKeyAlt))
        {
            experiment.SubmitResponse(SimpleExperimentVR.AlertDirection.Both);
            if (debugLogs) Debug.Log("[KeyboardResponseInput] Submitted Both");
            return;
        }
        if (GetKey(rightKey) || GetKey(rightKeyAlt))
        {
            experiment.SubmitResponse(SimpleExperimentVR.AlertDirection.Right);
            if (debugLogs) Debug.Log("[KeyboardResponseInput] Submitted Right");
        }
    }

    private static bool GetKey(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }
}
