using UnityEngine;

public class EdgeHintFollower : MonoBehaviour
{
    public EdgeCollider2D edge;
    public Transform pointer;
    public float speed = 1f;
    private int currentIndex = 0;
    private Vector3[] worldPoints;

    void Start()
    {
        if (edge == null || pointer == null)
        {
            Debug.LogWarning("Missing refs");
            enabled = false;
            return;
        }

        Vector2[] localPoints = edge.points;
        worldPoints = new Vector3[localPoints.Length];

        for (int i = 0; i < localPoints.Length; i++)
            worldPoints[i] = edge.transform.TransformPoint(localPoints[i]);
    }

    void Update()
    {
        if (worldPoints == null || worldPoints.Length < 2) return;

        Vector3 target = worldPoints[currentIndex];
        pointer.position = Vector3.MoveTowards(pointer.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(pointer.position, target) < 0.01f)
            currentIndex = (currentIndex + 1) % worldPoints.Length;
    }
}