Unity Firebase wiring README

Files added:
- Models/UserProfile.cs, Models/UserStats.cs
- Firebase/FirebaseInitializer.cs
- Services/AuthManager.cs, ProfileService.cs, ProgressService.cs
- UI/LessonTester.cs, StudentDashboard.cs, AdminDashboard.cs, LoginUIManager.cs

Quick hookup:
1. Import Firebase Unity SDK (Auth + Database) via Unity Package Manager or the provided .unitypackage.
2. Add a GameObject named "FirebaseBootstrap" in the first scene and attach `FirebaseInitializer`.
3. Create an `AuthManager` GameObject and attach `AuthManager`.
4. Add GameObjects for `ProfileService` and `ProgressService` and attach respective scripts.
5. For Student and Admin scenes, create UI canvases and wire fields in `StudentDashboard` and `AdminDashboard` via inspector.
6. For `LessonTester`, create a small panel with Dropdowns and InputFields and assign references.

Notes:
- Ensure Firebase Realtime Database rules restrict writes to student's own progress and allow admin reads.
- The code assumes simple JSON serialization and does not include advanced idempotency. For production, reconcile stars and completedLessons updates on the server.

Login / SignUp scene wiring
1. Create a scene named `Login` (make it the first scene in Build Settings).
2. Create UI panels under a Canvas:
   - MainMenuPanel with two buttons: Sign In (call LoginUIManager.OpenSignIn) and Sign Up (call LoginUIManager.OpenSignUp).
   - SignInPanel with InputFields signInEmail, signInPassword, a Submit button (wired to LoginUIManager.OnSignInClicked) and signInErrorText Text.
   - SignUpPanel with InputFields signUpDisplayName, signUpEmail, signUpPassword, a Dropdown signUpRoleDropdown (values: Student, Admin) and Next button (wired to LoginUIManager.OnSignUpClicked) and signUpErrorText.
   - OnboardingAgeGenderPanel with Dropdowns ageDropdown (3..10), genderDropdown (male/female/unspecified), avatarDropdown (four avatar keys) and Next button (wired to LoginUIManager.OnAgeGenderNext).
   - OnboardingSubjectsPanel with Toggles for the four subjects (Alphabet, Numbers, Reading, Match) and Submit button (wired to LoginUIManager.OnSubjectsSubmit).
3. Add LoginUIManager to a persistent GameObject, assign all panel and field references in the inspector, and assign AuthManager and ProfileService instances.
4. Scenes to load after auth: StudentDashboard and AdminDashboard (create these scenes and name them exactly as referenced in the script).

Behavior summary:
- Main menu lets user choose Sign In or Sign Up.
- Sign Up creates a user, then if role == "student" shows age/gender/avatar then subject preferences, saves profile and navigates to StudentDashboard. If role == "admin" saves profile and navigates to AdminDashboard.
- Sign In authenticates, loads profile and routes to the proper dashboard based on role. If profile missing, defaults to student onboarding.

Notes on wiring and testing
- Ensure `FirebaseInitializer` runs before any DatabaseReference usage.
- Create simple UI prefabs for admin search result rows and progress rows and assign them to AdminDashboard.
- Map avatar string keys to sprites in your StudentDashboard UI code when displaying avatars.

If you want, I can now create example UI prefabs and a sample `Login` scene (basic layout) in the repository so you can open the scene in the Unity Editor and test the flow quickly.

Sample lesson helper (automates creating a test lesson scene)
1. In the Unity Editor, open the menu: Tools → Bootcamp → Create Sample Lesson Scene and LessonLoader.
2. This creates `Assets/Scenes/Lesson_Alphabet_L1.unity` and a `LessonLoader` prefab under `Assets/Prefabs`.
3. Open the sample scene, add a `LessonTemplate` component to the LessonManager GameObject (if missing), wire the Finish button, and add the scene to Build Settings.

Student dashboard layout reminder
- StudentDashboard expects 4 `SubjectPanel` entries (Alphabet, Numbers, Reading, Match).
- Each `SubjectPanel` has a `lessonsPanel` GameObject with 5 lesson buttons (L1..L5). Assign them in the inspector.
- Lesson buttons are automatically wired to open `Lesson_{subject}_{lessonId}` additively. Create these scenes and add them to Build Settings.
