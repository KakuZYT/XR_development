using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GlobalClickPauseController : MonoBehaviour
{
    [SerializeField] private GameObject ignoredGalleryPanel;
    [SerializeField] private string pauseMessage = "Paused\nPress any key to continue";

    private bool pausedByClick;
    private float timeScaleBeforePause = 1f;
    private GameObject pauseOverlay;
    private Text pauseText;

    public void SetIgnoredGallery(GameObject galleryPanel)
    {
        ignoredGalleryPanel = galleryPanel;
    }

    public void ResumeIfPaused()
    {
        if (pausedByClick)
        {
            ResumeGame();
        }
    }

    private void Update()
    {
        if (pausedByClick)
        {
            if (ResumeInputWasPressed())
            {
                ResumeGame();
            }

            return;
        }

        if (CanPauseFromEscape() && PauseInputWasPressed())
        {
            PauseGame();
        }
    }

    private bool CanPauseFromEscape()
    {
        if (ignoredGalleryPanel != null && ignoredGalleryPanel.activeInHierarchy)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            return false;
        }

        return true;
    }

    private bool PauseInputWasPressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    private bool ResumeInputWasPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        bool inputPressed = false;

        if (Mouse.current != null)
        {
            inputPressed = Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            inputPressed = inputPressed
                || Gamepad.current.buttonSouth.wasPressedThisFrame
                || Gamepad.current.buttonNorth.wasPressedThisFrame
                || Gamepad.current.buttonEast.wasPressedThisFrame
                || Gamepad.current.buttonWest.wasPressedThisFrame
                || Gamepad.current.startButton.wasPressedThisFrame;
        }

        if (Touchscreen.current != null)
        {
            inputPressed = inputPressed || Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        return inputPressed;
    }

    private void PauseGame()
    {
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        pausedByClick = true;

        SetupPauseOverlay();
        pauseOverlay.SetActive(true);
    }

    private void ResumeGame()
    {
        Time.timeScale = timeScaleBeforePause <= 0f ? 1f : timeScaleBeforePause;
        pausedByClick = false;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(false);
        }
    }

    private void SetupPauseOverlay()
    {
        if (pauseOverlay != null)
        {
            return;
        }

        pauseOverlay = new GameObject("GlobalPauseOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        pauseOverlay.transform.SetParent(transform, false);

        Canvas canvas = pauseOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = pauseOverlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(pauseOverlay.transform, false);

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.48f);
        backgroundImage.raycastTarget = true;

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("PauseText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(pauseOverlay.transform, false);

        pauseText = textObject.GetComponent<Text>();
        pauseText.text = pauseMessage;
        pauseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pauseText.fontSize = 42;
        pauseText.fontStyle = FontStyle.Bold;
        pauseText.alignment = TextAnchor.MiddleCenter;
        pauseText.color = Color.white;
        pauseText.raycastTarget = false;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(760f, 180f);
        textRect.anchoredPosition = Vector2.zero;
    }
}
