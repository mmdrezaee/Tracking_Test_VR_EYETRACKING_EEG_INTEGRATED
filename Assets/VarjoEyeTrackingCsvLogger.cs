using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Varjo.XR;

public class VarjoEyeTrackingCsvLogger : MonoBehaviour
{
    [Header("Optional: sync time to experiment clock")]
    public SimpleExperimentVR experiment;

    [Header("Output")]
    public string fileName = "EyeTracking.csv";

    [Header("Varjo gaze settings (if supported by your Varjo plugin)")]
    public bool requestCalibrationOnStart = false;
    public VarjoEyeTracking.GazeOutputFilterType filterType = VarjoEyeTracking.GazeOutputFilterType.Standard;
    public VarjoEyeTracking.GazeOutputFrequency frequency = VarjoEyeTracking.GazeOutputFrequency.MaximumSupported;

    [Header("Logging rate")]
    [Tooltip("0 = log every frame. Otherwise logs at this Hz.")]
    public int logHz = 0;

    [Header("Optional gaze raycast to world")]
    public bool doWorldHitTest = false;
    public float hitTestMaxDistance = 25f;
    public LayerMask hitMask = ~0;

    private StreamWriter _w;
    private float _nextLogTime;

    // IMPORTANT: not readonly because we use them as out targets (or assign back from out results)
    private List<VarjoEyeTracking.GazeData> _gaze = new List<VarjoEyeTracking.GazeData>(512);
    private List<VarjoEyeTracking.EyeMeasurements> _meas = new List<VarjoEyeTracking.EyeMeasurements>(512);

    void Start()
    {
        string path;
        try
        {
            path = ExperimentPaths.PathInSession(fileName);
        }
        catch
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
        }

        _w = new StreamWriter(path, false);
        _w.AutoFlush = true;

        _w.WriteLine(
            "time_sec,frameNumber,captureTimeNs,status," +
            "origin_x,origin_y,origin_z,dir_x,dir_y,dir_z," +
            "focusDistance,focusStability," +
            "ipd_mm,leftPupil_mm,rightPupil_mm,leftOpen,rightOpen," +
            "hit,hit_x,hit_y,hit_z"
        );

        // If your Varjo plugin version does not contain these methods, comment these two lines.
        VarjoEyeTracking.SetGazeOutputFilterType(filterType);
        VarjoEyeTracking.SetGazeOutputFrequency(frequency);

        if (requestCalibrationOnStart)
        {
            VarjoEyeTracking.RequestGazeCalibration();
        }

        _nextLogTime = Time.time;
    }

    void OnDestroy()
    {
        if (_w != null)
        {
            _w.Flush();
            _w.Close();
            _w = null;
        }
    }

    void Update()
    {
        if (_w == null) return;

        if (logHz > 0)
        {
            float interval = 1f / Mathf.Max(1, logHz);
            if (Time.time < _nextLogTime) return;
            _nextLogTime = Time.time + interval;
        }

        // Varjo API returns lists via out parameters.
        // Use locals and assign back so we don't pass fields as out and we keep references updated.
        List<VarjoEyeTracking.GazeData> gaze;
        List<VarjoEyeTracking.EyeMeasurements> meas;
        VarjoEyeTracking.GetGazeList(out gaze, out meas);

        _gaze = gaze ?? _gaze;
        _meas = meas ?? _meas;

        if (_gaze == null || _gaze.Count == 0) return;

        float t = (experiment != null) ? experiment.AudioTime : Time.time;

        int measCount = (_meas != null) ? _meas.Count : 0;

        for (int i = 0; i < _gaze.Count; i++)
        {
            var g = _gaze[i];

            Vector3 o = g.gaze.origin;
            Vector3 d = g.gaze.forward;

            float ipd = -1f;
            float lp = -1f;
            float rp = -1f;
            float lo = -1f;
            float ro = -1f;

            if (i < measCount)
            {
                var m = _meas[i];
                ipd = m.interPupillaryDistanceInMM;
                lp = m.leftPupilDiameterInMM;
                rp = m.rightPupilDiameterInMM;
                lo = m.leftEyeOpenness;
                ro = m.rightEyeOpenness;
            }

            int hit = 0;
            Vector3 hp = Vector3.zero;

            if (doWorldHitTest && g.status == VarjoEyeTracking.GazeStatus.Valid)
            {
                Ray r = new Ray(o, d);
                if (Physics.Raycast(r, out RaycastHit h, hitTestMaxDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    hit = 1;
                    hp = h.point;
                }
            }

            string line =
                F(t) + "," +
                g.frameNumber + "," +
                g.captureTime + "," +
                ((int)g.status) + "," +
                F(o.x) + "," + F(o.y) + "," + F(o.z) + "," +
                F(d.x) + "," + F(d.y) + "," + F(d.z) + "," +
                F(g.focusDistance) + "," +
                F(g.focusStability) + "," +
                F(ipd) + "," +
                F(lp) + "," +
                F(rp) + "," +
                F(lo) + "," +
                F(ro) + "," +
                hit + "," +
                F(hp.x) + "," + F(hp.y) + "," + F(hp.z);

            _w.WriteLine(line);
        }
    }

    private string F(float v) => v.ToString("0.######", CultureInfo.InvariantCulture);
}
