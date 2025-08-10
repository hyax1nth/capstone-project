using UnityEngine;
using System;

public static class BeeCoolLogger
{
    private const string TAG = "[BeeCool]";
    private static bool logsEnabled = true;

    public static void Log(string message)
    {
        if (!logsEnabled) return;
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.Log($"{TAG} [{timestamp}] {message}");
    }

    public static void LogError(string message)
    {
        if (!logsEnabled) return;
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.LogError($"{TAG} [{timestamp}] {message}");
    }

    public static void LogWarning(string message)
    {
        if (!logsEnabled) return;
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.LogWarning($"{TAG} [{timestamp}] {message}");
    }

    public static void EnableLogs(bool enable)
    {
        logsEnabled = enable;
    }
}
