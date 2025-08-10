using UnityEngine;
using System.Collections;

public class LoadingPanelDisabler : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private float waitTime = 1f;

    void Start()
    {
        StartCoroutine(DisableAfterDelay());
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(waitTime);
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
}
