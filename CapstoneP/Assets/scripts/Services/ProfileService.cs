using System;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

/// <summary>
/// Reads/writes user profile under /users/{uid}/profile
/// Call SaveOnboarding for student onboarding.
/// </summary>
public class ProfileService : MonoBehaviour
{
    private DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;

    public Task SaveOnboarding(string uid, UserProfile profile)
    {
        if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));

        profile.displayNameLower = profile.displayName?.ToLowerInvariant();
        profile.createdAt = profile.createdAt == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : profile.createdAt;
        profile.lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string path = $"users/{uid}/profile";

        // Save profile, then initialize progress defaults for selected subjects (unlock L1)
        var json = JsonUtility.ToJson(profile);
        var task = Root.Child(path).SetRawJsonValueAsync(json);

        // If student, initialize first lesson unlocked for chosen subjects and basic stats
        if (profile.role == "student")
        {
            task.ContinueWith(_ =>
            {
                try
                {
                    foreach (var subj in profile.preferredSubjects)
                    {
                        var unlockRef = Root.Child($"progress/{uid}/{subj}/L1/unlocked");
                        unlockRef.SetValueAsync(true);
                    }

                    // Initialize empty stats node
                    var statsRef = Root.Child($"users/{uid}/stats");
                    var emptyStats = new UserStats();
                    statsRef.SetRawJsonValueAsync(JsonUtility.ToJson(emptyStats));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("ProfileService: failed to init progress/stats: " + ex.Message);
                }
            });
        }

        return task;
    }

    public Task<DataSnapshot> LoadProfile(string uid)
    {
        if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));
        return Root.Child($"users/{uid}/profile").GetValueAsync();
    }

    /// <summary>
    /// Set the 'role' field for a user's profile (e.g., "student" or "admin").
    /// This is a low-risk helper to promote a user to admin for testing.
    /// </summary>
    public Task SetRoleAsync(string uid, string role)
    {
        if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));
        if (string.IsNullOrEmpty(role)) throw new ArgumentNullException(nameof(role));
        return Root.Child($"users/{uid}/profile/role").SetValueAsync(role);
    }

    /// <summary>
    /// Try to read the role for a user from common locations and return it or null.
    /// </summary>
    public async System.Threading.Tasks.Task<string> GetRoleAsync(string uid)
    {
        if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));

        // First try /users/{uid}/profile/role
        var snap = await Root.Child($"users/{uid}/profile/role").GetValueAsync();
        if (snap.Exists && snap.Value != null)
        {
            return snap.Value.ToString();
        }

        // Next try /users/{uid}/role (older layouts)
        var snap2 = await Root.Child($"users/{uid}/role").GetValueAsync();
        if (snap2.Exists && snap2.Value != null)
        {
            return snap2.Value.ToString();
        }

        return null;
    }
}
