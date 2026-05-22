using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalClickPauseController : MonoBehaviour
{
    [SerializeField] private GameObject ignoredGalleryPanel;
    [SerializeField] private string pauseMessage = "Paused\nPress any key to continue";
    [SerializeField] private string beachSceneName = "01_Beach_StartMenu";
    [SerializeField] private string returnToBeachButtonText = "Return to Beach";

    private static GlobalClickPauseController instance;

    private bool pausedByClick;
    private float timeScaleBeforePause = 1f;
    private GameObject pauseOverlay;
    private Text pauseText;
    private Button returnToBeachButton;

    public static GlobalClickPauseController Instance
    {
        get
        {
            return EnsureInstance();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static GlobalClickPauseController EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GlobalClickPauseController existingController = FindFirstObjectByType<GlobalClickPauseController>();
        if (existingController != null)
        {
            instance = existingController;
            return instance;
        }

        GameObject pauseObject = new GameObject("GlobalClickPauseController");
        instance = pauseObject.AddComponent<GlobalClickPauseController>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.name = "GlobalClickPauseController";

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

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

        if (Gamepad.current != null)
        {
            inputPressed = inputPressed
                || Gamepad.current.buttonSouth.wasPressedThisFrame
                || Gamepad.current.buttonNorth.wasPressedThisFrame
                || Gamepad.current.buttonEast.wasPressedThisFrame
                || Gamepad.current.buttonWest.wasPressedThisFrame
                || Gamepad.current.startButton.wasPressedThisFrame;
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
        textRect.anchoredPosition = new Vector2(0f, 60f);

        GameObject buttonObject = new GameObject("ReturnToBeachButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(pauseOverlay.transform, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.62f, 0.84f, 1f, 0.95f);
        buttonImage.raycastTarget = true;

        returnToBeachButton = buttonObject.GetComponent<Button>();
        returnToBeachButton.targetGraphic = buttonImage;
        returnToBeachButton.onClick.AddListener(ReturnToBeach);

        ColorBlock colors = returnToBeachButton.colors;
        colors.normalColor = new Color(0.62f, 0.84f, 1f, 0.95f);
        colors.highlightedColor = new Color(0.78f, 0.92f, 1f, 1f);
        colors.pressedColor = new Color(0.42f, 0.68f, 0.88f, 1f);
        colors.selectedColor = new Color(0.62f, 0.84f, 1f, 1f);
        returnToBeachButton.colors = colors;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(320f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, -110f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text label = labelObject.GetComponent<Text>();
        label.text = returnToBeachButtonText;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void ReturnToBeach()
    {
        ResumeGame();
        SceneManager.LoadScene(beachSceneName);
    }
}
