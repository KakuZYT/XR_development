using UnityEngine;

public class BoatFloating : MonoBehaviour
{
    [Header("Water Level")]
    public float waterY = 0f;
    public float boatHeightOffset = 0.3f;

    [Header("Floating Motion")]
    public float bobAmplitude = 0.25f;
    public float bobSpeed = 1.2f;

    [Header("Rotation Motion")]
    public float rollAmplitude = 3f;
    public float pitchAmplitude = 2f;
    public float rotationSpeed = 0.8f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

        Vector3 newPosition = transform.position;
        newPosition.y = waterY + boatHeightOffset + bob;
        transform.position = newPosition;

        float roll = Mathf.Sin(Time.time * rotationSpeed) * rollAmplitude;
        float pitch = Mathf.Cos(Time.time * rotationSpeed * 0.7f) * pitchAmplitude;

        transform.rotation = startRotation * Quaternion.Euler(pitch, 0f, roll);
    }
}