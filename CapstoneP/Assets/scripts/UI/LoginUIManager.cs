using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages Login / SignUp flow and student onboarding pages.
/// Attach to a persistent Login UI GameObject and wire panels/fields in the inspector.
/// Scenes to load after auth: "StudentDashboard" and "AdminDashboard" (create these scenes).
/// </summary>
public class LoginUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel; // contains Sign In / Sign Up buttons
    public GameObject signInPanel;
    public GameObject signUpPanel;
    public GameObject onboardingAgeGenderPanel; // age, gender, avatar
    public GameObject onboardingSubjectsPanel; // preferredSubjects selection
    public Button mainMenuSignInButton;
    public Button mainMenuSignUpButton;

    [Header("Sign In Fields")]
    public TMP_InputField signInEmail;
    public TMP_InputField signInPassword;
    public Button signInSubmitButton;
    public TMP_Text signInErrorText;
    public Button signInBackButton;

    [Header("Sign Up Fields")]
    public TMP_InputField signUpEmail;
    public TMP_InputField signUpPassword;
    public TMP_InputField signUpDisplayName;
    public Button signUpNextButton;
    public TMP_Text signUpErrorText;
    public Button signUpBackButton;
    // NOTE: Admins must be created in Firebase Console. Role selection is not exposed in UI.

    [Header("Onboarding - Age/Gender/Avatar")]
    // Age buttons: 7 buttons representing ages 1..7 (assign in inspector)
    public List<Button> ageButtons;
    // Gender selection: two buttons
    public Button genderMaleButton;
    public Button genderFemaleButton;
    // Avatar selection buttons (4)
    public List<Button> avatarButtons;
    public Button ageGenderNextButton;
    public Button ageGenderBackButton;

    [Header("Onboarding - Preferred Subjects")]
    public Toggle alphabetToggle;
    public Toggle numbersToggle;
    public Toggle readingToggle;
    public Toggle matchToggle;
    public Button subjectsSubmitButton;
    public Button subjectsBackButton;

    [Header("Services")]
    public AuthManager authManager;
    public ProfileService profileService;

    private string pendingUid;
    private UserProfile pendingProfile = new UserProfile();

    // UI selection state
    private Button selectedAgeButton = null;
    private Button selectedGenderButton = null;
    private void Start()
    {
        Debug.Log("LoginUIManager: Starting...");
        
        // Show main menu panel by default
        if (mainMenuPanel != null)
        {
            ShowPanel(mainMenuPanel);
        }
        else
        {
            Debug.LogError("LoginUIManager: mainMenuPanel is not assigned in the inspector!");
        }
        
        // Auto-find services if not wired in inspector
        if (authManager == null)
        {
            authManager = FindAnyObjectByType<AuthManager>();
            if (authManager != null) Debug.Log("LoginUIManager: found AuthManager automatically.");
        }
        if (profileService == null)
        {
            profileService = FindAnyObjectByType<ProfileService>();
            if (profileService != null) Debug.Log("LoginUIManager: found ProfileService automatically.");
        }
        // Wire button callbacks (if assigned in inspector)
        if (signInSubmitButton != null) signInSubmitButton.onClick.AddListener(() => OnSignInClicked());
        if (signInBackButton != null) signInBackButton.onClick.AddListener(BackFromSignIn);
        if (signUpNextButton != null) signUpNextButton.onClick.AddListener(() => OnSignUpClicked());
        if (signUpBackButton != null) signUpBackButton.onClick.AddListener(BackFromSignUp);
        if (ageGenderNextButton != null) ageGenderNextButton.onClick.AddListener(OnAgeGenderNext);
        if (ageGenderBackButton != null) ageGenderBackButton.onClick.AddListener(BackFromAgeGender);
        if (subjectsSubmitButton != null) subjectsSubmitButton.onClick.AddListener(() => OnSubjectsSubmit());
        if (subjectsBackButton != null) subjectsBackButton.onClick.AddListener(BackFromSubjects);

        // Wire main menu buttons to open panels
        if (mainMenuSignInButton != null) mainMenuSignInButton.onClick.AddListener(OpenSignIn);
        if (mainMenuSignUpButton != null) mainMenuSignUpButton.onClick.AddListener(OpenSignUp);

        // Wire age/gender/avatar selection buttons (if assigned)
        if (ageButtons != null)
        {
            for (int i = 0; i < ageButtons.Count; i++)
            {
                int age = i + 1; // ages 1..7
                var btn = ageButtons[i];
                if (btn != null)
                {
                    int capturedAge = age;
                    Button capturedBtn = btn;
                    capturedBtn.onClick.AddListener(() => SelectAge(capturedAge, capturedBtn));
                    try { var rt = capturedBtn.GetComponent<RectTransform>(); if (rt != null) originalY[capturedBtn] = rt.anchoredPosition.y; } catch {}
                }
            }
        }

        if (genderMaleButton != null)
        {
            var b = genderMaleButton;
            b.onClick.AddListener(() => SelectGender("male", b));
            try { originalY[b] = b.GetComponent<RectTransform>().anchoredPosition.y; } catch {}
        }
        if (genderFemaleButton != null)
        {
            var b = genderFemaleButton;
            b.onClick.AddListener(() => SelectGender("female", b));
            try { originalY[b] = b.GetComponent<RectTransform>().anchoredPosition.y; } catch {}
        }

        if (avatarButtons != null)
        {
            for (int i = 0; i < avatarButtons.Count; i++)
            {
                int idx = i;
                var btn = avatarButtons[i];
                if (btn != null)
                {
                    Button capturedBtn = btn;
                    int capturedIdx = idx;
                    capturedBtn.onClick.AddListener(() => SelectAvatarIndex(capturedIdx, capturedBtn));
                    try { var rt = capturedBtn.GetComponent<RectTransform>(); if (rt != null) originalY[capturedBtn] = rt.anchoredPosition.y; } catch {}
                }
            }
        }

        // Ensure Next button disabled until selections made
        if (ageGenderNextButton != null) ageGenderNextButton.interactable = false;

        // Ensure UI panels are in a consistent initial state: show main menu only
        InitializePanels();
        // Validate inspector wiring
        ValidateInspectorBindings();

        ShowPanel(mainMenuPanel);
    }

    // Age/Gender/Avatar selection handlers (called by buttons)
    private int selectedAge = 1;
    private string selectedGender = "male";
    private int selectedAvatarIndex = -1;
    private Button selectedAvatarButton = null;
    // store original anchored Y positions so offsets restore correctly
    private System.Collections.Generic.Dictionary<Button, float> originalY = new System.Collections.Generic.Dictionary<Button, float>();

    private void InitializePanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (signInPanel != null) signInPanel.SetActive(false);
        if (signUpPanel != null) signUpPanel.SetActive(false);
        if (onboardingAgeGenderPanel != null) onboardingAgeGenderPanel.SetActive(false);
        if (onboardingSubjectsPanel != null) onboardingSubjectsPanel.SetActive(false);
    }

    private void ValidateInspectorBindings()
    {
        // Warn if required panels/buttons are not assigned so the user can wire them in the inspector
        if (mainMenuPanel == null) Debug.LogWarning("LoginUIManager: mainMenuPanel is not assigned in the inspector.");
        if (signInPanel == null) Debug.LogWarning("LoginUIManager: signInPanel is not assigned in the inspector.");
        if (signUpPanel == null) Debug.LogWarning("LoginUIManager: signUpPanel is not assigned in the inspector.");
        if (mainMenuSignInButton == null) Debug.LogWarning("LoginUIManager: mainMenuSignInButton not assigned.");
        if (mainMenuSignUpButton == null) Debug.LogWarning("LoginUIManager: mainMenuSignUpButton not assigned.");
        if (signInSubmitButton == null) Debug.LogWarning("LoginUIManager: signInSubmitButton not assigned.");
        if (signUpNextButton == null) Debug.LogWarning("LoginUIManager: signUpNextButton not assigned.");
    }

    public void SelectAge(int age)
    {
        selectedAge = age;
        // Optional: update UI state (highlight selected button) - left for inspector wiring
        Debug.Log($"Selected age: {age}");
    }

    public void SelectAge(int age, Button btn)
    {
        selectedAge = age;
        // Visual offset: restore previous selected age button position and offset the new one
        if (selectedAgeButton != null)
        {
            try
            {
                var animPrev = selectedAgeButton.GetComponent<ButtonPressAnimation>();
                if (animPrev != null)
                {
                    animPrev.SetSelected(false);
                }
                else
                {
                    var rtPrev = selectedAgeButton.GetComponent<RectTransform>();
                    if (rtPrev != null && originalY.ContainsKey(selectedAgeButton))
                        rtPrev.anchoredPosition = new Vector2(rtPrev.anchoredPosition.x, originalY[selectedAgeButton]);
                }
            }
            catch { }
        }
        selectedAgeButton = btn;
        if (selectedAgeButton != null)
        {
            try
            {
                var animNew = selectedAgeButton.GetComponent<ButtonPressAnimation>();
                if (animNew != null)
                {
                    animNew.SetSelected(true);
                }
                else
                {
                    var rtNew = selectedAgeButton.GetComponent<RectTransform>();
                    if (rtNew != null)
                    {
                        float baseY = originalY.ContainsKey(selectedAgeButton) ? originalY[selectedAgeButton] : rtNew.anchoredPosition.y;
                        rtNew.anchoredPosition = new Vector2(rtNew.anchoredPosition.x, baseY - 5f);
                    }
                }
            }
            catch { }
        }
        UpdateAgeGenderNextInteractable();
        Debug.Log($"Selected age: {age}");
    }

    public void SelectGender(string gender)
    {
        selectedGender = gender;
        Debug.Log($"Selected gender: {gender}");
    }

    public void SelectGender(string gender, Button btn)
    {
        selectedGender = gender;
        if (selectedGenderButton != null)
        {
            try
            {
                var animPrev = selectedGenderButton.GetComponent<ButtonPressAnimation>();
                if (animPrev != null)
                {
                    animPrev.SetSelected(false);
                }
                else
                {
                    var rtPrev = selectedGenderButton.GetComponent<RectTransform>();
                    if (rtPrev != null && originalY.ContainsKey(selectedGenderButton))
                        rtPrev.anchoredPosition = new Vector2(rtPrev.anchoredPosition.x, originalY[selectedGenderButton]);
                }
            }
            catch { }
        }
        selectedGenderButton = btn;
        if (selectedGenderButton != null)
        {
            try
            {
                var animNew = selectedGenderButton.GetComponent<ButtonPressAnimation>();
                if (animNew != null)
                {
                    animNew.SetSelected(true);
                }
                else
                {
                    var rtNew = selectedGenderButton.GetComponent<RectTransform>();
                    if (rtNew != null)
                    {
                        float baseY = originalY.ContainsKey(selectedGenderButton) ? originalY[selectedGenderButton] : rtNew.anchoredPosition.y;
                        rtNew.anchoredPosition = new Vector2(rtNew.anchoredPosition.x, baseY - 5f);
                    }
                }
            }
            catch { }
        }
        UpdateAgeGenderNextInteractable();
        Debug.Log($"Selected gender: {gender}");
    }

    private void UpdateAgeGenderNextInteractable()
    {
        if (ageGenderNextButton != null)
        {
            bool ok = selectedAgeButton != null && selectedGenderButton != null && selectedAvatarIndex >= 0;
            ageGenderNextButton.interactable = ok;
        }
    }

    public void SelectAvatarIndex(int index)
    {
        selectedAvatarIndex = index;
        Debug.Log($"Selected avatar index: {index}");
    }

    // overload used by avatar button wiring to provide visual feedback
    public void SelectAvatarIndex(int index, Button btn)
    {
        SelectAvatarIndex(index);
        // restore previous avatar button position
        if (selectedAvatarButton != null)
        {
            try
            {
                var animPrev = selectedAvatarButton.GetComponent<ButtonPressAnimation>();
                if (animPrev != null)
                {
                    animPrev.SetSelected(false);
                }
                else
                {
                    var rtPrev = selectedAvatarButton.GetComponent<RectTransform>();
                    if (rtPrev != null && originalY.ContainsKey(selectedAvatarButton))
                        rtPrev.anchoredPosition = new Vector2(rtPrev.anchoredPosition.x, originalY[selectedAvatarButton]);
                }
            }
            catch { }
        }
        selectedAvatarButton = btn;
        if (selectedAvatarButton != null)
        {
            try
            {
                var animNew = selectedAvatarButton.GetComponent<ButtonPressAnimation>();
                if (animNew != null)
                {
                    animNew.SetSelected(true);
                }
                else
                {
                    var rtNew = selectedAvatarButton.GetComponent<RectTransform>();
                    if (rtNew != null)
                    {
                        float baseY = originalY.ContainsKey(selectedAvatarButton) ? originalY[selectedAvatarButton] : rtNew.anchoredPosition.y;
                        rtNew.anchoredPosition = new Vector2(rtNew.anchoredPosition.x, baseY - 5f);
                    }
                }
            }
            catch { }
        }
        UpdateAgeGenderNextInteractable();
    }

    private void ShowPanel(GameObject panel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (signInPanel != null) signInPanel.SetActive(panel == signInPanel);
        if (signUpPanel != null) signUpPanel.SetActive(panel == signUpPanel);
        if (onboardingAgeGenderPanel != null) onboardingAgeGenderPanel.SetActive(panel == onboardingAgeGenderPanel);
        if (onboardingSubjectsPanel != null) onboardingSubjectsPanel.SetActive(panel == onboardingSubjectsPanel);

        // When showing onboarding panels, restore any previously-entered values so Back navigation doesn't lose state
        if (panel == onboardingAgeGenderPanel) RestoreAgeGenderUI();
        if (panel == onboardingSubjectsPanel) RestoreSubjectsUI();
    }

    private void RestoreAgeGenderUI()
    {
        if (pendingProfile != null)
        {
            if (pendingProfile.age >= 1 && pendingProfile.age <= 7) selectedAge = pendingProfile.age;
            if (!string.IsNullOrEmpty(pendingProfile.gender)) selectedGender = pendingProfile.gender;
            if (!string.IsNullOrEmpty(pendingProfile.avatar) && pendingProfile.avatar.StartsWith("avatar_"))
            {
                var idxStr = pendingProfile.avatar.Substring("avatar_".Length);
                if (int.TryParse(idxStr, out int idx)) selectedAvatarIndex = idx;
            }
        }

        // Apply visual selection for age
        if (ageButtons != null && selectedAge >= 1 && selectedAge <= ageButtons.Count)
        {
            var btn = ageButtons[selectedAge - 1];
            if (btn != null) SelectAge(selectedAge, btn);
        }
        // Apply visual selection for gender
        if (!string.IsNullOrEmpty(selectedGender))
        {
            if (selectedGender == "male" && genderMaleButton != null) SelectGender("male", genderMaleButton);
            if (selectedGender == "female" && genderFemaleButton != null) SelectGender("female", genderFemaleButton);
        }
        // Apply visual selection for avatar
        if (avatarButtons != null && selectedAvatarIndex >= 0 && selectedAvatarIndex < avatarButtons.Count)
        {
            var ab = avatarButtons[selectedAvatarIndex];
            if (ab != null) SelectAvatarIndex(selectedAvatarIndex, ab);
        }

        // Optionally restore button visuals - left as debug for now
        Debug.Log($"RestoreAgeGenderUI -> age:{selectedAge} gender:{selectedGender} avatar:{selectedAvatarIndex}");
    }

    private void RestoreSubjectsUI()
    {
        if (pendingProfile?.preferredSubjects != null)
        {
            if (alphabetToggle != null) alphabetToggle.isOn = pendingProfile.preferredSubjects.Contains("Alphabet");
            if (numbersToggle != null) numbersToggle.isOn = pendingProfile.preferredSubjects.Contains("Numbers");
            if (readingToggle != null) readingToggle.isOn = pendingProfile.preferredSubjects.Contains("Reading");
            if (matchToggle != null) matchToggle.isOn = pendingProfile.preferredSubjects.Contains("Match");
        }
    }

    // Called by Sign In button on main menu
    public void OpenSignIn() => ShowPanel(signInPanel);
    // Called by Sign Up button on main menu
    public void OpenSignUp()
    {
        ShowPanel(signUpPanel);
    }

    // Public wrapper so you can assign this method to a UI Button OnClick
    public async void OnSignInClicked()
    {
        signInErrorText.text = "";
        string email = signInEmail.text.Trim();
        string password = signInPassword.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            signInErrorText.text = "Enter email and password.";
            return;
        }

        var user = await authManager.SignInWithEmail(email, password);
        if (user == null)
        {
            signInErrorText.text = "Sign in failed. Check credentials.";
            return;
        }

    // Persist signed-in uid for session continuity
    PlayerPrefs.SetString("lastSignedInUid", user.UserId);
    PlayerPrefs.Save();

        // Load profile to find role and route
        // Try to get role from profile service (supports multiple DB layouts)
        string role = null;
        try
        {
            role = await profileService.GetRoleAsync(user.UserId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to read role via ProfileService.GetRoleAsync: " + ex.Message);
        }

        if (!string.IsNullOrEmpty(role) && role == "admin")
        {
            SceneManager.LoadScene("AdminDashboard");
            return;
        }
        else if (!string.IsNullOrEmpty(role))
        {
            SceneManager.LoadScene("StudentDashboard");
            return;
        }
        else
        {
            // If no profile found, default to student and go to onboarding
            pendingUid = user.UserId;
            pendingProfile = new UserProfile
            {
                displayName = user.DisplayName ?? "",
                role = "student"
            };
            ShowPanel(onboardingAgeGenderPanel);
        }
    }

    // Public wrapper for UI Button
    public async void OnSignUpClicked()
    {
        signUpErrorText.text = "";
        string email = signUpEmail.text.Trim();
        string password = signUpPassword.text;
        string displayName = signUpDisplayName.text.Trim();
        string role = "student"; // Admins must be created in Firebase Console

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(displayName))
        {
            signUpErrorText.text = "Enter name, email and password.";
            return;
        }

        var user = await authManager.SignUpWithEmail(email, password);
        if (user == null)
        {
            signUpErrorText.text = "Sign up failed. Try another email.";
            return;
        }

        pendingUid = user.UserId;
        pendingProfile = new UserProfile
        {
            displayName = displayName,
            role = role
        };

        if (role == "student")
        {
            // proceed to age/gender onboarding
            ShowPanel(onboardingAgeGenderPanel);
        }
        else
        {
            // admin - save minimal profile and go to admin dashboard
            await profileService.SaveOnboarding(pendingUid, pendingProfile);
            SceneManager.LoadScene("AdminDashboard");
        }
    }

#if UNITY_EDITOR
    // Editor/dev helper: Promote the currently-signed-in user to admin and open AdminDashboard
    [ContextMenu("PromoteCurrentUserToAdmin")]
    public async void PromoteCurrentUserToAdmin()
    {
        var user = AuthManager.CurrentUser;
        if (user == null)
        {
            Debug.LogError("PromoteCurrentUserToAdmin: No signed-in user.");
            return;
        }

        if (profileService == null)
        {
            profileService = FindAnyObjectByType<ProfileService>();
            if (profileService == null)
            {
                Debug.LogError("PromoteCurrentUserToAdmin: ProfileService not found.");
                return;
            }
        }

        try
        {
            await profileService.SetRoleAsync(user.UserId, "admin");
            Debug.Log($"PromoteCurrentUserToAdmin: Promoted {user.UserId} to admin.");
            SceneManager.LoadScene("AdminDashboard");
        }
        catch (Exception ex)
        {
            Debug.LogError("PromoteCurrentUserToAdmin: Failed to set role: " + ex.Message);
        }
    }
#endif

    private void OnAgeGenderNext()
    {
        // collect age/gender/avatar into pendingProfile
        pendingProfile.age = selectedAge;
        pendingProfile.gender = selectedGender;
        // avatar keys: map index -> key
        pendingProfile.avatar = "avatar_" + selectedAvatarIndex;

        // Ensure both age and gender and avatar were selected
        if (selectedAgeButton == null || selectedGenderButton == null)
        {
            Debug.LogWarning("Please select age and gender before continuing.");
            return;
        }

        ShowPanel(onboardingSubjectsPanel);
    }

    // Back handlers (public so you can wire them to UI Buttons)
    public void BackFromSignIn()
    {
        ShowPanel(mainMenuPanel);
    }

    public void BackFromSignUp()
    {
        ShowPanel(mainMenuPanel);
    }

    public void BackFromAgeGender()
    {
        ShowPanel(signUpPanel);
    }

    public void BackFromSubjects()
    {
        ShowPanel(onboardingAgeGenderPanel);
    }

    // Public wrapper for UI Button
    public async void OnSubjectsSubmit()
    {
        var subjects = new List<string>();
        if (alphabetToggle.isOn) subjects.Add("Alphabet");
        if (numbersToggle.isOn) subjects.Add("Numbers");
        if (readingToggle.isOn) subjects.Add("Reading");
        if (matchToggle.isOn) subjects.Add("Match");

        if (subjects.Count == 0)
        {
            Debug.LogWarning("Please select at least one subject.");
            return;
        }

        pendingProfile.preferredSubjects = subjects;

        try
        {
            await profileService.SaveOnboarding(pendingUid, pendingProfile);
            SceneManager.LoadScene("StudentDashboard");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save onboarding: " + ex.Message);
        }
    }
}
