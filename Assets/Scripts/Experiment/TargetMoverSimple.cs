using UnityEngine;

public class TargetMoverSimple : MonoBehaviour
{
    public Transform center;

    public float amplitudeX = 0.1f;
    public float amplitudeY = 0.1f;

    public float frequencyX = 0.35f;
    public float frequencyY = 0.22f;

    public bool lockY = false;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = (center != null) ? center.position : transform.position;
    }

    void Update()
    {
        Vector3 basePos = (center != null) ? center.position : _startPos;

        float x = Mathf.Sin(Time.time * Mathf.PI * 2f * frequencyX) * amplitudeX;
        float y = Mathf.Sin(Time.time * Mathf.PI * 2f * frequencyY) * amplitudeY;

        Vector3 pos = basePos + new Vector3(x, lockY ? 0f : y, 0f);
        transform.position = pos;
    }
}
