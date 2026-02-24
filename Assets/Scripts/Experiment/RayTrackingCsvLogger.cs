using System.Globalization;
using UnityEngine;

public class RayTrackingCsvLogger : MonoBehaviour
{
    public SimpleExperimentVR experiment;
    public RightHandRay rightHandRay;

    public string fileName = "RayTracking.csv";
    public int logHz = 60;

    private string _path;
    private float _nextTime;
    private bool _initialized;

    void Start()
    {
        if (experiment == null) Debug.LogError("RayTrackingCsvLogger: experiment missing.");
        if (rightHandRay == null) Debug.LogError("RayTrackingCsvLogger: rightHandRay missing.");

        _path = ExperimentPaths.PathInSession(fileName);

        if (!_initialized)
        {
            ExperimentPaths.WriteAllText(_path,
                "audioTimeSec,isRunning,isOnTarget,rayOriginX,rayOriginY,rayOriginZ,rayDirX,rayDirY,rayDirZ,targetX,targetY,targetZ\n");
            _initialized = true;
        }

        _nextTime = Time.time;
    }

    void Update()
    {
        if (experiment == null || rightHandRay == null) return;

        float interval = (logHz <= 0) ? 0.016f : (1f / logHz);

        if (Time.time < _nextTime) return;
        _nextTime = Time.time + interval;

        Vector3 ro = rightHandRay.RayOrigin;
        Vector3 rd = rightHandRay.RayDirection;
        Vector3 tp = rightHandRay.TargetPosition;

        string line =
            F(experiment.AudioTime) + "," +
            (experiment.IsRunning ? "1" : "0") + "," +
            (rightHandRay.IsOnTarget ? "1" : "0") + "," +
            F(ro.x) + "," + F(ro.y) + "," + F(ro.z) + "," +
            F(rd.x) + "," + F(rd.y) + "," + F(rd.z) + "," +
            F(tp.x) + "," + F(tp.y) + "," + F(tp.z);

        ExperimentPaths.AppendLine(_path, line);
    }

    private string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
