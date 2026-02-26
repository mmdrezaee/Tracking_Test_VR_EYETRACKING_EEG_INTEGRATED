using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using LSL;

public class LslCsvLogger : MonoBehaviour
{
    [Header("Optional: sync time to experiment clock")]
    public SimpleExperimentVR experiment;

    [Header("LSL")]
    public string StreamName = "";
    public double MaxChunkDurationSec = 0.2;

    [Header("Output")]
    public string fileName = "LSL_EEG.csv";

    [Header("Logging rate")]
    [Tooltip("0 = log every frame (all samples pulled each frame). Otherwise, pull+log at this Hz.")]
    public int logHz = 0;

    private StreamWriter _w;
    private float _nextLogTime;

    private ContinuousResolver resolver;
    private StreamInlet inlet;

    private float[,] dataBuffer;
    private double[] tsBuffer;

    private int nChannels;
    private int bufSamples;

    private readonly StringBuilder sb = new StringBuilder(4096);

    // Keep the same electrode columns you requested (DSI-24 typical order).
    private static readonly string[] Dsi24Labels =
    {
        "Fp1-Pz","Fp2-Pz","Fz-Pz","F3-Pz","F4-Pz","F7-Pz","F8-Pz",
        "Cz-Pz","C3-Pz","C4-Pz","T3-Pz","T4-Pz","T5-Pz","T6-Pz",
        "P3-Pz","Pz","P4-Pz","O1-Pz","O2-Pz","A1-Pz","A2-Pz",
        "X1","X2","X3"
    };

    IEnumerator Start()
    {
        // Match the eye-tracking logger path behavior
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

        if (string.IsNullOrWhiteSpace(StreamName))
        {
            Debug.LogError("LslCsvLogger: StreamName is empty.");
            yield break;
        }

        // Resolve stream by name
        resolver = new ContinuousResolver("name", StreamName);

        var results = resolver.results();
        while (results.Length == 0)
        {
            yield return new WaitForSeconds(0.1f);
            results = resolver.results();
        }

        inlet = new StreamInlet(results[0]);

        nChannels = inlet.info().channel_count();
        double srate = inlet.info().nominal_srate();
        if (srate <= 0) srate = 60; // fallback for irregular rate streams

        bufSamples = Mathf.CeilToInt((float)(srate * MaxChunkDurationSec));
        dataBuffer = new float[bufSamples, nChannels];
        tsBuffer = new double[bufSamples];

        // Header: match the eye logger style for time column ("time_sec") and also include lsl_timestamp.
        // Keep your electrode labels as columns (or ch0.. if not 24).
        sb.Clear();
        sb.Append("time_sec,lsl_timestamp");
        if (nChannels == 24)
        {
            for (int i = 0; i < 24; i++)
            {
                sb.Append(',');
                sb.Append(Dsi24Labels[i]);
            }
        }
        else
        {
            for (int c = 0; c < nChannels; c++)
            {
                sb.Append(",ch");
                sb.Append(c);
            }
        }
        _w.WriteLine(sb.ToString());

        _nextLogTime = Time.time;

        Debug.Log($"LslCsvLogger started. Writing to: {path} (channels={nChannels})");
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
        if (_w == null || inlet == null) return;

        // Match the eye logger’s logging-rate gating
        if (logHz > 0)
        {
            float interval = 1f / Mathf.Max(1, logHz);
            if (Time.time < _nextLogTime) return;
            _nextLogTime = Time.time + interval;
        }

        // Pull chunk (may contain multiple samples per frame)
        int samplesReturned = inlet.pull_chunk(dataBuffer, tsBuffer);
        if (samplesReturned <= 0) return;

        // Match the eye logger’s time base (use Time.time during baseline when experiment is not running)
        float t = (experiment != null && experiment.IsRunning) ? experiment.AudioTime : Time.time;

        // Write one row per LSL sample.
        // Note: time_sec here is the Unity/experiment clock at the moment of logging (same as eye logger),
        // and lsl_timestamp is the native LSL time for the sample.
        for (int s = 0; s < samplesReturned; s++)
        {
            sb.Clear();

            sb.Append(F(t));
            sb.Append(',');
            sb.Append(tsBuffer[s].ToString("G17", CultureInfo.InvariantCulture));

            for (int c = 0; c < nChannels; c++)
            {
                sb.Append(',');
                sb.Append(F(dataBuffer[s, c]));
            }

            _w.WriteLine(sb.ToString());
        }
    }

    private string F(float v) => v.ToString("0.######", CultureInfo.InvariantCulture);
}
