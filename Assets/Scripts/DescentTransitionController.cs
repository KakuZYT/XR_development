using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DescentTransitionController : MonoBehaviour
{
    [Header("XR Rig")]
    [SerializeField] private Transform xrOrigin;

    [Header("Descent Movement")]
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 1.5f, -3f);
    [SerializeField] private Vector3 endPosition = new Vector3(0f, -2f, 2f);
    [SerializeField] private float descentDuration = 6f;
    [SerializeField] private float startDelay = 0.15f;
    [SerializeField] private int stabilizationFrames = 3;
    [SerializeField] private bool disableCharacterControllerDuringDescent = true;
    [SerializeField] private bool respectCollisionDuringDescent = true;

    [Header("Controller Pointer Visuals")]
    [SerializeField] private bool tuneLeftPointerWidth = true;
    [SerializeField, Range(0.1f, 1f)] private float leftPointerWidthScale = 0.45f;

    [Header("Ambient Audio Transition")]
    [SerializeField] private bool useDescentAmbientAudio = true;
    [SerializeField] private float underwaterCrossfadeDuration = 3f;

    [Header("Next Scene")]
    [SerializeField] private bool loadNextSceneAfterDescent = false;
    [SerializeField] private string nextSceneName = "03_Coral_Reef_Explore";

    private CharacterController characterController;
    private DescentAmbientAudioController ambientAudioController;

    private void Start()
    {
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin is not assigned.");
            return;
        }

        characterController = xrOrigin.GetComponent<CharacterController>();
        ConfigureControllerPointerVisuals();

        if (useDescentAmbientAudio)
        {
            ambientAudioController = GetComponent<DescentAmbientAudioController>();

            if (ambientAudioController == null)
            {
                ambientAudioController = gameObject.AddComponent<DescentAmbientAudioController>();
            }
        }

        StartCoroutine(PlayDescent());
    }

    private IEnumerator PlayDescent()
    {
        float timer = 0f;
        float duration = Mathf.Max(descentDuration, 0.1f);
        xrOrigin.position = startPosition;

        bool shouldRestoreCharacterController = false;
        if (disableCharacterControllerDuringDescent && characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
            shouldRestoreCharacterController = true;
        }

        for (int i = 0; i < stabilizationFrames; i++)
        {
            yield return null;
        }

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (ambientAudioController != null)
        {
            ambientAudioController.CrossfadeToUnderwater(
                Mathf.Min(Mathf.Max(underwaterCrossfadeDuration, 0.1f), duration)
            );
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // Smooth movement instead of linear drop.
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            MoveRig(Vector3.Lerp(startPosition, endPosition, smoothT));

            yield return null;
        }

        MoveRig(endPosition);

        if (shouldRestoreCharacterController)
        {
            characterController.enabled = true;
        }

        if (loadNextSceneAfterDescent && !string.IsNullOrEmpty(nextSceneName))
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }
    }

    private void MoveRig(Vector3 targetPosition)
    {
        if (respectCollisionDuringDescent && characterController != null && characterController.enabled)
        {
            characterController.Move(targetPosition - xrOrigin.position);
            return;
        }

        xrOrigin.position = targetPosition;
    }

    private void ConfigureControllerPointerVisuals()
    {
        if (!tuneLeftPointerWidth)
        {
            return;
        }

        foreach (LineRenderer lineRenderer in xrOrigin.GetComponentsInChildren<LineRenderer>(true))
        {
            Transform current = lineRenderer.transform;

            while (current != null && current != xrOrigin)
            {
                if (current.name == "Left Controller")
                {
                    lineRenderer.widthMultiplier *= leftPointerWidthScale;
                    break;
                }

                current = current.parent;
            }
        }
    }
}
