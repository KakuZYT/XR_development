using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Buttons")]
    public Button openGalleryButton;
    public Button returnButton;

    [Header("Scene")]
    public string scene1Name = "01_Beach_StartMenu";

    [Header("Photo Layout")]
    public float thumbnailWidth = 260f;
    public float thumbnailHeight = 150f;
    public float thumbnailSpacing = 20f;
    public int columns = 3;

    [Header("Photo Frame")]
    public float frameBorder = 8f;

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

        SetupScrollRect();
        SetupGalleryContentLayout();
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

        SetupScrollRect();
        SetupGalleryContentLayout();

        ClearOldPhotos();
        LoadPhotosFromImgFolder();
        UpdateContentHeight();

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

        ScrollBy(input * keyboardScrollSpeed * Time.deltaTime);
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

        ScrollBy(y * stickScrollSpeed * Time.deltaTime);

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

        ScrollBy(y * stickScrollSpeed * Time.deltaTime);

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
        SceneManager.LoadScene(scene1Name);
    }

    void LoadPhotosFromImgFolder()
    {
        string folderPath = GetImgFolderPath();

        loadedPhotoCount = 0;

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning("Photo folder does not exist: " + folderPath);
            CreateMessageText("No img folder found.");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.png");

        if (files.Length == 0)
        {
            Debug.Log("No photos found in: " + folderPath);
            CreateMessageText("No photos found.");
            return;
        }

        System.Array.Sort(files);

        for (int i = 0; i < files.Length; i++)
        {
            CreatePhotoThumbnail(files[i]);
        }

        loadedPhotoCount = files.Length;

        Debug.Log("Loaded photos: " + files.Length);
    }

    void CreatePhotoThumbnail(string filePath)
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

        GameObject frameObject = new GameObject("PhotoFrame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(photoContent, false);

        Image frameImage = frameObject.GetComponent<Image>();
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

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

        createdPhotoObjects.Add(frameObject);
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
        text.color = Color.white;

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

        if (grid == null)
        {
            grid = photoContent.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = new Vector2(thumbnailWidth, thumbnailHeight);
        grid.spacing = new Vector2(thumbnailSpacing, thumbnailSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        ContentSizeFitter fitter = photoContent.GetComponent<ContentSizeFitter>();

        if (fitter != null)
        {
            Destroy(fitter);
        }

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

        int safeColumns = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt((float)itemCount / safeColumns);

        float height = rows * thumbnailHeight + Mathf.Max(0, rows - 1) * thumbnailSpacing + 40f;

        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);

        Debug.Log("Gallery content height updated: " + height + ", rows: " + rows + ", photos: " + itemCount);
    }

    string GetImgFolderPath()
    {
        return Path.Combine(Application.dataPath, "img");
    }

    void OnDestroy()
    {
        ClearOldPhotos();
    }
}