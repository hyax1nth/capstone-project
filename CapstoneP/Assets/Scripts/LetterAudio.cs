using UnityEngine;

public class LetterAudio : MonoBehaviour
{
    public AudioSource audioSource;

    public void PlayLetterSound()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }

        }
}
