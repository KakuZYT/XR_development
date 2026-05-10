using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartBoatSequence : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform xrCamera;

    [Header("Target")]
    [SerializeField] private Transform boatBoardingPoint;

    [Header("UI")]
    [SerializeField] private GameObject startMenuCanvas;
    [SerializeField] private Image fadeImage;

    [Header("Timing")]
    [SerializeField] private float moveDuration = 10f;
    [SerializeField] private float waitOnBoatDuration = 3f;
    [SerializeField] private float fadeDuration = 2f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "02_Descent_Transition";

    [Header("Movement")]
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private float gravity = -9.81f;

    private bool isRunning;
    private CharacterController characterController;

    private void Awake()
    {
        if (xrOrigin != null)
        {
            characterController = xrOrigin.GetComponent<CharacterController>();
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }
    }

    public void StartSequence()
    {
        Debug.Log("StartBoatSequence triggered.");

        if (isRunning)
        {
            return;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin is not assigned.");
            return;
        }

        if (xrCamera == null)
        {
            Debug.LogError("XR Camera is not assigned.");
            return;
        }

        if (boatBoardingPoint == null)
        {
            Debug.LogError("Boat Boarding Point is not assigned.");
            return;
        }

        if (fadeImage == null)
        {
            Debug.LogError("Fade Image is not assigned.");
            return;
        }

        if (characterController == null)
        {
            Debug.LogWarning("XR Origin has no CharacterController. The script will move by Transform.");
        }

        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        isRunning = true;

        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(false);
        }

        yield return MovePlayerToBoatWithCollision();

        yield return new WaitForSeconds(waitOnBoatDuration);

        yield return FadeToBlack();

        Debug.Log("Loading scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator MovePlayerToBoatWithCollision()
    {
        Vector3 cameraOffset = xrCamera.position - xrOrigin.position;
        cameraOffset.y = 0f;

        Vector3 targetPosition = boatBoardingPoint.position - cameraOffset;
        targetPosition.y = xrOrigin.position.y;

        Vector3 startPosition = xrOrigin.position;
        float duration = Mathf.Max(moveDuration, 0.1f);
        float timer = 0f;

        Debug.Log("Move distance to boat: " + Vector3.Distance(startPosition, targetPosition));

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 desiredPosition = Vector3.Lerp(startPosition, targetPosition, t);

            Vector3 horizontalMove = desiredPosition - xrOrigin.position;
            horizontalMove.y = 0f;

            Vector3 verticalMove = Vector3.zero;

            if (characterController != null && characterController.enabled)
            {
                if (characterController.isGrounded)
                {
                    verticalMove.y = -groundOffset;
                }
                else
                {
                    verticalMove.y = gravity * Time.deltaTime;
                }

                characterController.Move(horizontalMove + verticalMove);
            }
            else
            {
                xrOrigin.position += horizontalMove;
            }

            yield return null;
        }
    }

    private IEnumerator FadeToBlack()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.transform.SetAsLastSibling();

        Color color = fadeImage.color;
        color.r = 0f;
        color.g = 0f;
        color.b = 0f;
        color.a = 0f;
        fadeImage.color = color;

        float timer = 0f;
        float duration = Mathf.Max(fadeDuration, 0.1f);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            color.a = timer / duration;
            fadeImage.color = color;

            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }
}