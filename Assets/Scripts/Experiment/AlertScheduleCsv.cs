using System;
using System.Collections.Generic;
using System.Globalization;

public static class AlertScheduleCsv
{
    public class Trial
    {
        public int trialIndex;
        public int alertNumber;
        public float startSeconds;
        public string correctDirection;
    }

    // Accepts either:
    // startSec, direction
    // alertNumber, startSec, direction
    public static List<Trial> Parse(string csvText)
    {
        var trials = new List<Trial>();
        if (string.IsNullOrWhiteSpace(csvText)) return trials;

        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string line = raw.Trim();
            if (line.StartsWith("#")) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 2) continue;

            // Skip header rows if they contain non-numeric start time
            bool looksHeader = parts[0].Trim().ToLower().Contains("start") ||
                               parts[0].Trim().ToLower().Contains("alert");
            if (looksHeader) continue;

            if (parts.Length == 2)
            {
                // startSec, direction
                if (!TryParseFloat(parts[0], out float startSec)) continue;
                string dir = parts[1].Trim();

                trials.Add(new Trial
                {
                    startSeconds = startSec,
                    correctDirection = dir
                });
            }
            else
            {
                // alertNumber, startSec, direction
                if (!int.TryParse(parts[0].Trim(), out int alertNumber)) continue;
                if (!TryParseFloat(parts[1], out float startSec)) continue;
                string dir = parts[2].Trim();

                trials.Add(new Trial
                {
                    alertNumber = alertNumber,
                    startSeconds = startSec,
                    correctDirection = dir
                });
            }
        }

        // Sort by time, then reindex and ensure alertNumber exists
        trials.Sort((a, b) => a.startSeconds.CompareTo(b.startSeconds));

        for (int i = 0; i < trials.Count; i++)
        {
            trials[i].trialIndex = i;
            if (trials[i].alertNumber == 0)
                trials[i].alertNumber = i + 1;
        }

        return trials;
    }

    private static bool TryParseFloat(string s, out float v)
    {
        return float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }
}
