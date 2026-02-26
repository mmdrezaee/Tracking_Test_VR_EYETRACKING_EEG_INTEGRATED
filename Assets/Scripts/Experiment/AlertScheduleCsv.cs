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
    // - startSec, direction
    // - alertNumber, startSec, direction
    // - Any wider format that has a header row containing
    //   "start" and "direction" (and optionally "alert")
    //   somewhere in the column names. Extra columns are ignored.
    public static List<Trial> Parse(string csvText)
    {
        var trials = new List<Trial>();
        if (string.IsNullOrWhiteSpace(csvText)) return trials;

        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        // Optional header detection (supports 3+ columns, e.g. 5-column files in the new Experiment folder).
        int headerLineIndex = -1;
        int alertCol = -1;
        int startCol = -1;
        int dirCol = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            string rawHeader = lines[i];
            if (string.IsNullOrWhiteSpace(rawHeader)) continue;

            string lineHeader = rawHeader.Trim();
            if (lineHeader.StartsWith("#")) continue;

            string[] headerParts = lineHeader.Split(',');
            if (headerParts.Length < 2) continue;

            // If the first one or two columns contain letters, we treat this as a header line.
            bool firstHasLetter = HasLetter(headerParts[0]);
            bool secondHasLetter = headerParts.Length > 1 && HasLetter(headerParts[1]);
            if (!firstHasLetter && !secondHasLetter)
            {
                // First non-comment, non-empty line looks numeric -> assume there's no header.
                break;
            }

            headerLineIndex = i;

            for (int c = 0; c < headerParts.Length; c++)
            {
                string name = headerParts[c].Trim().ToLower();
                if (name.Contains("alert"))
                    alertCol = c;
                if (name.Contains("start"))
                    startCol = c;
                if (name.Contains("start_time") || name.Contains("time_sec"))
                    startCol = c;
                if (name.Contains("direction") || name == "dir")
                    dirCol = c;
            }

            break;
        }

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string line = raw.Trim();
            if (line.StartsWith("#")) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 2) continue;

            // Skip the header line we detected above, if any.
            if (headerLineIndex >= 0 && lines[headerLineIndex].Trim() == line)
                continue;

            bool parsed = false;

            // Case 1: header-based parsing (supports 3+ columns, arbitrary order).
            if (headerLineIndex >= 0 && startCol >= 0 && dirCol >= 0)
            {
                if (startCol >= parts.Length || dirCol >= parts.Length)
                    continue;

                string startStr = parts[startCol];
                string dirStr = parts[dirCol].Trim();

                if (!TryParseFloat(startStr, out float startSec))
                    continue;

                int alertNumber = 0;
                if (alertCol >= 0 && alertCol < parts.Length)
                    int.TryParse(parts[alertCol].Trim(), out alertNumber);

                trials.Add(new Trial
                {
                    alertNumber = alertNumber,
                    startSeconds = startSec,
                    correctDirection = dirStr
                });

                parsed = true;
            }

            if (!parsed)
            {
                // Case 2: legacy positional parsing (2 or 3 columns).
                // Skip header-like rows if they contain non-numeric start time in the first column.
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
                    // alertNumber, startSec, direction (extra columns ignored)
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

    private static bool HasLetter(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsLetter(s[i])) return true;
        }
        return false;
    }
}
