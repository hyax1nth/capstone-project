using System;
using System.Threading.Tasks;
using UnityEngine;

public class UserProfileService
{
    private static UserProfileService _instance;
    public static UserProfileService Instance => _instance ??= new UserProfileService();

    private readonly RealtimeDBService _db;
    private readonly string _usersPath;

    private UserProfileService()
    {
        _db = RealtimeDBService.Instance;
        _usersPath = Constants.DatabasePaths.Users;
    }

    public async Task<UserProfile> GetProfile(string userId)
    {
        try
        {
            return await _db.GetAsync<UserProfile>($"{_usersPath}/{userId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting user profile: {ex.Message}");
            throw;
        }
    }

    public async Task SaveProfile(string userId, UserProfile profile)
    {
        try
        {
            // Ensure displayNameLower is set
            profile.displayNameLower = profile.displayName?.ToLower() ?? "";
            
            // DEV flag for admin role (should be false in production)
            const bool DEV_MODE = false;
            if (DEV_MODE && profile.displayNameLower == "admin")
            {
                profile.role = "admin";
            }

            await _db.SetAsync($"{_usersPath}/{userId}", profile);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving user profile: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetRole(string userId)
    {
        try
        {
            var profile = await GetProfile(userId);
            return profile?.role ?? "student";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting user role: {ex.Message}");
            return "student"; // Default to student role on error
        }
    }

    public async Task<bool> EnsureProfileCompleteness(string userId)
    {
        try
        {
            var profile = await GetProfile(userId);
            if (profile == null) return false;

            return profile.IsProfileComplete();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking profile completeness: {ex.Message}");
            return false;
        }
    }
}
