using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Admin dashboard controller. Minimal implementation: search users and open profile view.
/// </summary>
public class AdminDashboard : MonoBehaviour
{
    public TMP_InputField searchInput;
    public Button searchButton;
    public RectTransform resultsContainer;
    public GameObject resultRowPrefab; // prefab with Text (TMP) and ViewProfile button
    public GameObject homePanel; // the homescreen panel that contains the scrollable list + search
    public GameObject loadingSpinner; // optional spinner shown while loading data
    public TMP_Text noResultsText; // optional text shown when no users found

    public RectTransform profilePanel;
    public TMP_Text profileNameText;
    public TMP_Text profileMetaText;
    public UnityEngine.UI.Image profileAvatarImage;
    public TMP_Text profileRoleText;
    public TMP_Text profileEmailText;
    public TMP_Text profileSubjectsText;
    public TMP_Text profileTimestampsText;
    public RectTransform progressTableContainer;
    public GameObject progressRowPrefab; // prefab with columns for subject, lesson, status, stars (TMP Text children)
    public Button profileCloseButton;
    public Button deleteUserButton; // optional: delete user data (DB only)
    public Button logoutButton;
    public Button exitButton;
    public Button refreshButton;
    // Confirmation modal (simple reusable confirmation panel)
    public GameObject confirmPanel;
    public TMP_Text confirmMessageText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    // (Removed remote function support: deletion will be DB-only from the client.)

    // internal
    private DatabaseReference rootRef;
    private string currentProfileUid = null;
    private string pendingDeleteUid = null;
    private enum PendingAction { None, DeleteUser, Logout, Exit }
    private PendingAction pendingAction = PendingAction.None;
    private string pendingActionUid = null;

    private async void Start()
    {
        Debug.Log("AdminDashboard: Start()");

        // Wait a short time for FirebaseInitializer to finish dependency checks and enable persistence.
        // If there is no FirebaseInitializer in the scene, create a bootstrap object (matching SplashScreenController's behavior)
        try
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<FirebaseInitializer>();
            if (existing == null)
            {
                Debug.Log("AdminDashboard: No FirebaseInitializer found in scene.");
            }

            if (!FirebaseInitializer.IsInitialized)
            {
                if (existing == null)
                {
                    Debug.LogWarning("AdminDashboard: Creating FirebaseBootstrap GameObject to initialize Firebase.");
                    var go = new GameObject("FirebaseBootstrap");
                    go.AddComponent<FirebaseInitializer>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }

                // Give Firebase more time to initialize (some platforms or slow networks can take longer)
                bool ready = await WaitForFirebaseInitialized(15000);
                if (!ready)
                {
                    Debug.LogError("AdminDashboard: Firebase not initialized in time. Database calls will be skipped.");
                    if (noResultsText != null) noResultsText.gameObject.SetActive(true);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("AdminDashboard: Exception while ensuring FirebaseInitializer exists: " + ex.Message);
        }

        if (searchButton != null)
            searchButton.onClick.AddListener(() => SearchUsers(searchInput != null ? searchInput.text : ""));
        else
            Debug.LogWarning("AdminDashboard: searchButton not assigned in inspector");

    if (searchInput != null) searchInput.onValueChanged.AddListener((s) => SearchUsers(s));
    if (profileCloseButton != null) profileCloseButton.onClick.AddListener(CloseProfile);

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(() => ShowConfirmation(PendingAction.Logout, null, "Are you sure you want to log out?"));
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => ShowConfirmation(PendingAction.Exit, null, "Are you sure you want to exit the app?"));
        }
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => { _ = LoadAllUsers(); });
        }
    if (deleteUserButton != null) deleteUserButton.onClick.RemoveAllListeners();

    // Confirmation panel setup
    if (confirmPanel != null) confirmPanel.SetActive(false);
    if (confirmYesButton != null)
    {
        confirmYesButton.onClick.RemoveAllListeners();
        confirmYesButton.onClick.AddListener(ConfirmYes);
    }
    if (confirmNoButton != null)
    {
        confirmNoButton.onClick.RemoveAllListeners();
        confirmNoButton.onClick.AddListener(ConfirmNo);
    }

    // Ensure the results list is constrained to vertical scrolling only (prevents free dragging)
    if (resultsContainer != null)
    {
        var scroll = resultsContainer.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            try
            {
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = false;
                Debug.Log("AdminDashboard: configured ScrollRect for vertical-only movement");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("AdminDashboard: failed to configure ScrollRect: " + ex.Message);
            }
        }
        else
        {
            Debug.Log("AdminDashboard: no ScrollRect found in parent of resultsContainer (ensure your list is inside a ScrollRect)");
        }
    }

        // initial UI state
        if (profilePanel != null) profilePanel.gameObject.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
        if (noResultsText != null) noResultsText.gameObject.SetActive(false);

        // Wait for a signed-in user (email auth) before attempting DB reads
        bool signedIn = await WaitForSignedInUser(10000);
        if (!signedIn)
        {
            Debug.LogError("AdminDashboard: No signed-in user detected within timeout. Ensure user is authenticated before opening Admin scene.");
            if (noResultsText != null) { noResultsText.gameObject.SetActive(true); noResultsText.text = "Not signed in."; }
            return;
        }

        // Quick admin role check: ensure the signed-in user is allowed to view users
        bool isAdmin = await IsCurrentUserAdmin();
        if (!isAdmin)
        {
            Debug.LogError("AdminDashboard: Current user is not an admin (or lacks permission). DB reads will be blocked by security rules.");
            if (noResultsText != null) { noResultsText.gameObject.SetActive(true); noResultsText.text = "Access denied: not an admin."; }
            return;
        }

    // cache root reference and Load all users into the homescreen list at start
    rootRef = FirebaseDatabase.DefaultInstance.RootReference;
    _ = LoadAllUsers();
    }

    // Wait (with timeout) for a Firebase authenticated user to be available
    private async Task<bool> WaitForSignedInUser(int timeoutMs = 10000)
    {
        try
        {
            int waited = 0;
            const int step = 200;
            while (waited < timeoutMs)
            {
                var user = FirebaseAuth.DefaultInstance.CurrentUser;
                if (user != null) return true;
                await Task.Delay(step);
                waited += step;
            }
            return FirebaseAuth.DefaultInstance.CurrentUser != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("AdminDashboard: Exception while waiting for signed-in user: " + ex.Message);
            return false;
        }
    }

    // Check whether the currently-signed-in user is listed under /admins/{uid} in the DB.
    // Returns true if the admin node exists and is truthy. Logs detailed errors if the read is denied.
    private async Task<bool> IsCurrentUserAdmin()
    {
        try
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                Debug.LogWarning("IsCurrentUserAdmin: no current user");
                return false;
            }

            // Try a few common locations where 'admin' might be represented in this project.
            // 1) /admins/{uid} boolean or object
            // 2) /users/{uid}/profile/role == 'admin'
            // 3) /users/{uid}/role (older layout)

            // Helper to safely read a path and return snapshot (logs errors)
            async Task<DataSnapshot> SafeGet(string path)
            {
                try
                {
                    return await FirebaseDatabase.DefaultInstance.RootReference.Child(path).GetValueAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"IsCurrentUserAdmin: GetValueAsync failed for path='{path}': {ex.Message}");
                    return null;
                }
            }

            // 1) /admins/{uid}
            var adminsSnap = await SafeGet($"admins/{user.UserId}");
            if (adminsSnap != null && adminsSnap.Exists)
            {
                var val = adminsSnap.Value;
                if (val is bool vb)
                {
                    Debug.Log("IsCurrentUserAdmin: admins node boolean check = " + vb);
                    if (vb) return true;
                }
                else if (val is string vs && string.Equals(vs, "true", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("IsCurrentUserAdmin: admins node string 'true' detected");
                    return true;
                }
                else if (adminsSnap.HasChild("isAdmin") && adminsSnap.Child("isAdmin").Exists)
                {
                    var child = adminsSnap.Child("isAdmin");
                    if (child.Value is bool cb)
                    {
                        Debug.Log("IsCurrentUserAdmin: admins/{uid}/isAdmin boolean = " + cb);
                        if (cb) return true;
                    }
                }
                // If admins node exists but didn't indicate admin, continue to other checks.
                Debug.Log("IsCurrentUserAdmin: admins node present but did not indicate admin");
            }

            // 2) /users/{uid}/profile/role
            var profileRoleSnap = await SafeGet($"users/{user.UserId}/profile/role");
            if (profileRoleSnap != null && profileRoleSnap.Exists && profileRoleSnap.Value != null)
            {
                var roleStr = profileRoleSnap.Value.ToString();
                Debug.Log("IsCurrentUserAdmin: profile role = " + roleStr);
                if (string.Equals(roleStr, "admin", StringComparison.OrdinalIgnoreCase)) return true;
            }

            // 3) /users/{uid}/role (legacy)
            var legacyRoleSnap = await SafeGet($"users/{user.UserId}/role");
            if (legacyRoleSnap != null && legacyRoleSnap.Exists && legacyRoleSnap.Value != null)
            {
                var lr = legacyRoleSnap.Value.ToString();
                Debug.Log("IsCurrentUserAdmin: legacy role = " + lr);
                if (string.Equals(lr, "admin", StringComparison.OrdinalIgnoreCase)) return true;
            }

            // No admin markers found
            Debug.Log("IsCurrentUserAdmin: no admin markers found for uid=" + user.UserId);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError("IsCurrentUserAdmin: unexpected error: " + ex);
            return false;
        }
    }

    // Wait (with timeout) for the project's FirebaseInitializer to report it's ready.
    private async Task<bool> WaitForFirebaseInitialized(int timeoutMs = 8000)
    {
        try
        {
            int waited = 0;
            const int step = 200;
            while (waited < timeoutMs)
            {
                if (FirebaseInitializer.IsInitialized) return true;
                await Task.Delay(step);
                waited += step;
            }
            // One final check
            return FirebaseInitializer.IsInitialized;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("AdminDashboard: Exception while waiting for Firebase initialization: " + ex.Message);
            return false;
        }
    }

    public async void SearchUsers(string query)
    {
        // Simple search by displayName; show spinner and fallback to no-results
        ClearResults();
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        if (noResultsText != null) noResultsText.gameObject.SetActive(false);

        try
        {
            var usersRef = (rootRef ?? FirebaseDatabase.DefaultInstance.RootReference).Child("users");
            DataSnapshot snapshot = null;
            try
            {
                snapshot = await usersRef.GetValueAsync();
            }
            catch (Exception exGet)
            {
                Debug.LogError("AdminDashboard: SearchUsers GetValueAsync failed: " + exGet);
                if (noResultsText != null) noResultsText.gameObject.SetActive(true);
                return;
            }

            if (snapshot == null || !snapshot.Exists)
            {
                if (noResultsText != null) noResultsText.gameObject.SetActive(true);
                return;
            }

            var qLower = string.IsNullOrEmpty(query) ? null : query.ToLowerInvariant();
            int found = 0;
            foreach (var child in snapshot.Children)
            {
                if (!child.HasChild("profile")) continue;
                var profile = child.Child("profile");
                if (!profile.HasChild("displayName")) continue;
                string displayName = profile.Child("displayName").Value.ToString();
                string displayLower = profile.HasChild("displayNameLower") ? profile.Child("displayNameLower").Value.ToString() : displayName.ToLowerInvariant();
                if (qLower != null && !displayLower.Contains(qLower)) continue;

                CreateResultRow(displayName, child.Key, profile);
                found++;
            }

            if (found == 0 && noResultsText != null) noResultsText.gameObject.SetActive(true);
        }
        finally
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    private void CreateResultRow(string displayName, string uid, DataSnapshot profile)
    {
        if (resultRowPrefab == null || resultsContainer == null)
        {
            Debug.LogWarning("AdminDashboard: resultRowPrefab or resultsContainer not assigned");
            return;
        }

        var row = Instantiate(resultRowPrefab, resultsContainer);
        Debug.Log($"AdminDashboard: CreateResultRow called for '{displayName}' uid={uid} instantiated '{row.name}'");

        // Minimal list: prefer a named TMP child 'DisplayNameText', otherwise write to first TMP/Text
        TMP_Text label = null;
        var named = row.transform.Find("DisplayNameText");
        if (named != null) label = named.GetComponent<TMP_Text>();
        if (label == null) label = row.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = displayName;
            Debug.Log($"AdminDashboard: label found, set text to '{displayName}'");
        }
        else
        {
            var texts = row.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0) texts[0].text = displayName;
            else Debug.LogWarning("AdminDashboard: CreateResultRow could not find any TMP_Text or Text in the row prefab");
        }

        // Prefer explicit buttons inside the prefab named "ViewButton" and "DeleteButton".
        var viewBtnTf = row.transform.Find("ViewButton");
        var deleteBtnTf = row.transform.Find("DeleteButton");

        Button viewBtn = null;
        Button delBtn = null;
        if (viewBtnTf != null) viewBtn = viewBtnTf.GetComponent<Button>();
        if (deleteBtnTf != null) delBtn = deleteBtnTf.GetComponent<Button>();

        if (viewBtn != null)
        {
            viewBtn.onClick.RemoveAllListeners();
            viewBtn.onClick.AddListener(() =>
            {
                if (homePanel != null) homePanel.SetActive(false);
                OpenProfile(uid);
            });
        }
        else
        {
            // Fallback: if no explicit ViewButton, wire the first Button found to open profile
            var anyBtn = row.GetComponentInChildren<Button>(true);
            if (anyBtn != null)
            {
                anyBtn.onClick.RemoveAllListeners();
                anyBtn.onClick.AddListener(() =>
                {
                    if (homePanel != null) homePanel.SetActive(false);
                    OpenProfile(uid);
                });
                Debug.Log("AdminDashboard: wired fallback button to open profile for row " + displayName);
            }
            else
            {
                Debug.LogWarning("AdminDashboard: row prefab has no ViewButton or Button to open profile");
            }
        }

        if (delBtn != null)
        {
            delBtn.onClick.RemoveAllListeners();
            delBtn.onClick.AddListener(() => PrepareDeleteUser(uid));
        }
        else
        {
            Debug.Log("AdminDashboard: row prefab has no DeleteButton; use the profile Delete action instead");
        }

        // Remove any EventTrigger components that could allow dragging/moving the row unexpectedly
        var eventTriggers = row.GetComponents<UnityEngine.EventSystems.EventTrigger>();
        foreach (var et in eventTriggers)
        {
            Destroy(et);
        }

        // Ensure the row stretches horizontally so layout system controls position (reduces free-drag behavior)
        var rt = row.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        }
    }

    [ContextMenu("Spawn Test Rows")]
    public void SpawnTestRows()
    {
    Debug.Log("SpawnTestRows: disabled in production builds.");
    }

    public async Task LoadAllUsers()
    {
        Debug.Log("AdminDashboard: LoadAllUsers() start");
        ClearResults();
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        if (noResultsText != null) noResultsText.gameObject.SetActive(false);
        try
        {
            var usersRef = (rootRef ?? FirebaseDatabase.DefaultInstance.RootReference).Child("users");
            DataSnapshot snapshot = null;
            try
            {
                snapshot = await usersRef.GetValueAsync();
            }
            catch (Exception exGet)
            {
                Debug.LogError("AdminDashboard: LoadAllUsers GetValueAsync failed: " + exGet);
                if (noResultsText != null) noResultsText.gameObject.SetActive(true);
                return;
            }

            Debug.Log($"AdminDashboard: LoadAllUsers snapshot.Exists={(snapshot != null && snapshot.Exists)}");
            if (snapshot == null || !snapshot.Exists)
            {
                if (noResultsText != null) noResultsText.gameObject.SetActive(true);
                return;
            }
            int count = 0;
            foreach (var child in snapshot.Children)
            {
                if (!child.HasChild("profile")) continue;
                var profile = child.Child("profile");
                string displayName = profile.HasChild("displayName") ? profile.Child("displayName").Value.ToString() : child.Key;
                CreateResultRow(displayName, child.Key, profile);
                count++;
            }
            if (count == 0 && noResultsText != null) noResultsText.gameObject.SetActive(true);
            Debug.Log($"AdminDashboard: LoadAllUsers created {count} rows");
        }
        finally
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    private void ClearResults()
    {
        foreach (Transform t in resultsContainer) Destroy(t.gameObject);
    }

    public async void OpenProfile(string uid)
    {
        // show profile panel and load basic profile fields only (keep it simple)
        if (profilePanel != null) profilePanel.gameObject.SetActive(true);
        if (profileNameText != null) profileNameText.text = "(loading...)";

        DataSnapshot profileSnap = null;
        try
        {
            profileSnap = await FirebaseDatabase.DefaultInstance.RootReference.Child($"users/{uid}/profile").GetValueAsync();
        }
        catch (Exception exGet)
        {
            Debug.LogError("AdminDashboard: OpenProfile GetValueAsync failed for uid=" + uid + " : " + exGet);
            return;
        }

        if (profileSnap != null && profileSnap.Exists)
        {
            string name = profileSnap.HasChild("displayName") ? profileSnap.Child("displayName").Value.ToString() : "(unknown)";
            if (profileNameText != null) profileNameText.text = name;

            if (profileRoleText != null && profileSnap.HasChild("role")) profileRoleText.text = profileSnap.Child("role").Value.ToString();
            if (profileEmailText != null)
            {
                if (profileSnap.HasChild("email")) profileEmailText.text = profileSnap.Child("email").Value.ToString();
                else profileEmailText.text = "-";
            }

            if (profileTimestampsText != null)
            {
                try
                {
                    long created = profileSnap.HasChild("createdAt") && long.TryParse(profileSnap.Child("createdAt").Value.ToString(), out var cval) ? cval : 0L;
                    long lastLogin = profileSnap.HasChild("lastLoginAt") && long.TryParse(profileSnap.Child("lastLoginAt").Value.ToString(), out var lval) ? lval : 0L;
                    var parts = new List<string>();
                    if (created > 0) parts.Add("Created: " + DateTimeOffset.FromUnixTimeMilliseconds(created).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                    if (lastLogin > 0) parts.Add("Last login: " + DateTimeOffset.FromUnixTimeMilliseconds(lastLogin).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                    profileTimestampsText.text = parts.Count > 0 ? string.Join("\n", parts) : "-";
                }
                catch (Exception)
                {
                    profileTimestampsText.text = "-";
                }
            }

            var metaParts = new List<string>();
            if (profileSnap.HasChild("age")) metaParts.Add($"Age: {profileSnap.Child("age").Value}");
            if (profileSnap.HasChild("gender")) metaParts.Add($"Gender: {profileSnap.Child("gender").Value}");
            if (profileMetaText != null) profileMetaText.text = string.Join("  ", metaParts);

            if (profileSubjectsText != null && profileSnap.HasChild("preferredSubjects"))
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var s in profileSnap.Child("preferredSubjects").Children)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append(s.Value.ToString());
                    }
                    profileSubjectsText.text = sb.ToString();
                }
                catch { profileSubjectsText.text = "-"; }
            }

            if (profileAvatarImage != null && profileSnap.HasChild("avatar"))
            {
                var avatarKey = profileSnap.Child("avatar").Value.ToString();
                var tex = Resources.Load<Sprite>($"Avatars/{avatarKey}");
                if (tex != null) profileAvatarImage.sprite = tex;
            }
        }

        // hide homescreen results while profile is open
        if (homePanel != null) homePanel.SetActive(false);

        // wire delete button for this profile (two-step confirm)
        currentProfileUid = uid;
        if (deleteUserButton != null)
        {
            deleteUserButton.onClick.RemoveAllListeners();
            deleteUserButton.onClick.AddListener(() => PrepareDeleteUser(uid));
            var t = deleteUserButton.GetComponentInChildren<TMP_Text>(); if (t != null) t.text = "Delete User";
        }
    }

    // Close profile view (public for inspector)
    public void CloseProfile()
    {
        if (profilePanel != null) profilePanel.gameObject.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);

        // clear UI fields
        if (profileNameText != null) profileNameText.text = "";
        if (profileMetaText != null) profileMetaText.text = "";
        if (profileRoleText != null) profileRoleText.text = "";
        if (profileEmailText != null) profileEmailText.text = "";
        if (profileSubjectsText != null) profileSubjectsText.text = "";
        if (progressTableContainer != null)
        {
            foreach (Transform t in progressTableContainer) Destroy(t.gameObject);
        }
        currentProfileUid = null;
    }

    private void PrepareDeleteUser(string uid)
    {
        // show a confirmation modal before deleting user
        ShowConfirmation(PendingAction.DeleteUser, uid, $"Are you sure you want to delete this user?");
    }

    private void ShowConfirmation(PendingAction action, string uid, string message)
    {
        pendingAction = action;
        pendingActionUid = uid;
        if (confirmMessageText != null) confirmMessageText.text = message;
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    private void ConfirmYes()
    {
        if (pendingAction == PendingAction.DeleteUser && !string.IsNullOrEmpty(pendingActionUid))
        {
            // perform delete
            _ = DeleteUserData(pendingActionUid);
        }
        else if (pendingAction == PendingAction.Logout)
        {
            Logout();
        }
        else if (pendingAction == PendingAction.Exit)
        {
            ExitApp();
        }

        // clear and hide
        pendingAction = PendingAction.None;
        pendingActionUid = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void ConfirmNo()
    {
        // simply hide confirmation
        pendingAction = PendingAction.None;
        pendingActionUid = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private System.Collections.IEnumerator ResetDeletePending(string uid, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (pendingDeleteUid == uid) pendingDeleteUid = null;
        if (deleteUserButton != null)
        {
            var tt = deleteUserButton.GetComponentInChildren<TMP_Text>(); if (tt != null) tt.text = "Delete User";
        }
    }

    private async Task DeleteUserData(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;
        if (rootRef == null) rootRef = FirebaseDatabase.DefaultInstance.RootReference;
        try
        {
            Debug.Log("AdminDashboard: Deleting user data for uid=" + uid);
            // Delete database nodes only (no Auth deletion)
            await rootRef.Child($"users/{uid}").RemoveValueAsync();
            await rootRef.Child($"progress/{uid}").RemoveValueAsync();
            await rootRef.Child($"stats/{uid}").RemoveValueAsync();
            Debug.Log("AdminDashboard: DeleteUserData completed (DB-only) for uid=" + uid);
            // refresh list and close profile
            CloseProfile();
            await LoadAllUsers();
        }
        catch (Exception ex)
        {
            Debug.LogError("AdminDashboard: failed to delete user data: " + ex.Message);
        }
    }

    // Remote Cloud Function support fully removed. Deletions are DB-only from the client.

    private void Logout()
    {
        try
        {
            FirebaseAuth.DefaultInstance.SignOut();
        }
        catch (Exception ex) { Debug.LogWarning("Logout: SignOut threw: " + ex.Message); }
        PlayerPrefs.DeleteKey("lastSignedInUid");
        PlayerPrefs.DeleteKey("lastSignedInRole");
        PlayerPrefs.Save();
        try { SceneManager.LoadScene("Login"); } catch { }
    }

    private void ExitApp()
    {
        Application.Quit();
    }

    // Public test method: wire to a dev-only button to run a quick DB/auth diagnostic.
    public async void TestDbConnection()
    {
        try
        {
            var app = FirebaseApp.DefaultInstance;
            Debug.Log($"TestDbConnection: FirebaseApp name={app.Name} dbUrl={app.Options.DatabaseUrl}");
        }
        catch (Exception ex)
        {
            Debug.LogError("TestDbConnection: No FirebaseApp instance: " + ex);
        }

        try
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null)
            {
                Debug.Log($"TestDbConnection: Auth user uid={user.UserId} email={user.Email}");
                try
                {
                    var token = await user.TokenAsync(false);
                    Debug.Log($"TestDbConnection: auth token length={token?.Length}");
                }
                catch (Exception tex)
                {
                    Debug.LogWarning("TestDbConnection: failed to get token: " + tex.Message);
                }
            }
            else
            {
                Debug.Log("TestDbConnection: no auth user");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("TestDbConnection: Auth check failed: " + ex);
        }

        try
        {
            var snap = await FirebaseDatabase.DefaultInstance.RootReference.Child("users").GetValueAsync();
            Debug.Log($"TestDbConnection: read users ok exists={snap.Exists} children={snap.ChildrenCount}");
        }
        catch (Exception ex)
        {
            Debug.LogError("TestDbConnection: DB read failed: " + ex);
        }
    }
}
