using System.Globalization;
using UnityEngine;

public class TrialCsvLogger : MonoBehaviour
{
    public SimpleExperimentVR experiment;
    public string fileName = "Trials.csv";

    private string _path;
    private bool _initialized;

    void OnEnable()
    {
        if (experiment == null)
        {
            Debug.LogError("TrialCsvLogger: experiment reference missing.");
            return;
        }

        _path = ExperimentPaths.PathInSession(fileName);

        if (!_initialized)
        {
            ExperimentPaths.WriteAllText(_path,
                "trialIndex,alertNumber,scheduledStartSec,actualStartSec,trialInitiatedUtc,correctDirection,responseDirection,responseTimeSec,reactionTimeSec,outcome\n");
            _initialized = true;
        }

        experiment.OnTrialCompleted += HandleTrial;
    }

    void OnDisable()
    {
        if (experiment != null)
            experiment.OnTrialCompleted -= HandleTrial;
    }

    private void HandleTrial(SimpleExperimentVR.TrialResult r)
    {
        string line =
            r.trialIndex.ToString() + "," +
            r.alertNumber.ToString() + "," +
            F(r.scheduledStartSec) + "," +
            F(r.actualStartSec) + "," +
            Escape(r.trialInitiatedUtc) + "," +
            r.correctDirection.ToString() + "," +
            r.responseDirection.ToString() + "," +
            F(r.responseTimeSec) + "," +
            F(r.reactionTimeSec) + "," +
            r.outcome;

        ExperimentPaths.AppendLine(_path, line);
    }

    private string F(float v)
    {
        return v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\""))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
