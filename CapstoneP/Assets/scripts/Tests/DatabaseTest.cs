using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DatabaseTest : MonoBehaviour
{
    [System.Serializable]
    public class Subject
    {
        public string name;
        public int lessonCount;
    }

    [System.Serializable]
    public class Catalog
    {
        public Dictionary<string, Subject> subjects;
    }

    async void Start()
    {
        Debug.Log("Starting database test...");
        await Task.Delay(2000); // Wait for Firebase to fully initialize
        
        try
        {
            Debug.Log("Checking if database is initialized...");
            if (RealtimeDBService.Instance == null)
            {
                Debug.LogError("RealtimeDBService instance is null!");
                return;
            }

            Debug.Log("Attempting to read catalog data...");
            // Try to read the entire catalog
            var catalog = await RealtimeDBService.Instance.GetAsync<Dictionary<string, Subject>>("catalog");
            
            if (catalog != null)
            {
                Debug.Log("Successfully retrieved catalog data!");
                foreach (var subject in catalog)
                {
                    Debug.Log($"Subject: {subject.Key}");
                    Debug.Log($"  Name: {subject.Value.name}");
                    Debug.Log($"  Lesson Count: {subject.Value.lessonCount}");
                }
            }
            else
            {
                Debug.LogError("Catalog data is null! This might mean the path doesn't exist in the database.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading from database: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            if (e.InnerException != null)
            {
                Debug.LogError($"Inner exception: {e.InnerException.Message}");
                Debug.LogError($"Inner exception stack trace: {e.InnerException.StackTrace}");
            }
        }
    }
}
