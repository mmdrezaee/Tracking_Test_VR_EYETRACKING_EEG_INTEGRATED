using System;
using System.IO;
using UnityEngine;

public static class ExperimentPaths
{
    public static string SessionDirectory { get; private set; } = "";

    // If true, builds will try to write next to the exe: <BuildFolder>/ExperimentOutputs
    // If false, builds write to Application.persistentDataPath/ExperimentOutputs
    public static bool UseBuildFolderInBuild = true;

    public static void InitSession(string participantId, string conditionName)
    {
        string baseDir = GetBaseOutputDir();

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string safeP = Sanitize(participantId);
        string safeC = Sanitize(conditionName);

        string sessionName = $"{stamp}_{safeP}_{safeC}";
        SessionDirectory = Path.Combine(baseDir, sessionName);

        Directory.CreateDirectory(SessionDirectory);
        Debug.Log("[ExperimentPaths] SessionDirectory: " + SessionDirectory);
    }

    public static string PathInSession(string fileName)
    {
        if (string.IsNullOrEmpty(SessionDirectory))
            InitSession("P00", "ConditionA");

        return Path.Combine(SessionDirectory, fileName);
    }

    public static void WriteAllText(string path, string content)
    {
        EnsureDirForFile(path);
        File.WriteAllText(path, content);
    }

    public static void AppendLine(string path, string line)
    {
        EnsureDirForFile(path);
        File.AppendAllText(path, line + "\n");
    }

    private static void EnsureDirForFile(string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    private static string GetBaseOutputDir()
    {
#if UNITY_EDITOR
        // Project root / ExperimentOutputs
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string dir = Path.Combine(projectRoot, "ExperimentOutputs");
        Directory.CreateDirectory(dir);
        return dir;
#else
        // Build mode
        // Option A: next to the exe (what you want)
        if (UseBuildFolderInBuild)
        {
            // Application.dataPath -> <BuildFolder>/<GameName>_Data
            // Parent -> <BuildFolder>
            string buildRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string dir = Path.Combine(buildRoot, "ExperimentOutputs");

            try
            {
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ExperimentPaths] Could not write to build folder. Falling back to persistentDataPath. " + e.Message);
            }
        }

        // Option B fallback: always-writable location
        string fallbackDir = Path.Combine(Application.persistentDataPath, "ExperimentOutputs");
        Directory.CreateDirectory(fallbackDir);
        return fallbackDir;
#endif
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "NA";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), "_");
        return s;
    }
}
