using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleExperimentVR : MonoBehaviour
{
    public enum AlertDirection { Left, Both, Right, Unknown }

    public struct TrialResult
    {
        public int trialIndex;
        public int alertNumber;

        public float scheduledStartSec;
        public float actualStartSec;

        public string trialInitiatedUtc;

        public AlertDirection correctDirection;
        public AlertDirection responseDirection;

        public float responseTimeSec;   // experiment time (seconds since audio start)
        public float reactionTimeSec;   // responseTimeSec - actualStartSec

        public string outcome;          // Correct / Incorrect / Missed / Missed_NotReached
    }

    [Header("Session")]
    public string participantId = "P00";
    public string conditionName = "ConditionA";

    [Header("Audio (single combined track)")]
    public AudioSource stimulusSource;
    public AudioClip combinedStimulusClip;
    public bool playOnStart = true;
    public float startDelaySeconds = 0.1f;

    [Header("CSV Input (trials)")]
    public TextAsset alertScheduleCsv;
    public string inputCsvCopyName = "InputAlertSchedule.csv";

    [Header("External session (Experiment folder)")]
    [Tooltip("When true, session CSV and audio are loaded by ExperimentSessionConfigurator; leave alertScheduleCsv and combinedStimulusClip unset.")]
    public bool useExternalSessionConfig = false;

    [Header("Trial timing")]
    public bool endTrialAtNextStart = true;
    public float responseWindowSeconds = 2.0f;

    [Header("End grace")]
    [Tooltip("Extra seconds after track end to still accept responses and avoid last-trial misses.")]
    public float endGraceSeconds = 1.0f;

    [Header("Target placement (old 3D target, keep off for TrackingBoard task)")]
    public Transform targetTransform;
    public Transform headCameraTransform;
    public Vector3 targetOffsetLocal = new Vector3(0f, 0f, 3.5f);
    public bool placeTargetOnBegin = false;
    public bool disableTargetMoverOnBegin = true;
    public bool showTargetOnBegin = true;

    [Header("Optional: TrackingBoard placement")]
    [Tooltip("If assigned, PlaceNow() will be called at experiment begin.")]
    public VRTrackingBoardPlacer trackingBoardPlacer;
    public bool placeBoardOnBegin = true;
    public float boardPlaceDelaySeconds = 0.35f;

    [Header("End of block")]
    public bool quitOnAudioEnd = true;
    public float quitDelaySeconds = 1.0f;

    [Header("Debug")]
    public bool verboseLogs = false;

    public bool IsRunning { get; private set; }

    public bool IsAwaitingResponse => IsRunning && _waitingForResponse && !_hasResponse;

    // Experiment time in seconds since scheduled audio start.
    // Continues increasing even after audio ends, which is what we want for endGraceSeconds.
    public float ExperimentTimeSec
    {
        get
        {
            if (!_clockStarted) return 0f;
            return (float)(AudioSettings.dspTime - _dspStartTime);
        }
    }

    // Keep this for compatibility with other scripts
    public float AudioTime => ExperimentTimeSec;

    public string SessionDirectory => ExperimentPaths.SessionDirectory;

    public event Action<TrialResult> OnTrialCompleted;

    private List<AlertScheduleCsv.Trial> _trials = new List<AlertScheduleCsv.Trial>();

    private bool _waitingForResponse;
    private bool _hasResponse;
    private AlertDirection _responseDir = AlertDirection.Unknown;
    private float _responseTime = -1f;

    private float _currentActualStart;
    private AlertDirection _currentCorrect;

    private double _dspStartTime;
    private bool _clockStarted;

    void Awake()
    {
        ExperimentPaths.InitSession(participantId, conditionName);

        if (!useExternalSessionConfig && alertScheduleCsv != null)
        {
            string copyPath = ExperimentPaths.PathInSession(inputCsvCopyName);
            ExperimentPaths.WriteAllText(copyPath, alertScheduleCsv.text);
        }
    }

    void Start()
    {
        if (stimulusSource == null)
        {
            Debug.LogError("SimpleExperimentVR: stimulusSource is not assigned.");
            return;
        }

        if (useExternalSessionConfig)
        {
            // CSV and audio are set by ExperimentSessionConfigurator; it will call Begin() when ready.
            return;
        }

        if (combinedStimulusClip == null)
        {
            Debug.LogError("SimpleExperimentVR: combinedStimulusClip is not assigned.");
            return;
        }
        if (alertScheduleCsv == null)
        {
            Debug.LogError("SimpleExperimentVR: alertScheduleCsv is not assigned.");
            return;
        }

        _trials = AlertScheduleCsv.Parse(alertScheduleCsv.text);

        if (_trials.Count == 0)
        {
            Debug.LogError("SimpleExperimentVR: CSV parsed 0 trials.");
            return;
        }

        if (playOnStart)
            Begin();
    }

    /// <summary>
    /// Called by ExperimentSessionConfigurator when loading from the external Experiment folder.
    /// Sets participant/condition, parses CSV, copies it to session output, sets the audio clip, and is ready for Begin().
    /// </summary>
    public void SetSessionFromExternal(string csvText, AudioClip audioClip)
    {
        ExperimentPaths.InitSession(participantId, conditionName);

        string copyPath = ExperimentPaths.PathInSession(inputCsvCopyName);
        ExperimentPaths.WriteAllText(copyPath, csvText);

        _trials = AlertScheduleCsv.Parse(csvText);
        combinedStimulusClip = audioClip;
    }

    public void Begin()
    {
        StopAllCoroutines();
        StartCoroutine(RunBlock());
    }

    public void SubmitResponse(AlertDirection dir)
    {
        if (!IsRunning) return;
        if (!_waitingForResponse) return;
        if (_hasResponse) return;

        float now = ExperimentTimeSec;
        if (now < _currentActualStart) return;

        _hasResponse = true;
        _responseDir = dir;
        _responseTime = now;

        if (verboseLogs)
            Debug.Log("[SimpleExperimentVR] Response " + dir + " at " + now.ToString("F3"));
    }

    private IEnumerator RunBlock()
    {
        stimulusSource.clip = combinedStimulusClip;
        stimulusSource.loop = false;

        float trackLen = combinedStimulusClip.length;
        float sessionEnd = trackLen + Mathf.Max(0f, endGraceSeconds);

        // Optional: place TrackingBoard after XR pose settles
        if (placeBoardOnBegin && trackingBoardPlacer != null)
        {
            yield return new WaitForSeconds(boardPlaceDelaySeconds);
            trackingBoardPlacer.PlaceNow();
        }

        // Optional: old 3D target placement (keep off for TrackingBoard task)
        if (placeTargetOnBegin)
            PlaceTargetInFrontOfHead();

        // Start a stable clock and schedule audio precisely
        _dspStartTime = AudioSettings.dspTime + Math.Max(0.0, startDelaySeconds);
        _clockStarted = true;

        stimulusSource.PlayScheduled(_dspStartTime);

        // Wait until the audio "time zero" moment
        yield return new WaitForSeconds(startDelaySeconds);

        IsRunning = true;

        for (int i = 0; i < _trials.Count; i++)
        {
            var t = _trials[i];

            float scheduled = t.startSeconds;
            float nextScheduled = (i < _trials.Count - 1) ? _trials[i + 1].startSeconds : sessionEnd;

            // Wait until scheduled time, but stop if session already ended
            while (ExperimentTimeSec < scheduled && ExperimentTimeSec < sessionEnd)
                yield return null;

            // Session ended before reaching this trial
            if (ExperimentTimeSec >= sessionEnd && ExperimentTimeSec < scheduled)
            {
                EmitTrialResult(new TrialResult
                {
                    trialIndex = t.trialIndex,
                    alertNumber = t.alertNumber,
                    scheduledStartSec = scheduled,
                    actualStartSec = -1f,
                    trialInitiatedUtc = "",
                    correctDirection = ParseDirection(t.correctDirection),
                    responseDirection = AlertDirection.Unknown,
                    responseTimeSec = -1f,
                    reactionTimeSec = -1f,
                    outcome = "Missed_NotReached"
                });
                continue;
            }

            float actualStart = ExperimentTimeSec;

            _currentActualStart = actualStart;
            _currentCorrect = ParseDirection(t.correctDirection);

            _waitingForResponse = true;
            _hasResponse = false;
            _responseDir = AlertDirection.Unknown;
            _responseTime = -1f;

            string initiatedUtc = DateTime.UtcNow.ToString("O");

            float deadline = actualStart + responseWindowSeconds;

            if (endTrialAtNextStart)
                deadline = Mathf.Min(deadline, nextScheduled);

            deadline = Mathf.Min(deadline, sessionEnd);

            // IMPORTANT: do not stop waiting just because AudioSource.isPlaying changes.
            while (ExperimentTimeSec < deadline)
            {
                if (_hasResponse) break;
                yield return null;
            }

            if (_hasResponse)
            {
                float rt = _responseTime - actualStart;
                bool correct = (_responseDir == _currentCorrect);

                EmitTrialResult(new TrialResult
                {
                    trialIndex = t.trialIndex,
                    alertNumber = t.alertNumber,
                    scheduledStartSec = scheduled,
                    actualStartSec = actualStart,
                    trialInitiatedUtc = initiatedUtc,
                    correctDirection = _currentCorrect,
                    responseDirection = _responseDir,
                    responseTimeSec = _responseTime,
                    reactionTimeSec = rt,
                    outcome = correct ? "Correct" : "Incorrect"
                });
            }
            else
            {
                EmitTrialResult(new TrialResult
                {
                    trialIndex = t.trialIndex,
                    alertNumber = t.alertNumber,
                    scheduledStartSec = scheduled,
                    actualStartSec = actualStart,
                    trialInitiatedUtc = initiatedUtc,
                    correctDirection = _currentCorrect,
                    responseDirection = AlertDirection.Unknown,
                    responseTimeSec = -1f,
                    reactionTimeSec = -1f,
                    outcome = "Missed"
                });
            }

            _waitingForResponse = false;
        }

        // Keep running until the full session end including grace
        while (ExperimentTimeSec < sessionEnd)
            yield return null;

        IsRunning = false;

        if (quitOnAudioEnd)
        {
            // Extra delay helps prevent cutting the final audio buffer and gives loggers time to flush
            yield return new WaitForSeconds(quitDelaySeconds);
            QuitApp();
        }
    }

    private void PlaceTargetInFrontOfHead()
    {
        if (targetTransform == null || headCameraTransform == null) return;

        if (showTargetOnBegin && !targetTransform.gameObject.activeSelf)
            targetTransform.gameObject.SetActive(true);

        if (disableTargetMoverOnBegin)
        {
            var mover = targetTransform.GetComponent<TargetMoverSimple>();
            if (mover != null) mover.enabled = false;
        }

        Vector3 worldPos = headCameraTransform.TransformPoint(targetOffsetLocal);
        targetTransform.position = worldPos;
        targetTransform.rotation = Quaternion.identity;

        if (verboseLogs)
            Debug.Log("[SimpleExperimentVR] Placed target at " + targetTransform.position);
    }

    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EmitTrialResult(TrialResult r)
    {
        if (verboseLogs)
            Debug.Log("[Trial] " + r.trialIndex + " outcome=" + r.outcome + ", response=" + r.responseDirection);

        OnTrialCompleted?.Invoke(r);
    }

    private AlertDirection ParseDirection(string s)
    {
        if (string.IsNullOrEmpty(s)) return AlertDirection.Unknown;
        string x = s.Trim().ToLower();

        if (x == "l" || x.Contains("left")) return AlertDirection.Left;
        if (x == "b" || x.Contains("both") || x.Contains("up") || x.Contains("back")) return AlertDirection.Both;
        if (x == "r" || x.Contains("right")) return AlertDirection.Right;

        if (x == "0") return AlertDirection.Left;
        if (x == "1") return AlertDirection.Both;
        if (x == "2") return AlertDirection.Right;

        return AlertDirection.Unknown;
    }
}
