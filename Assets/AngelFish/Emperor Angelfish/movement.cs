using UnityEngine;

public class FishMovement : MonoBehaviour
{
    public float speed = 3f;
    public float moveDistance = 30f;

    private Vector3 startPos;
    private bool movingRight = true;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (movingRight)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            if (Vector3.Distance(startPos, transform.position) > moveDistance)
            {
                movingRight = false;
                transform.Rotate(0, 180, 0);
            }
        }
        else
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            if (Vector3.Distance(startPos, transform.position) < 1f)
            {
                movingRight = true;
                transform.Rotate(0, 180, 0);
            }
        }
    }
}