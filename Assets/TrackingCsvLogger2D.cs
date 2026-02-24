using System.Globalization;
using UnityEngine;

public class TrackingCsvLogger2D : MonoBehaviour
{
    public RayCursorToBoard2D cursorSource;
    public BouncingTarget2D targetSource;
    public TrackingStamina staminaSource;

    public SimpleExperimentVR experiment; // optional, for audio time
    public string fileName = "Tracking2D.csv";
    public int logHz = 60;

    private string _path;
    private float _nextTime;
    private bool _initialized;

    void Start()
    {
        _path = ExperimentPaths.PathInSession(fileName);

        if (!_initialized)
        {
            ExperimentPaths.WriteAllText(_path,
                "timeSec,targetXpx,targetYpx,cursorXpx,cursorYpx,errorPx,onBoard,contact,stamina\n");
            _initialized = true;
        }

        _nextTime = Time.time;
    }

    void Update()
    {
        if (cursorSource == null || targetSource == null || staminaSource == null) return;

        float interval = (logHz <= 0) ? 0.016f : (1f / logHz);
        if (Time.time < _nextTime) return;
        _nextTime = Time.time + interval;

        float t = experiment != null ? experiment.AudioTime : Time.time;

        Vector2 target = targetSource.CurrentTargetPosPx();
        Vector2 cursor = cursorSource.CursorPosPx;

        float err = Vector2.Distance(cursor, target);

        string line =
            F(t) + "," +
            F(target.x) + "," + F(target.y) + "," +
            F(cursor.x) + "," + F(cursor.y) + "," +
            F(err) + "," +
            (cursorSource.OnBoard ? "1" : "0") + "," +
            (staminaSource.IsContact ? "1" : "0") + "," +
            F(staminaSource.stamina);

        ExperimentPaths.AppendLine(_path, line);
    }

    private string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
