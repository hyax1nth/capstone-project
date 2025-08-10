using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class LocalCacheService
{
    private static LocalCacheService _instance;
    public static LocalCacheService Instance => _instance ??= new LocalCacheService();

    private readonly string _cacheFilePath;
    private readonly Queue<string> _analyticsQueue;
    private const int MAX_QUEUE_SIZE = 1000;

    private LocalCacheService()
    {
        _cacheFilePath = Path.Combine(Application.persistentDataPath, "analytics.jsonl");
        _analyticsQueue = new Queue<string>();
        LoadCache();
    }

    public async Task EnqueueAnalyticEvent(string eventName, Dictionary<string, object> parameters)
    {
        try
        {
            if (_analyticsQueue.Count >= MAX_QUEUE_SIZE)
            {
                _analyticsQueue.Dequeue(); // Remove oldest event if queue is full
            }

            var eventData = new Dictionary<string, object>
            {
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["event"] = eventName,
                ["parameters"] = parameters
            };

            string json = JsonUtility.ToJson(eventData);
            _analyticsQueue.Enqueue(json);

            // Non-blocking save
            await Task.Run(() => SaveCache());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error enqueueing analytic event: {ex.Message}");
        }
    }

    private void LoadCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath)) return;

            string[] lines = File.ReadAllLines(_cacheFilePath);
            foreach (string line in lines)
            {
                if (_analyticsQueue.Count < MAX_QUEUE_SIZE)
                {
                    _analyticsQueue.Enqueue(line);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading cache: {ex.Message}");
        }
    }

    private void SaveCache()
    {
        try
        {
            File.WriteAllLines(_cacheFilePath, _analyticsQueue);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving cache: {ex.Message}");
        }
    }
}
