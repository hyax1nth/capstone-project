using UnityEngine;

public class StrokeCheckpointTracker : MonoBehaviour
{
    public int totalCheckpoints;
    private int checkpointsHit = 0;

    public void RegisterCheckpoint(int index)
    {
        // Only count if hitting the correct next checkpoint
        if (index == checkpointsHit + 1)
        {
            checkpointsHit++;
            Debug.Log("Checkpoint " + index + " hit!");
        }
    }

    public bool AllCheckpointsHit()
    {
        return checkpointsHit >= totalCheckpoints;
    }

    public void ResetCheckpoints()
    {
        checkpointsHit = 0;
    }
}