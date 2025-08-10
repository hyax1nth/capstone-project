using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class RealtimeDBService
{
    private static RealtimeDBService _instance;
    public static RealtimeDBService Instance => _instance ??= new RealtimeDBService();

    private DatabaseReference _database;
    private bool _isInitialized;

    private RealtimeDBService()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            if (FirebaseDatabase.DefaultInstance == null)
            {
                Debug.LogError("Firebase Database not initialized. Make sure Firebase is properly set up.");
                return;
            }

            _database = FirebaseDatabase.DefaultInstance.RootReference;
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
            _isInitialized = true;
            Debug.Log("Firebase Database initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing Firebase Database: {ex.Message}");
            _isInitialized = false;
        }
    }

    public async Task<T> GetAsync<T>(string path) where T : class
    {
        if (!_isInitialized)
        {
            Debug.LogError("Database not initialized. Reinitializing...");
            InitializeDatabase();
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Database could not be initialized");
            }
        }

        try
        {
            var snapshot = await _database.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error getting data: {task.Exception?.Message}");
                    throw task.Exception;
                }
                return task.Result;
            });

            if (!snapshot.Exists) return null;

            string json = snapshot.GetRawJsonValue();
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting data at {path}: {ex.Message}");
            throw;
        }
    }

    public async Task SetAsync<T>(string path, T data) where T : class
    {
        try
        {
            string json = JsonConvert.SerializeObject(data);
            await _database.Child(path).SetRawJsonValueAsync(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error setting data at {path}: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateChildrenAsync(string path, object data)
    {
        try
        {
            string json = JsonConvert.SerializeObject(data);
            var updates = new Dictionary<string, object>();
            var deserializedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            
            foreach (var kvp in deserializedData)
            {
                updates[$"{path}/{kvp.Key}"] = kvp.Value;
            }

            await _database.UpdateChildrenAsync(updates);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating children at {path}: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string path)
    {
        try
        {
            await _database.Child(path).RemoveValueAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deleting data at {path}: {ex.Message}");
            throw;
        }
    }

    public Query OrderByChild(string path, string childKey)
    {
        return _database.Child(path).OrderByChild(childKey);
    }
}
