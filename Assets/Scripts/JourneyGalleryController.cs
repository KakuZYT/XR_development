using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRInputDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class JourneyGalleryController : MonoBehaviour
{
    [Header("Gallery UI")]
    public GameObject galleryCanvas;
    public Transform photoContent;
    public ScrollRect photoScrollRect;
    public float panelBottomExtraSpace = 90f;

    [Header("Buttons")]
    public Button openGalleryButton;
    public Button returnButton;
    public Button continueButton;

    [Header("Learn More Link")]
    public string learnMoreLinkText = "Know more about animals in Great Barrier Reef";
    public string learnMoreLinkUrl = "https://www2.gbrmpa.gov.au/learn/animals";

    [Header("Scene")]
    public string scene1Name = "01_Beach_StartMenu";

    [Header("Photo Layout")]
    public int maxPhotosToShow = 6;
    public float thumbnailWidth = 260f;
    public float thumbnailHeight = 150f;
    public float thumbnailSpacing = 20f;
    public float infoButtonWidth = 220f;
    public float infoButtonHeight = 74f;

    [Header("Photo Frame")]
    public float frameBorder = 8f;

    [Header("Info Drawer")]
    public float drawerHeight = 180f;
    public int drawerTitleFontSize = 24;
    public int drawerBodyFontSize = 18;

    [Header("Scroll Input")]
    public bool enableMouseWheelScroll = true;
    public bool enableKeyboardScroll = true;
    public bool enableGamepadStickScroll = true;
    public bool enableXRStickScroll = true;

    public float mouseWheelScrollStep = 0.18f;
    public float keyboardScrollSpeed = 0.8f;
    public float stickScrollSpeed = 0.8f;
    public float stickDeadZone = 0.2f;

    [Header("Debug")]
    public bool debugScrollInput = false;

    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();
    private readonly List<GameObject> createdPhotoObjects = new List<GameObject>();

    private int loadedPhotoCount = 0;
    private InputAction mouseScrollAction;
    private GameObject infoDrawer;
    private Text infoDrawerTitleText;
    private Text infoDrawerBodyText;
    private bool galleryPausedGame;
    private float timeScaleBeforeGallery = 1f;
    private bool panelSizeAdjusted;
    private bool buttonsPositionAdjusted;
    private bool scrollViewAdjustedForButtons;
    private Button learnMoreLinkButton;
    private GlobalClickPauseController globalPauseController;

    void Awake()
    {
        mouseScrollAction = new InputAction(
            name: "Mouse Scroll",
            type: InputActionType.Value,
            binding: "<Mouse>/scroll/y"
        );
    }

    void OnEnable()
    {
        if (mouseScrollAction != null)
        {
            mouseScrollAction.Enable();
        }
    }

    void OnDisable()
    {
        if (mouseScrollAction != null)
        {
            mouseScrollAction.Disable();
        }
    }

    void Start()
    {
        if (galleryCanvas != null)
        {
            galleryCanvas.SetActive(false);
        }

        if (openGalleryButton != null)
        {
            openGalleryButton.onClick.RemoveAllListeners();
            openGalleryButton.onClick.AddListener(OpenGallery);
        }
        else
        {
            Debug.LogWarning("Open Gallery Button is not assigned.");
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(ReturnToScene1);
        }
        else
        {
            Debug.LogWarning("Return Button is not assigned.");
        }

        SetupContinueButton();
        SetupGlobalPauseController();
        SetupGalleryPanelSize();
        SetupScrollRect();
        SetupGalleryContentLayout();
        SetupInfoDrawer();
        ApplyGalleryTextColors();
    }

    void Update()
    {
        if (galleryCanvas == null || !galleryCanvas.activeSelf)
        {
            return;
        }

        HandleMouseWheelScroll();
        HandleKeyboardScroll();
        HandleGamepadStickScroll();
        HandleXRStickScroll();
    }

    public void OpenGallery()
    {
        if (galleryCanvas == null)
        {
            Debug.LogWarning("Gallery Canvas is not assigned.");
            return;
        }

        galleryCanvas.SetActive(true);
        if (globalPauseController != null)
        {
            globalPauseController.ResumeIfPaused();
        }

        PauseGameForGallery();

        SetupGalleryPanelSize();
        SetupScrollRect();
        SetupGalleryContentLayout();
        SetupInfoDrawer();
        HideInfoDrawer();

        ClearOldPhotos();
        LoadPhotosFromImgFolder();
        UpdateContentHeight();
        ApplyGalleryTextColors();

        Canvas.ForceUpdateCanvases();

        if (photoScrollRect != null)
        {
            photoScrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log("Gallery opened.");
    }

    public void CloseGallery()
    {
        if (galleryCanvas != null)
        {
            galleryCanvas.SetActive(false);
        }

        ResumeGameFromGallery();
    }

    void SetupScrollRect()
    {
        if (photoScrollRect == null)
        {
            Debug.LogWarning("Photo Scroll Rect is not assigned.");
            return;
        }

        photoScrollRect.horizontal = false;
        photoScrollRect.vertical = true;
        photoScrollRect.movementType = ScrollRect.MovementType.Clamped;
        photoScrollRect.inertia = true;
        photoScrollRect.scrollSensitivity = 40f;

        ReserveBottomButtonArea();

        if (photoContent != null)
        {
            RectTransform contentRect = photoContent.GetComponent<RectTransform>();

            if (contentRect != null)
            {
                photoScrollRect.content = contentRect;
            }
        }
    }

    void HandleMouseWheelScroll()
    {
        if (!enableMouseWheelScroll)
        {
            return;
        }

        if (photoScrollRect == null || mouseScrollAction == null)
        {
            return;
        }

        float scrollValue = mouseScrollAction.ReadValue<float>();

        if (Mathf.Abs(scrollValue) < 0.01f)
        {
            return;
        }

        float direction = scrollValue > 0f ? 1f : -1f;

        ScrollBy(direction * mouseWheelScrollStep);

        if (debugScrollInput)
        {
            Debug.Log("Mouse wheel input detected: " + scrollValue);
        }
    }

    void HandleKeyboardScroll()
    {
        if (!enableKeyboardScroll)
        {
            return;
        }

        if (photoScrollRect == null || Keyboard.current == null)
        {
            return;
        }

        float input = 0f;

        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.pageUpKey.isPressed)
        {
            input += 1f;
        }

        if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed || Keyboard.current.pageDownKey.isPressed)
        {
            input -= 1f;
        }

        if (Mathf.Abs(input) < 0.01f)
        {
            return;
        }

        ScrollBy(input * keyboardScrollSpeed * Time.unscaledDeltaTime);
    }

    void HandleGamepadStickScroll()
    {
        if (!enableGamepadStickScroll)
        {
            return;
        }

        if (photoScrollRect == null || Gamepad.current == null)
        {
            return;
        }

        Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
        Vector2 rightStick = Gamepad.current.rightStick.ReadValue();

        float y = Mathf.Abs(rightStick.y) > Mathf.Abs(leftStick.y) ? rightStick.y : leftStick.y;

        if (Mathf.Abs(y) < stickDeadZone)
        {
            return;
        }

        ScrollBy(y * stickScrollSpeed * Time.unscaledDeltaTime);

        if (debugScrollInput)
        {
            Debug.Log("Gamepad stick scroll input detected: " + y);
        }
    }

    void HandleXRStickScroll()
    {
        if (!enableXRStickScroll)
        {
            return;
        }

        if (photoScrollRect == null)
        {
            return;
        }

        float y = GetXRPrimary2DAxisY();

        if (Mathf.Abs(y) < stickDeadZone)
        {
            return;
        }

        ScrollBy(y * stickScrollSpeed * Time.unscaledDeltaTime);

        if (debugScrollInput)
        {
            Debug.Log("XR stick scroll input detected: " + y);
        }
    }

    float GetXRPrimary2DAxisY()
    {
        float result = 0f;

        List<XRInputDevice> devices = new List<XRInputDevice>();

        XRInputDevices.GetDevicesWithCharacteristics(
            XRInputDeviceCharacteristics.Left | XRInputDeviceCharacteristics.Controller,
            devices
        );

        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 axis))
            {
                if (Mathf.Abs(axis.y) > Mathf.Abs(result))
                {
                    result = axis.y;
                }
            }
        }

        devices.Clear();

        XRInputDevices.GetDevicesWithCharacteristics(
            XRInputDeviceCharacteristics.Right | XRInputDeviceCharacteristics.Controller,
            devices
        );

        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 axis))
            {
                if (Mathf.Abs(axis.y) > Mathf.Abs(result))
                {
                    result = axis.y;
                }
            }
        }

        return result;
    }

    void ScrollBy(float amount)
    {
        if (photoScrollRect == null)
        {
            return;
        }

        photoScrollRect.verticalNormalizedPosition += amount;
        photoScrollRect.verticalNormalizedPosition = Mathf.Clamp01(photoScrollRect.verticalNormalizedPosition);
    }

    public void ReturnToScene1()
    {
        if (string.IsNullOrEmpty(scene1Name))
        {
            Debug.LogWarning("Scene 1 name is empty.");
            return;
        }

        Debug.Log("Trying to load scene: " + scene1Name);
        ResumeGameFromGallery();
        SceneManager.LoadScene(scene1Name);
    }

    void PauseGameForGallery()
    {
        if (!galleryPausedGame)
        {
            timeScaleBeforeGallery = Time.timeScale;
            galleryPausedGame = true;
        }

        Time.timeScale = 0f;
    }

    void ResumeGameFromGallery()
    {
        if (!galleryPausedGame)
        {
            return;
        }

        Time.timeScale = timeScaleBeforeGallery <= 0f ? 1f : timeScaleBeforeGallery;
        galleryPausedGame = false;
    }

    void SetupContinueButton()
    {
        RectTransform returnRect = returnButton != null ? returnButton.GetComponent<RectTransform>() : null;
        ApplyButtonTint(returnButton, new Color(0.62f, 0.84f, 1f, 0.9f), new Color(0.08f, 0.16f, 0.2f, 1f));

        if (continueButton != null)
        {
            PositionGalleryButtons(returnRect, continueButton.GetComponent<RectTransform>());
            SetupLearnMoreLink(returnRect);
            ApplyButtonTint(continueButton, new Color(0.58f, 0.9f, 0.62f, 0.92f), new Color(0.06f, 0.18f, 0.08f, 1f));
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CloseGallery);
            continueButton.gameObject.SetActive(true);
            return;
        }

        if (returnButton == null)
        {
            return;
        }

        GameObject continueObject = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        continueObject.transform.SetParent(returnButton.transform.parent, false);

        RectTransform continueRect = continueObject.GetComponent<RectTransform>();

        PositionGalleryButtons(returnRect, continueRect);
        SetupLearnMoreLink(returnRect);

        Image returnImage = returnButton.GetComponent<Image>();
        Image continueImage = continueObject.GetComponent<Image>();

        if (returnImage != null)
        {
            continueImage.sprite = returnImage.sprite;
            continueImage.type = returnImage.type;
        }

        continueImage.color = new Color(0.58f, 0.9f, 0.62f, 0.92f);

        continueButton = continueObject.GetComponent<Button>();
        continueButton.targetGraphic = continueImage;
        continueButton.onClick.AddListener(CloseGallery);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(continueObject.transform, false);

        Text label = labelObject.GetComponent<Text>();
        label.text = "Continue";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.06f, 0.18f, 0.08f, 1f);
        label.raycastTarget = false;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.localScale = Vector3.one;
    }

    void SetupGalleryPanelSize()
    {
        // Keep the world-space GalleryCanvas transform stable; moving it here can push the panel out of view.
        panelSizeAdjusted = true;
    }

    void PositionGalleryButtons(RectTransform returnRect, RectTransform continueRect)
    {
        if (returnRect == null)
        {
            return;
        }

        if (!buttonsPositionAdjusted)
        {
            returnRect.anchoredPosition += new Vector2(0f, 52f);
            buttonsPositionAdjusted = true;
        }

        if (continueRect == null)
        {
            return;
        }

        float returnHeight = returnRect.rect.height > 1f ? returnRect.rect.height : Mathf.Abs(returnRect.sizeDelta.y);
        if (returnHeight < 1f)
        {
            returnHeight = 44f;
        }

        continueRect.anchorMin = returnRect.anchorMin;
        continueRect.anchorMax = returnRect.anchorMax;
        continueRect.pivot = returnRect.pivot;
        continueRect.sizeDelta = returnRect.sizeDelta;
        continueRect.anchoredPosition = returnRect.anchoredPosition + new Vector2(0f, -returnHeight - 8f);
        continueRect.localScale = Vector3.one;
    }

    void SetupLearnMoreLink(RectTransform returnRect)
    {
        if (returnRect == null || returnButton == null)
        {
            return;
        }

        if (learnMoreLinkButton != null)
        {
            PositionLearnMoreLink(returnRect, learnMoreLinkButton.GetComponent<RectTransform>());
            learnMoreLinkButton.onClick.RemoveAllListeners();
            learnMoreLinkButton.onClick.AddListener(OpenLearnMoreLink);
            return;
        }

        GameObject linkObject = new GameObject("GreatBarrierReefAnimalsLink", typeof(RectTransform), typeof(Text), typeof(Button));
        linkObject.transform.SetParent(returnButton.transform.parent, false);

        Text linkText = linkObject.GetComponent<Text>();
        linkText.text = learnMoreLinkText;
        linkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        linkText.fontSize = 18;
        linkText.fontStyle = FontStyle.Bold;
        linkText.alignment = TextAnchor.MiddleCenter;
        linkText.color = new Color(0.04f, 0.42f, 0.15f, 1f);
        linkText.horizontalOverflow = HorizontalWrapMode.Wrap;
        linkText.verticalOverflow = VerticalWrapMode.Truncate;
        linkText.raycastTarget = true;

        learnMoreLinkButton = linkObject.GetComponent<Button>();
        learnMoreLinkButton.targetGraphic = linkText;
        learnMoreLinkButton.onClick.AddListener(OpenLearnMoreLink);

        RectTransform linkRect = linkObject.GetComponent<RectTransform>();
        PositionLearnMoreLink(returnRect, linkRect);

        GameObject underlineObject = new GameObject("Underline", typeof(RectTransform), typeof(Image));
        underlineObject.transform.SetParent(linkObject.transform, false);

        Image underlineImage = underlineObject.GetComponent<Image>();
        underlineImage.color = new Color(0.04f, 0.42f, 0.15f, 0.85f);
        underlineImage.raycastTarget = false;

        RectTransform underlineRect = underlineObject.GetComponent<RectTransform>();
        underlineRect.anchorMin = new Vector2(0.08f, 0f);
        underlineRect.anchorMax = new Vector2(0.92f, 0f);
        underlineRect.pivot = new Vector2(0.5f, 0f);
        underlineRect.sizeDelta = new Vector2(0f, 2f);
        underlineRect.anchoredPosition = new Vector2(0f, 3f);
        underlineRect.localScale = Vector3.one;
    }

    void PositionLearnMoreLink(RectTransform returnRect, RectTransform linkRect)
    {
        if (returnRect == null || linkRect == null)
        {
            return;
        }

        linkRect.anchorMin = returnRect.anchorMin;
        linkRect.anchorMax = returnRect.anchorMax;
        linkRect.pivot = returnRect.pivot;
        linkRect.sizeDelta = new Vector2(430f, 30f);
        linkRect.anchoredPosition = returnRect.anchoredPosition + new Vector2(0f, 58f);
        linkRect.localScale = Vector3.one;
    }

    void OpenLearnMoreLink()
    {
        Application.OpenURL(learnMoreLinkUrl);
    }

    void ApplyButtonTint(Button button, Color buttonColor, Color labelColor)
    {
        if (button == null)
        {
            return;
        }

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = buttonColor;
        }

        Text buttonText = button.GetComponentInChildren<Text>(true);
        if (buttonText != null)
        {
            buttonText.color = labelColor;
        }
    }

    void ApplyGalleryTextColors()
    {
        if (galleryCanvas == null)
        {
            return;
        }

        Text[] uiTexts = galleryCanvas.GetComponentsInChildren<Text>(true);
        foreach (Text uiText in uiTexts)
        {
            if (uiText == null || IsLearnMoreLinkText(uiText.transform))
            {
                continue;
            }

            uiText.color = Color.black;
        }

        TMP_Text[] tmpTexts = galleryCanvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text tmpText in tmpTexts)
        {
            if (tmpText == null || IsLearnMoreLinkText(tmpText.transform))
            {
                continue;
            }

            tmpText.color = Color.black;
        }

        if (learnMoreLinkButton != null)
        {
            Text linkText = learnMoreLinkButton.GetComponent<Text>();
            if (linkText != null)
            {
                linkText.color = new Color(0.04f, 0.42f, 0.15f, 1f);
            }
        }
    }

    bool IsLearnMoreLinkText(Transform textTransform)
    {
        return learnMoreLinkButton != null
            && textTransform != null
            && textTransform.IsChildOf(learnMoreLinkButton.transform);
    }

    void ReserveBottomButtonArea()
    {
        if (scrollViewAdjustedForButtons || photoScrollRect == null)
        {
            return;
        }

        RectTransform scrollRectTransform = photoScrollRect.GetComponent<RectTransform>();
        if (scrollRectTransform == null)
        {
            return;
        }

        const float reservedButtonSpace = 90f;
        scrollRectTransform.sizeDelta = new Vector2(
            scrollRectTransform.sizeDelta.x,
            Mathf.Max(180f, scrollRectTransform.sizeDelta.y - reservedButtonSpace)
        );
        scrollRectTransform.anchoredPosition += new Vector2(0f, reservedButtonSpace * 0.5f);
        scrollViewAdjustedForButtons = true;
    }

    void SetupGlobalPauseController()
    {
        GlobalClickPauseController pauseController = GlobalClickPauseController.Instance;

        globalPauseController = pauseController;
        pauseController.SetIgnoredGallery(galleryCanvas);
    }

    void LoadPhotosFromImgFolder()
    {
        loadedPhotoCount = 0;

        string[] files = GetPhotoFiles();

        if (files.Length == 0)
        {
            Debug.Log("No photos found.");
            CreateMessageText("No photos found.");
            return;
        }

        int photoLimit = Mathf.Min(files.Length, Mathf.Max(0, maxPhotosToShow));

        for (int i = 0; i < photoLimit; i++)
        {
            CreatePhotoItem(files[i], i + 1);
        }

        loadedPhotoCount = photoLimit;

        Debug.Log("Loaded photos: " + photoLimit + " of " + files.Length);
    }

    string[] GetPhotoFiles()
    {
        List<string> files = new List<string>();
        string[] folderPaths = GetImgFolderPaths();

        for (int i = 0; i < folderPaths.Length; i++)
        {
            string folderPath = folderPaths[i];

            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            files.AddRange(Directory.GetFiles(folderPath, "*.png"));
        }

        files.Sort((left, right) =>
        {
            System.DateTime leftTime = GetPhotoTime(left);
            System.DateTime rightTime = GetPhotoTime(right);

            int timeCompare = rightTime.CompareTo(leftTime);

            if (timeCompare != 0)
            {
                return timeCompare;
            }

            return string.Compare(right, left, System.StringComparison.OrdinalIgnoreCase);
        });

        return files.ToArray();
    }

    void CreatePhotoItem(string filePath, int photoNumber)
    {
        if (photoContent == null)
        {
            Debug.LogWarning("Photo Content is not assigned.");
            return;
        }

        byte[] imageData = File.ReadAllBytes(filePath);

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        bool loaded = texture.LoadImage(imageData);

        if (!loaded)
        {
            Debug.LogWarning("Failed to load image: " + filePath);
            Destroy(texture);
            return;
        }

        loadedTextures.Add(texture);

        GameObject itemObject = new GameObject("PhotoItem", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        itemObject.transform.SetParent(photoContent, false);

        HorizontalLayoutGroup itemLayout = itemObject.GetComponent<HorizontalLayoutGroup>();
        itemLayout.childAlignment = TextAnchor.MiddleLeft;
        itemLayout.childControlWidth = true;
        itemLayout.childControlHeight = true;
        itemLayout.childForceExpandWidth = false;
        itemLayout.childForceExpandHeight = false;
        itemLayout.spacing = thumbnailSpacing;
        itemLayout.padding = new RectOffset(0, 0, 0, 0);

        LayoutElement itemLayoutElement = itemObject.GetComponent<LayoutElement>();
        itemLayoutElement.minHeight = thumbnailHeight;
        itemLayoutElement.preferredHeight = thumbnailHeight;
        itemLayoutElement.flexibleWidth = 1f;

        GameObject frameObject = new GameObject("PhotoFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        frameObject.transform.SetParent(itemObject.transform, false);

        Image frameImage = frameObject.GetComponent<Image>();
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        LayoutElement frameLayout = frameObject.GetComponent<LayoutElement>();
        frameLayout.minWidth = thumbnailWidth;
        frameLayout.preferredWidth = thumbnailWidth;
        frameLayout.minHeight = thumbnailHeight;
        frameLayout.preferredHeight = thumbnailHeight;

        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.sizeDelta = new Vector2(thumbnailWidth, thumbnailHeight);
        frameRect.localScale = Vector3.one;

        GameObject imageObject = new GameObject("PhotoImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(frameObject.transform, false);

        Image photoImage = imageObject.GetComponent<Image>();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        photoImage.sprite = sprite;
        photoImage.color = Color.white;
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(frameBorder, frameBorder);
        imageRect.offsetMax = new Vector2(-frameBorder, -frameBorder);
        imageRect.localScale = Vector3.one;

        float compactInfoColumnWidth = Mathf.Clamp(infoButtonWidth, 150f, 180f);
        float compactInfoButtonWidth = Mathf.Clamp(infoButtonWidth * 0.62f, 96f, 118f);
        float compactInfoButtonHeight = Mathf.Clamp(infoButtonHeight, 30f, 36f);

        GameObject infoColumnObject = new GameObject("InfoColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        infoColumnObject.transform.SetParent(itemObject.transform, false);

        VerticalLayoutGroup infoColumnLayout = infoColumnObject.GetComponent<VerticalLayoutGroup>();
        infoColumnLayout.childAlignment = TextAnchor.UpperCenter;
        infoColumnLayout.childControlWidth = true;
        infoColumnLayout.childControlHeight = true;
        infoColumnLayout.childForceExpandWidth = false;
        infoColumnLayout.childForceExpandHeight = false;
        infoColumnLayout.spacing = 8f;
        infoColumnLayout.padding = new RectOffset(0, 0, 16, 8);

        LayoutElement infoColumnElement = infoColumnObject.GetComponent<LayoutElement>();
        infoColumnElement.minWidth = compactInfoColumnWidth;
        infoColumnElement.preferredWidth = compactInfoColumnWidth;
        infoColumnElement.minHeight = thumbnailHeight;
        infoColumnElement.preferredHeight = thumbnailHeight;

        string infoTitle = GetPhotoTitle(filePath);

        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        titleObject.transform.SetParent(infoColumnObject.transform, false);

        Text titleLabel = titleObject.GetComponent<Text>();
        titleLabel.text = infoTitle;
        titleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleLabel.fontSize = 18;
        titleLabel.fontStyle = FontStyle.Bold;
        titleLabel.alignment = TextAnchor.MiddleCenter;
        titleLabel.color = Color.black;
        titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleLabel.verticalOverflow = VerticalWrapMode.Truncate;
        titleLabel.raycastTarget = false;

        LayoutElement titleLayout = titleObject.GetComponent<LayoutElement>();
        titleLayout.minWidth = compactInfoColumnWidth;
        titleLayout.preferredWidth = compactInfoColumnWidth;
        titleLayout.minHeight = 44f;
        titleLayout.preferredHeight = 44f;
        titleLayout.flexibleWidth = 0f;
        titleLayout.flexibleHeight = 0f;

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(compactInfoColumnWidth, 44f);
        titleRect.localScale = Vector3.one;

        GameObject infoButtonObject = new GameObject("InfoButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        infoButtonObject.transform.SetParent(infoColumnObject.transform, false);

        Image infoButtonImage = infoButtonObject.GetComponent<Image>();
        infoButtonImage.color = new Color(0.72f, 0.94f, 0.74f, 0.92f);
        infoButtonImage.raycastTarget = true;

        Button infoButton = infoButtonObject.GetComponent<Button>();
        infoButton.targetGraphic = infoButtonImage;
        infoButton.onClick.AddListener(() => ShowInfoDrawer(filePath, itemObject.transform));

        LayoutElement infoButtonLayout = infoButtonObject.GetComponent<LayoutElement>();
        infoButtonLayout.minWidth = compactInfoButtonWidth;
        infoButtonLayout.preferredWidth = compactInfoButtonWidth;
        infoButtonLayout.minHeight = compactInfoButtonHeight;
        infoButtonLayout.preferredHeight = compactInfoButtonHeight;
        infoButtonLayout.flexibleWidth = 0f;
        infoButtonLayout.flexibleHeight = 0f;

        RectTransform infoButtonRect = infoButtonObject.GetComponent<RectTransform>();
        infoButtonRect.sizeDelta = new Vector2(compactInfoButtonWidth, compactInfoButtonHeight);

        GameObject infoLabelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        infoLabelObject.transform.SetParent(infoButtonObject.transform, false);

        Text infoLabel = infoLabelObject.GetComponent<Text>();
        infoLabel.text = "Info";
        infoLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoLabel.fontSize = 18;
        infoLabel.fontStyle = FontStyle.Bold;
        infoLabel.alignment = TextAnchor.MiddleCenter;
        infoLabel.color = new Color(0.05f, 0.18f, 0.08f, 1f);
        infoLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        infoLabel.verticalOverflow = VerticalWrapMode.Truncate;
        infoLabel.raycastTarget = false;

        RectTransform infoLabelRect = infoLabelObject.GetComponent<RectTransform>();
        infoLabelRect.anchorMin = Vector2.zero;
        infoLabelRect.anchorMax = Vector2.one;
        infoLabelRect.offsetMin = Vector2.zero;
        infoLabelRect.offsetMax = Vector2.zero;
        infoLabelRect.localScale = Vector3.one;

        createdPhotoObjects.Add(itemObject);
    }

    void SetupInfoDrawer()
    {
        if (galleryCanvas == null)
        {
            return;
        }

        if (infoDrawer != null)
        {
            return;
        }

        if (photoContent == null)
        {
            return;
        }

        infoDrawer = new GameObject("PhotoInfoDrawer", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        infoDrawer.transform.SetParent(photoContent, false);

        LayoutElement drawerLayout = infoDrawer.GetComponent<LayoutElement>();
        drawerLayout.minHeight = drawerHeight;
        drawerLayout.preferredHeight = drawerHeight;
        drawerLayout.flexibleWidth = 1f;

        RectTransform drawerRect = infoDrawer.GetComponent<RectTransform>();
        drawerRect.anchorMin = new Vector2(0f, 1f);
        drawerRect.anchorMax = new Vector2(1f, 1f);
        drawerRect.pivot = new Vector2(0.5f, 1f);
        drawerRect.sizeDelta = new Vector2(0f, drawerHeight);
        drawerRect.localScale = Vector3.one;

        Image drawerImage = infoDrawer.GetComponent<Image>();
        drawerImage.color = new Color(0.86f, 0.94f, 0.9f, 0.96f);
        drawerImage.raycastTarget = true;

        Button drawerButton = infoDrawer.GetComponent<Button>();
        drawerButton.targetGraphic = drawerImage;
        drawerButton.onClick.AddListener(HideInfoDrawer);

        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(infoDrawer.transform, false);

        infoDrawerTitleText = titleObject.GetComponent<Text>();
        infoDrawerTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoDrawerTitleText.fontSize = drawerTitleFontSize;
        infoDrawerTitleText.fontStyle = FontStyle.Bold;
        infoDrawerTitleText.alignment = TextAnchor.MiddleLeft;
        infoDrawerTitleText.color = Color.black;
        infoDrawerTitleText.raycastTarget = false;

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -52f);
        titleRect.offsetMax = new Vector2(-64f, -14f);
        titleRect.localScale = Vector3.one;

        GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Text));
        bodyObject.transform.SetParent(infoDrawer.transform, false);

        infoDrawerBodyText = bodyObject.GetComponent<Text>();
        infoDrawerBodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoDrawerBodyText.fontSize = drawerBodyFontSize;
        infoDrawerBodyText.alignment = TextAnchor.UpperLeft;
        infoDrawerBodyText.color = Color.black;
        infoDrawerBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        infoDrawerBodyText.verticalOverflow = VerticalWrapMode.Truncate;
        infoDrawerBodyText.raycastTarget = false;

        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 20f);
        bodyRect.offsetMax = new Vector2(-24f, -58f);
        bodyRect.localScale = Vector3.one;

        GameObject closeObject = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(infoDrawer.transform, false);

        Image closeImage = closeObject.GetComponent<Image>();
        closeImage.color = new Color(1f, 1f, 1f, 0.55f);
        closeImage.raycastTarget = true;

        Button closeButton = closeObject.GetComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(HideInfoDrawer);

        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(42f, 34f);
        closeRect.anchoredPosition = new Vector2(-16f, -14f);
        closeRect.localScale = Vector3.one;

        GameObject closeTextObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        closeTextObject.transform.SetParent(closeObject.transform, false);

        Text closeText = closeTextObject.GetComponent<Text>();
        closeText.text = "X";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 22;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.black;
        closeText.raycastTarget = false;

        RectTransform closeTextRect = closeTextObject.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
        closeTextRect.localScale = Vector3.one;

        HideInfoDrawer();
    }

    void ShowInfoDrawer(string filePath, Transform clickedItem)
    {
        SetupInfoDrawer();

        if (infoDrawer == null)
        {
            return;
        }

        string title = GetPhotoTitle(filePath);
        string body = GetPhotoBody(filePath);

        if (infoDrawerTitleText != null)
        {
            infoDrawerTitleText.text = title;
        }

        if (infoDrawerBodyText != null)
        {
            infoDrawerBodyText.text = body;
        }

        if (clickedItem != null && clickedItem.parent == photoContent)
        {
            int clickedIndex = clickedItem.GetSiblingIndex();
            int drawerIndex = infoDrawer.transform.GetSiblingIndex();

            if (drawerIndex < clickedIndex)
            {
                clickedIndex--;
            }

            infoDrawer.transform.SetSiblingIndex(clickedIndex + 1);
        }

        infoDrawer.SetActive(true);
        UpdateContentHeight();

        Canvas.ForceUpdateCanvases();
    }

    void HideInfoDrawer()
    {
        if (infoDrawer != null)
        {
            infoDrawer.SetActive(false);
            UpdateContentHeight();
        }
    }

    string GetPhotoTitle(string filePath)
    {
        string description = GetStoredPhotoDescription(filePath);

        if (!string.IsNullOrEmpty(description))
        {
            string[] lines = description.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (!string.IsNullOrEmpty(line))
                {
                    return line;
                }
            }
        }

        return "Photo Details";
    }

    string GetPhotoBody(string filePath)
    {
        string description = GetStoredPhotoDescription(filePath);

        if (!string.IsNullOrEmpty(description))
        {
            string[] lines = description.Split('\n');
            List<string> bodyLines = new List<string>();
            bool skippedTitle = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (!skippedTitle)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        skippedTitle = true;
                    }

                    continue;
                }

                bodyLines.Add(line);
            }

            string body = string.Join("\n", bodyLines).Trim();

            if (!string.IsNullOrEmpty(body))
            {
                return body;
            }
        }

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return GetCapturedTimeText(fileName);
    }

    string GetStoredPhotoDescription(string filePath)
    {
        string descriptionPath = Path.ChangeExtension(filePath, ".txt");

        if (!File.Exists(descriptionPath))
        {
            return string.Empty;
        }

        return File.ReadAllText(descriptionPath).Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }

    string GetPhotoDescription(string filePath, int photoNumber)
    {
        string description = GetStoredPhotoDescription(filePath);

        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string capturedTime = GetCapturedTimeText(fileName);

        return capturedTime;
    }

    System.DateTime GetPhotoTime(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (TryGetPhotoTimeFromName(fileName, out System.DateTime photoTime))
        {
            return photoTime;
        }

        return File.GetLastWriteTime(filePath);
    }

    string GetCapturedTimeText(string fileName)
    {
        if (TryGetPhotoTimeFromName(fileName, out System.DateTime photoTime))
        {
            return "Captured: " + photoTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return fileName;
    }

    bool TryGetPhotoTimeFromName(string fileName, out System.DateTime photoTime)
    {
        photoTime = System.DateTime.MinValue;

        const string prefix = "Photo_";

        if (!fileName.StartsWith(prefix) || fileName.Length < 21)
        {
            return false;
        }

        string datePart = fileName.Substring(6, 8);
        string timePart = fileName.Substring(15, 6);
        string timestamp = datePart + timePart;

        return System.DateTime.TryParseExact(
            timestamp,
            "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out photoTime
        );
    }

    void CreateMessageText(string message)
    {
        if (photoContent == null)
        {
            return;
        }

        GameObject textObject = new GameObject("MessageText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(photoContent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600f, 100f);
        rect.localScale = Vector3.one;

        createdPhotoObjects.Add(textObject);
    }

    void ClearOldPhotos()
    {
        for (int i = 0; i < createdPhotoObjects.Count; i++)
        {
            if (createdPhotoObjects[i] != null)
            {
                Destroy(createdPhotoObjects[i]);
            }
        }

        createdPhotoObjects.Clear();

        for (int i = 0; i < loadedTextures.Count; i++)
        {
            if (loadedTextures[i] != null)
            {
                Destroy(loadedTextures[i]);
            }
        }

        loadedTextures.Clear();
        loadedPhotoCount = 0;
    }

    void SetupGalleryContentLayout()
    {
        if (photoContent == null)
        {
            Debug.LogWarning("Photo Content is not assigned.");
            return;
        }

        GridLayoutGroup grid = photoContent.GetComponent<GridLayoutGroup>();

        if (grid != null)
        {
            Destroy(grid);
        }

        VerticalLayoutGroup verticalLayout = photoContent.GetComponent<VerticalLayoutGroup>();

        if (verticalLayout == null)
        {
            verticalLayout = photoContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.spacing = thumbnailSpacing;
        verticalLayout.padding = new RectOffset(20, 20, 20, 20);

        ContentSizeFitter fitter = photoContent.GetComponent<ContentSizeFitter>();

        if (fitter == null)
        {
            fitter = photoContent.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform contentRect = photoContent.GetComponent<RectTransform>();

        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
        }
    }

    void UpdateContentHeight()
    {
        if (photoContent == null)
        {
            return;
        }

        RectTransform contentRect = photoContent.GetComponent<RectTransform>();

        if (contentRect == null)
        {
            return;
        }

        int itemCount = Mathf.Max(loadedPhotoCount, createdPhotoObjects.Count);

        if (itemCount <= 0)
        {
            itemCount = 1;
        }

        float height = itemCount * thumbnailHeight + Mathf.Max(0, itemCount - 1) * thumbnailSpacing + 40f;

        if (infoDrawer != null && infoDrawer.activeSelf)
        {
            height += drawerHeight + thumbnailSpacing;
        }

        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);

        Debug.Log("Gallery content height updated: " + height + ", photos: " + itemCount);
    }

    string[] GetImgFolderPaths()
    {
        return new string[]
        {
            Path.Combine(Application.persistentDataPath, "img"),
            Path.Combine(Application.dataPath, "img")
        };
    }

    void OnDestroy()
    {
        ClearOldPhotos();
    }
}
