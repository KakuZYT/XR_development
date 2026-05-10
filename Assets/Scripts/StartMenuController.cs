using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup mainPanelCanvasGroup;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "02_Descent_Transition";

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 1f;

    private bool isStarting;

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartAdventure);
        }
    }

    public void StartAdventure()
    {
        if (isStarting)
        {
            return;
        }

        isStarting = true;
        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        if (mainPanelCanvasGroup != null)
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                mainPanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }

            mainPanelCanvasGroup.alpha = 0f;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}