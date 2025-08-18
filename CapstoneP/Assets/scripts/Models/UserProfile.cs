using System;
using System.Collections.Generic;

[Serializable]
public class UserProfile
{
    public string displayName;
    public string displayNameLower;
    public int age;
    public string gender; // "male", "female", "unspecified"
    public string avatar; // key of avatar image
    public List<string> preferredSubjects = new List<string>();
    public string role; // "student" or "admin"
    public long createdAt;
    public long lastLoginAt;
}
