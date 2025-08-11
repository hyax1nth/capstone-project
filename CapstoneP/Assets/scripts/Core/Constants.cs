using UnityEngine;

public static class Constants
{
    public static readonly string[] DefaultSubjects = { "English", "Math", "Science", "Art", "Music" };
    public const int LessonsPerSubject = 5;
    public const int MinAge = 1;
    public const int MaxAge = 7;
    
    public static class Scenes
    {
        public const string AppBootstrap = "AppBootstrap";
        public const string MainMenu = "MainMenu";
        public const string SignIn = "SignIn";
        public const string SignUp = "SignUp";
        public const string OnboardingAge = "OnboardingAge";
        public const string OnboardingSubjects = "OnboardingSubjects";
        public const string StudentDashboard = "StudentDashboard";
        public const string AdminDashboard = "AdminDashboard";
        public const string StudentDetail = "StudentDetail";
        public const string LessonTap = "Lesson_Tap";
        public const string LessonDrag = "Lesson_Drag";
        public const string LessonSwipe = "Lesson_Swipe";
        public const string LessonSpeech = "Lesson_Speech";
    }

    public static class DatabasePaths
    {
        public const string Users = "users";
        public const string Progress = "progress";
        public const string Catalog = "catalog/subjects";
    }
}
