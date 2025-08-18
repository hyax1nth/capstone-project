using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("The order this checkpoint should be hit in (1, 2, 3...)")]
    public int checkpointIndex = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure we only react to the tracing input, not the hint pointer
        var guide = GetComponentInParent<StrokeGuide>();
        if (guide != null)
        {
            guide.RegisterCheckpoint(checkpointIndex);
            // Uncomment this if you want debug feedback while testing:
            Debug.Log($"Hit checkpoint {checkpointIndex}");
        }
    }
}