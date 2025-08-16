using UnityEngine;
using UnityEngine.UI; // Needed for Button component
using UnityEngine.SceneManagement;

public class Controller : MonoBehaviour
{
    [Header("Letter Loading")]
    public Transform spawnPoint;                // Where the prefab will appear
    public GameObject[] letterPrefabs;          // Assign ALL letter prefabs here in order (A → Z)

    [Header("UI Buttons")]
    public Button backBtn;                      // Assign BackBtn from the scene
    public Button nextBtn;                      // Assign NextBtn from the scene

    private int currentIndex = 0;
    private GameObject currentLetterInstance;

    void Start()
    {
        // Get selected letter name from Alphabet UI (default to first)
        string selectedLetter = PlayerPrefs.GetString("SelectedLetter", letterPrefabs[0].name);

        // Find index of selected letter in array
        for (int i = 0; i < letterPrefabs.Length; i++)
        {
            if (letterPrefabs[i].name == selectedLetter)
            {
                currentIndex = i;
                break;
            }
        }

        // Load starting letter
        LoadLetter(currentIndex);
    }

    private void LoadLetter(int index)
    {
        // Destroy any existing letter prefab
        if (currentLetterInstance != null)
            Destroy(currentLetterInstance);

        // Instantiate new one
        currentLetterInstance = Instantiate(letterPrefabs[index], spawnPoint.position, Quaternion.identity);

        // Play audio automatically for the new letter
        PlayCurrentLetterSound();

        // Update navigation buttons
        UpdateNavButtons();
    }

    public void PlayCurrentLetterSound()
    {
        if (currentLetterInstance != null)
        {
            LetterAudio letterAudio = currentLetterInstance.GetComponentInChildren<LetterAudio>();
            if (letterAudio != null)
                letterAudio.PlayLetterSound();
        }
    }

    public void NextLetter()
    {
        if (currentIndex < letterPrefabs.Length - 1)
        {
            currentIndex++;
            LoadLetter(currentIndex);
        }
    }

    public void PreviousLetter()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            LoadLetter(currentIndex);
        }
    }

    private void UpdateNavButtons()
    {
        if (backBtn != null)
            backBtn.interactable = currentIndex > 0;

        if (nextBtn != null)
            nextBtn.interactable = currentIndex < letterPrefabs.Length - 1;
    }
}