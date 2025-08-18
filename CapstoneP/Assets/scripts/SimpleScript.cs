using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Grouped Button Selector")]
public class GroupedButtonSelector : MonoBehaviour
{
    [Header("Button Groups (assign in Inspector)")]
    public List<Button> ageButtons = new List<Button>();
    public List<Button> genderButtons = new List<Button>();
    public List<Button> avatarButtons = new List<Button>();

    [Header("Visual")]
    [Tooltip("Y offset applied to the selected button's RectTransform.anchoredPosition.y")]
    public float pressedYOffset = -5f;

    // internal state
    private readonly Dictionary<Button, Vector2> originalAnchored = new Dictionary<Button, Vector2>();
    private Button selectedAge;
    private Button selectedGender;
    private Button selectedAvatar;

    private void Start()
    {
        WireGroup(ageButtons, OnAgeClicked);
        WireGroup(genderButtons, OnGenderClicked);
        WireGroup(avatarButtons, OnAvatarClicked);

        // ensure initial selected visuals (if any)
        ApplySelectedVisual(selectedAge);
        ApplySelectedVisual(selectedGender);
        ApplySelectedVisual(selectedAvatar);
    }

    private void OnDestroy()
    {
        UnwireGroup(ageButtons, OnAgeClicked);
        UnwireGroup(genderButtons, OnGenderClicked);
        UnwireGroup(avatarButtons, OnAvatarClicked);
    }

    private void WireGroup(List<Button> group, UnityEngine.Events.UnityAction<Button> handler)
    {
        if (group == null) return;
        foreach (var b in group)
        {
            if (b == null) continue;
            // store original anchored position
            var rt = b.GetComponent<RectTransform>();
            if (rt != null && !originalAnchored.ContainsKey(b))
                originalAnchored[b] = rt.anchoredPosition;

            // capture local reference for closure
            Button captured = b;
            b.onClick.AddListener(() => handler(captured));
        }
    }

    private void UnwireGroup(List<Button> group, UnityEngine.Events.UnityAction<Button> handler)
    {
        if (group == null) return;
        foreach (var b in group)
        {
            if (b == null) continue;
            b.onClick.RemoveAllListeners(); // safe, or remove specific if you prefer
        }
    }

    // public getters
    public int SelectedAgeIndex => IndexOf(ageButtons, selectedAge);
    public int SelectedGenderIndex => IndexOf(genderButtons, selectedGender);
    public int SelectedAvatarIndex => IndexOf(avatarButtons, selectedAvatar);

    private int IndexOf(List<Button> list, Button b)
    {
        if (list == null || b == null) return -1;
        return list.IndexOf(b);
    }

    // click handlers
    private void OnAgeClicked(Button b)    => SelectInGroup(b, ref selectedAge, ageButtons);
    private void OnGenderClicked(Button b) => SelectInGroup(b, ref selectedGender, genderButtons);
    private void OnAvatarClicked(Button b) => SelectInGroup(b, ref selectedAvatar, avatarButtons);

    private void SelectInGroup(Button clicked, ref Button selectedField, List<Button> group)
    {
        if (clicked == null) return;
        // already selected -> no-op
        if (clicked == selectedField) return;

        // restore previous
        if (selectedField != null)
            RestoreOriginal(selectedField);

        // set new
        selectedField = clicked;
        ApplySelectedVisual(selectedField);
    }

    private void RestoreOriginal(Button b)
    {
        if (b == null) return;
        var rt = b.GetComponent<RectTransform>();
        if (rt == null) return;
        // prefer using the ButtonPressAnimation to animate back to original if present
        var anim = b.GetComponent<ButtonPressAnimation>();
        if (anim != null)
        {
            anim.SetSelected(false);
            return;
        }

        if (originalAnchored.TryGetValue(b, out Vector2 orig))
            rt.anchoredPosition = orig;
    }

    private void ApplySelectedVisual(Button b)
    {
        if (b == null) return;
        var rt = b.GetComponent<RectTransform>();
        if (rt == null) return;
        // if a ButtonPressAnimation exists, let it handle the visual pressed state (animated)
        var anim = b.GetComponent<ButtonPressAnimation>();
        if (anim != null)
        {
            // mark as selected so the animation will persist the pressed pose
            anim.SetSelected(true);
            return;
        }

        float baseY = originalAnchored.ContainsKey(b) ? originalAnchored[b].y : rt.anchoredPosition.y;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseY + pressedYOffset);
    }
}