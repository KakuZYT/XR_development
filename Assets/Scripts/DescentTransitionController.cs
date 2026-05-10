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

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "03_Coral_Reef_Explore";

    private void Start()
    {
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin is not assigned.");
            return;
        }

        StartCoroutine(PlayDescent());
    }

    private IEnumerator PlayDescent()
    {
        float timer = 0f;
        xrOrigin.position = startPosition;

        while (timer < descentDuration)
        {
            timer += Time.deltaTime;
            float t = timer / descentDuration;

            // Smooth movement instead of linear drop.
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            xrOrigin.position = Vector3.Lerp(startPosition, endPosition, smoothT);

            yield return null;
        }

        xrOrigin.position = endPosition;
        SceneManager.LoadScene(nextSceneName);
    }
}