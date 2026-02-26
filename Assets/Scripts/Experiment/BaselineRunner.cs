using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EEG baseline: shows a fixation cross for a set duration (default 5 min) while EEG and eye tracking record.
/// Participants fixate on the cross, refrain from speaking, and minimize movement.
/// Run by ExperimentSessionConfigurator when Session = 0 (baseline).
/// </summary>
public class BaselineRunner : MonoBehaviour
{
    [Header("Duration")]
    [Tooltip("Baseline recording duration in seconds (default 5 minutes).")]
    public float durationSeconds = 300f;

    [Header("Fixation cross")]
    [Tooltip("If set, this GameObject (e.g. Canvas with a cross) is shown during baseline. If null, a simple cross is created at runtime.")]
    public GameObject fixationCrossRoot;

    [Header("Completion")]
    [Tooltip("Seconds to show completion message before quitting.")]
    public float completionDisplaySeconds = 3f;

    [Header("Visibility")]
    [Tooltip("If true, all other Canvas UIs are hidden during baseline so only the fixation cross is visible.")]
    public bool hideOtherCanvases = true;

    [Header("Debug")]
    public bool verboseLogs = false;

    private float _startTime;
    private bool _running;
    private GameObject _createdCrossRoot;
    private RectTransform _crossRect;
    private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();

    public bool IsRunning => _running;

    public void StartBaseline()
    {
        if (_running) return;

        _running = true;
        _startTime = Time.time;

        if (hideOtherCanvases)
            HideOtherCanvasUIs();

        if (fixationCrossRoot != null)
        {
            fixationCrossRoot.SetActive(true);
        }
        else
        {
            CreateFixationCross();
        }

        if (verboseLogs)
            Debug.Log("[BaselineRunner] Started. Duration=" + durationSeconds + "s. Fixate on the cross, remain still, do not speak.");
    }

    private void CreateFixationCross()
    {
        var canvasGo = new GameObject("BaselineFixationCanvas");
        _createdCrossRoot = canvasGo;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var textGo = new GameObject("FixationCross");
        textGo.transform.SetParent(canvasGo.transform, false);

        _crossRect = textGo.AddComponent<RectTransform>();
        _crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        _crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        _crossRect.pivot = new Vector2(0.5f, 0.5f);
        _crossRect.anchoredPosition = Vector2.zero;
        _crossRect.sizeDelta = new Vector2(200f, 200f);

        var text = textGo.AddComponent<Text>();
        text.text = "+";

        // Use a reliable built-in font (Arial on most Unity installs)
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
        {
            // Fallback to a dynamic OS font if needed
            font = Font.CreateDynamicFontFromOSFont("Arial", 120);
        }
        text.font = font;

        text.fontSize = 140;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // Add a subtle outline to make the cross stand out against varied backgrounds.
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void Update()
    {
        if (!_running) return;

        // Gentle \"breathing\" animation to keep the cross engaging and help maintain fixation.
        if (_crossRect != null)
        {
            float t = Time.time - _startTime;
            float phase = Mathf.Sin(t * Mathf.PI * 2f * 0.25f); // 0.25 Hz
            float scale = 1f + 0.08f * phase;                   // ±8% size change
            _crossRect.localScale = Vector3.one * scale;
        }

        float elapsed = Time.time - _startTime;
        if (elapsed >= durationSeconds)
        {
            _running = false;
            if (fixationCrossRoot != null)
                fixationCrossRoot.SetActive(false);
            if (_createdCrossRoot != null)
                _createdCrossRoot.SetActive(false);

            if (verboseLogs)
                Debug.Log("[BaselineRunner] Baseline complete (" + durationSeconds + "s). Quitting in " + completionDisplaySeconds + "s.");

            Invoke(nameof(QuitApp), completionDisplaySeconds);
        }
    }

    private void HideOtherCanvasUIs()
    {
        _hiddenCanvases.Clear();

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (fixationCrossRoot != null && (c.gameObject == fixationCrossRoot || c.transform.IsChildOf(fixationCrossRoot.transform)))
                continue;

            if (c.isActiveAndEnabled)
            {
                c.enabled = false;
                _hiddenCanvases.Add(c);
            }
        }
    }

    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
