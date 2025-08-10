using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CatalogService
{
    private static CatalogService _instance;
    public static CatalogService Instance => _instance ??= new CatalogService();

    private readonly RealtimeDBService _db;
    private readonly string _catalogPath;

    private CatalogService()
    {
        _db = RealtimeDBService.Instance;
        _catalogPath = Constants.DatabasePaths.Catalog;
    }

    public async Task<Dictionary<string, int>> GetSubjectLessonCounts()
    {
        try
        {
            var catalog = await _db.GetAsync<Dictionary<string, Dictionary<string, object>>>(_catalogPath);
            var counts = new Dictionary<string, int>();

            if (catalog != null)
            {
                foreach (var subject in catalog)
                {
                    if (subject.Value.TryGetValue("lessonCount", out object count))
                    {
                        counts[subject.Key] = Convert.ToInt32(count);
                    }
                }
            }

            return counts;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting subject lesson counts: {ex.Message}");
            throw;
        }
    }

    public async Task EnsureDefaultCatalog()
    {
        try
        {
            var catalog = await _db.GetAsync<Dictionary<string, object>>(_catalogPath);
            if (catalog != null && catalog.Count > 0) return;

            var defaultCatalog = new Dictionary<string, object>();
            foreach (string subject in Constants.DefaultSubjects)
            {
                defaultCatalog[subject.ToLower()] = new Dictionary<string, object>
                {
                    ["name"] = subject,
                    ["lessonCount"] = Constants.LessonsPerSubject
                };
            }

            await _db.SetAsync(_catalogPath, defaultCatalog);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error ensuring default catalog: {ex.Message}");
            throw;
        }
    }
}
