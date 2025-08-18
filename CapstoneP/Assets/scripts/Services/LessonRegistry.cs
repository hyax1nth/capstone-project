using System.Collections.Generic;

/// <summary>
/// Simple registry to pass named parameters into lesson scenes. Uses a string-keyed dictionary.
/// </summary>
public static class LessonRegistry
{
    private static Dictionary<string, string> values = new Dictionary<string, string>();

    public static void Set(string key, string value)
    {
        values[key] = value;
    }

    public static string Get(string key, string fallback = null)
    {
        return values.TryGetValue(key, out var v) ? v : fallback;
    }

    public static void Clear()
    {
        values.Clear();
    }
}
