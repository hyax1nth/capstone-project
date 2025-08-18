using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
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

        // Load all users into the homescreen list at start
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

            var adminRef = FirebaseDatabase.DefaultInstance.RootReference.Child($"admins/{user.UserId}");
            DataSnapshot snap = null;
            try
            {
                snap = await adminRef.GetValueAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("IsCurrentUserAdmin: GetValueAsync failed for admins node: " + ex);
                return false;
            }

            if (snap == null || !snap.Exists) return false;
            // If value is boolean true or string "true", consider admin
            var v = snap.Value;
            if (v is bool b) return b;
            if (v is string s && string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            // if child named 'isAdmin' exists
            if (snap.HasChild("isAdmin"))
            {
                var c = snap.Child("isAdmin");
                if (c.Exists && c.Value is bool cb) return cb;
            }
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
            var usersRef = FirebaseDatabase.DefaultInstance.RootReference.Child("users");
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

        var btn = row.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                if (homePanel != null) homePanel.SetActive(false);
                OpenProfile(uid);
            });
        }
        else
        {
            Debug.LogWarning("AdminDashboard: row prefab has no Button (GetComponentInChildren<Button> returned null)");
        }
    }

    [ContextMenu("Spawn Test Rows")]
    public void SpawnTestRows()
    {
        if (resultRowPrefab == null || resultsContainer == null)
        {
            Debug.LogWarning("AdminDashboard: SpawnTestRows requires resultRowPrefab and resultsContainer assigned.");
            return;
        }
        ClearResults();
        for (int i = 1; i <= 6; i++)
        {
            CreateResultRow($"Test User {i}", $"test{i}", null);
        }
    }

    public async Task LoadAllUsers()
    {
        Debug.Log("AdminDashboard: LoadAllUsers() start");
        ClearResults();
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        if (noResultsText != null) noResultsText.gameObject.SetActive(false);
        try
        {
            var usersRef = FirebaseDatabase.DefaultInstance.RootReference.Child("users");
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
            if (profileEmailText != null && profileSnap.HasChild("email")) profileEmailText.text = profileSnap.Child("email").Value.ToString();

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
    }

    // Close profile view (public for inspector)
    public void CloseProfile()
    {
        if (profilePanel != null) profilePanel.gameObject.SetActive(false);
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
            Debug.Log(user != null ? $"TestDbConnection: Auth user uid={user.UserId} email={user.Email}" : "TestDbConnection: no auth user");
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
