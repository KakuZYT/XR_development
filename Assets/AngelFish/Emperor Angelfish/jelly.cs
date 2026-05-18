using UnityEngine;

public class JellyfishMovement : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float floatHeight = 2f;
    public float driftSpeed = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Floating up and down
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // Slow drifting forward
        transform.position = new Vector3(
            transform.position.x,
            newY,
            transform.position.z + driftSpeed * Time.deltaTime
        );
    }
}