using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// One-build, many-configurations: loads session CSV and the matching Mixed WAV from the external Experiment folder,
/// then injects them into SimpleExperimentVR and starts the experiment.
/// Example: Subj01_Sess01_alert_info.csv with Subj01_Sess01_N0.846_mode1_mode2_rep2_Mixed.wav
/// </summary>
public class ExperimentSessionConfigurator : MonoBehaviour
{
    [Header("Experiment folder (external)")]
    [Tooltip("Root path containing Subject_01 .. Subject_20, each with SubjXX_SessYY_alert_info.csv and SubjXX_SessYY_*_Mixed.wav")]
    public string experimentRootPath = @"C:\Users\rezaeia2\Desktop\Tracking Test Project\Experiment";

    [Header("Subject & Session")]
    [Tooltip("Subject 1–20. Session 0 = EEG baseline (5 min fixation); 1–24 = main task.")]
    [Range(0, 20)]
    public int subject = 1;
    [Range(0, 24)]
    public int session = 1;

    [Header("References")]
    public SimpleExperimentVR experiment;
    [Tooltip("Required for Session 0 (baseline). Add a GameObject with BaselineRunner to the scene.")]
    public BaselineRunner baselineRunner;

    [Header("Debug")]
    public bool verboseLogs = false;

    private void Awake()
    {
        // Session 0 = EEG baseline: init session folder in Awake so loggers see it in their Start()
        if (session == 0 && experiment != null && subject >= 1 && subject <= 20 && baselineRunner != null)
        {
            experiment.participantId = "P" + subject.ToString("D2");
            experiment.conditionName = "Baseline";
            ExperimentPaths.InitSession(experiment.participantId, experiment.conditionName);
        }
    }

    private void Start()
    {
        if (experiment == null)
        {
            Debug.LogError("ExperimentSessionConfigurator: experiment (SimpleExperimentVR) is not assigned.");
            return;
        }

        if (!experiment.useExternalSessionConfig)
        {
            Debug.LogWarning("ExperimentSessionConfigurator: SimpleExperimentVR.useExternalSessionConfig is false. Enable it to use external CSV + audio.");
            return;
        }

        // Session 0 = EEG baseline: fixation cross only, 5 min, no audio/trials
        if (session == 0)
        {
            if (baselineRunner == null)
            {
                Debug.LogError("ExperimentSessionConfigurator: Session 0 (baseline) requires BaselineRunner to be assigned.");
                return;
            }
            if (subject < 1 || subject > 20)
            {
                Debug.LogError("ExperimentSessionConfigurator: Subject must be 1–20 for baseline.");
                return;
            }
            if (verboseLogs)
                Debug.Log("[ExperimentSessionConfigurator] Starting EEG baseline for Subject " + subject + " (5 min fixation).");
            baselineRunner.StartBaseline();
            return;
        }

        StartCoroutine(LoadAndStartSession());
    }

    private IEnumerator LoadAndStartSession()
    {
        if (subject < 1 || subject > 20 || session < 1 || session > 24)
        {
            Debug.LogError("ExperimentSessionConfigurator: Subject must be 1–20 and Session 1–24 for main task.");
            yield break;
        }

        string subjectDirName = "Subject_" + subject.ToString("D2");
        string subjectDir = Path.Combine(experimentRootPath, subjectDirName);

        if (!Directory.Exists(subjectDir))
        {
            Debug.LogError("ExperimentSessionConfigurator: Subject folder not found: " + subjectDir);
            yield break;
        }

        // CSV path: Subj01_Sess01_alert_info.csv
        string csvFileName = string.Format("Subj{0:D2}_Sess{1:D2}_alert_info.csv", subject, session);
        string csvPath = Path.Combine(subjectDir, csvFileName);

        if (!File.Exists(csvPath))
        {
            Debug.LogError("ExperimentSessionConfigurator: CSV not found: " + csvPath);
            yield break;
        }

        string csvText = File.ReadAllText(csvPath);
        if (string.IsNullOrWhiteSpace(csvText))
        {
            Debug.LogError("ExperimentSessionConfigurator: CSV is empty: " + csvPath);
            yield break;
        }

        if (verboseLogs)
            Debug.Log("[ExperimentSessionConfigurator] Loaded CSV: " + csvPath);

        // Audio: find the single file matching SubjXX_SessYY_*_Mixed.wav
        string prefix = string.Format("Subj{0:D2}_Sess{1:D2}_", subject, session);
        string[] wavFiles = Directory.GetFiles(subjectDir, "*.wav");
        string mixedWavPath = null;
        foreach (string f in wavFiles)
        {
            string name = Path.GetFileName(f);
            if (name.StartsWith(prefix) && name.Contains("_Mixed."))
            {
                mixedWavPath = f;
                break;
            }
        }

        if (string.IsNullOrEmpty(mixedWavPath))
        {
            Debug.LogError("ExperimentSessionConfigurator: No Mixed WAV found in " + subjectDir + " for prefix " + prefix + "*_Mixed.wav. Found .wav: " + (wavFiles.Length > 0 ? string.Join(", ", wavFiles) : "none"));
            yield break;
        }

        if (verboseLogs)
            Debug.Log("[ExperimentSessionConfigurator] Loading WAV: " + mixedWavPath);

        // Set participant/condition for output folder naming
        experiment.participantId = "P" + subject.ToString("D2");
        experiment.conditionName = "Sess" + session.ToString("D2");

        // Load WAV from file (async)
        string uri = "file:///" + mixedWavPath.Replace("\\", "/");
        using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("ExperimentSessionConfigurator: Failed to load WAV: " + req.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogError("ExperimentSessionConfigurator: GetContent returned null for " + mixedWavPath);
                yield break;
            }

            experiment.SetSessionFromExternal(csvText, clip);
            if (verboseLogs)
                Debug.Log("[ExperimentSessionConfigurator] Starting experiment for Subject " + subject + ", Session " + session);
            experiment.Begin();
        }
    }
}
