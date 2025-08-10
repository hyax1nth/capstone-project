using System;

[Serializable]
public class UserProfile
{
    public string displayName;
    public string displayNameLower;
    public int age;
    public string[] preferredSubjects;
    public string role;
    public long createdAt;

    public UserProfile()
    {
        createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        role = "student"; // Default role
    }

    public bool IsProfileComplete()
    {
        return age >= Constants.MinAge && 
               age <= Constants.MaxAge && 
               preferredSubjects != null && 
               preferredSubjects.Length > 0;
    }
}
