using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PhotoSystem : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("UI")]
    public GameObject photoCanvas;
    public Button takePhotoButton;

    [Header("Photo Effects")]
    public Image flashImage;
    public GameObject photoPreviewPanel;
    public Image photoPreviewImage;

    [Header("Detection")]
    public float detectDistance = 50f;
    public float detectRadius = 0.35f;
    public string targetTag = "PhotoTarget";
    public bool allowObjectsWithInfoData = true;
    public LayerMask detectionMask = ~0;

    [Header("Preview Settings")]
    public float previewWidth = 320f;
    public float previewBorderSize = 16f;
    public float previewMarginRight = 0f;
    public float previewMarginBottom = 0f;
    public float previewShowTime = 3f;

    private GameObject currentTarget;
    private ObjectInfoData currentTargetInfo;
    private bool canTakePhoto = false;
    private bool isTakingPhoto = false;

    private Coroutine flashCoroutine;
    private Coroutine previewCoroutine;

    private Texture2D currentScreenshot;
    private Sprite currentPreviewSprite;

    void Start()
    {
        if (photoCanvas != null)
        {
            photoCanvas.SetActive(false);
        }

        if (photoPreviewPanel != null)
        {
            photoPreviewPanel.SetActive(false);
        }

        SetupFlashImage();
        SetupPreviewUI();

        if (takePhotoButton != null)
        {
            takePhotoButton.onClick.RemoveAllListeners();
            takePhotoButton.onClick.AddListener(TakePhoto);
        }

        CreateImgFolderIfNeeded();
    }

    void Update()
    {
        DetectTarget();
    }

    void DetectTarget()
    {
        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        Debug.DrawRay(ray.origin, ray.direction * detectDistance, Color.red);

        RaycastHit[] hits = Physics.SphereCastAll(ray, detectRadius, detectDistance, detectionMask);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            GameObject hitObject = hit.collider.gameObject;
            ObjectInfoData hitInfoData = FindObjectInfoData(hitObject);

            Debug.Log($"Photo check hit: {hitObject.name}, Tag: {hitObject.tag}, Layer: {LayerMask.LayerToName(hitObject.layer)}");

            if (hitObject.CompareTag(targetTag) || (allowObjectsWithInfoData && hitInfoData != null))
            {
                currentTarget = hitInfoData != null ? hitInfoData.gameObject : hitObject;
                currentTargetInfo = hitInfoData;
                canTakePhoto = true;

                if (photoCanvas != null && !photoCanvas.activeSelf)
                {
                    photoCanvas.SetActive(true);
                }

                if (takePhotoButton != null && !isTakingPhoto)
                {
                    takePhotoButton.gameObject.SetActive(true);
                }

                return;
            }
        }

        currentTarget = null;
        currentTargetInfo = null;
        canTakePhoto = false;

        bool previewIsShowing = photoPreviewPanel != null && photoPreviewPanel.activeSelf;

        if (takePhotoButton != null)
        {
            takePhotoButton.gameObject.SetActive(false);
        }

        if (photoCanvas != null && !isTakingPhoto && !previewIsShowing)
        {
            photoCanvas.SetActive(false);
        }
    }

    ObjectInfoData FindObjectInfoData(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return null;
        }

        ObjectInfoData infoData = hitObject.GetComponentInParent<ObjectInfoData>();

        if (infoData != null)
        {
            return infoData;
        }

        return hitObject.GetComponentInChildren<ObjectInfoData>();
    }

    public void TakePhoto()
    {
        if (!canTakePhoto || isTakingPhoto || currentTarget == null)
        {
            return;
        }

        StartCoroutine(CapturePhoto());
    }

    IEnumerator CapturePhoto()
    {
        isTakingPhoto = true;

        if (takePhotoButton != null)
        {
            takePhotoButton.gameObject.SetActive(false);
        }

        yield return new WaitForEndOfFrame();

        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
        }

        GameObject photographedTarget = currentTarget;
        ObjectInfoData photographedInfo = currentTargetInfo;

        currentScreenshot = ScreenCapture.CaptureScreenshotAsTexture();

        SavePhotoToImgFolder(currentScreenshot, photographedTarget, photographedInfo);
        ShowPhotoPreview(currentScreenshot);

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashEffect());

        yield return new WaitForSeconds(0.3f);

        isTakingPhoto = false;

        if (canTakePhoto && takePhotoButton != null)
        {
            takePhotoButton.gameObject.SetActive(true);
        }
    }

    void SavePhotoToImgFolder(Texture2D texture, GameObject photographedTarget, ObjectInfoData photographedInfo)
    {
        byte[] pngData = texture.EncodeToPNG();

        string folderPath = GetImgFolderPath();

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = "Photo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(folderPath, fileName);

        File.WriteAllBytes(filePath, pngData);
        SavePhotoDescription(filePath, photographedTarget, photographedInfo);
    }

    void SavePhotoDescription(string photoPath, GameObject photographedTarget, ObjectInfoData photographedInfo)
    {
        string descriptionPath = Path.ChangeExtension(photoPath, ".txt");
        string descriptionText = GetTargetDescription(photographedTarget, photographedInfo);

        File.WriteAllText(descriptionPath, descriptionText);
    }

    string GetTargetDescription(GameObject photographedTarget, ObjectInfoData photographedInfo)
    {
        if (photographedTarget == null)
        {
            return "Unknown target";
        }

        ObjectInfoData infoData = photographedInfo;

        if (infoData == null)
        {
            infoData = photographedTarget.GetComponentInParent<ObjectInfoData>();
        }

        if (infoData == null)
        {
            infoData = photographedTarget.GetComponentInChildren<ObjectInfoData>();
        }

        if (infoData == null)
        {
            return photographedTarget.name;
        }

        string title = string.IsNullOrWhiteSpace(infoData.objectName) ? photographedTarget.name : infoData.objectName;
        string description = string.IsNullOrWhiteSpace(infoData.description) ? "No description available." : infoData.description;

        return title + "\n\n" + description;
    }

    void ShowPhotoPreview(Texture2D texture)
    {
        if (photoCanvas != null && !photoCanvas.activeSelf)
        {
            photoCanvas.SetActive(true);
        }

        if (photoPreviewPanel == null || photoPreviewImage == null)
        {
            return;
        }

        SetupPreviewUI();

        if (currentPreviewSprite != null)
        {
            Destroy(currentPreviewSprite);
        }

        currentPreviewSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        photoPreviewImage.sprite = currentPreviewSprite;
        photoPreviewImage.color = Color.white;
        photoPreviewImage.preserveAspect = true;
        photoPreviewImage.raycastTarget = false;
        photoPreviewImage.type = Image.Type.Simple;

        float aspect = (float)texture.width / texture.height;
        float targetWidth = previewWidth;
        float targetHeight = targetWidth / aspect;

        RectTransform panelRect = photoPreviewPanel.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);

            panelRect.sizeDelta = new Vector2(targetWidth, targetHeight);
            panelRect.anchoredPosition = new Vector2(-previewMarginRight, previewMarginBottom);
            panelRect.localScale = Vector3.one;
        }

        RectTransform imageRect = photoPreviewImage.GetComponent<RectTransform>();

        if (imageRect != null)
        {
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.pivot = new Vector2(0.5f, 0.5f);

            imageRect.offsetMin = new Vector2(previewBorderSize, previewBorderSize);
            imageRect.offsetMax = new Vector2(-previewBorderSize, -previewBorderSize);
            imageRect.localScale = Vector3.one;
        }

        photoPreviewPanel.SetActive(true);

        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
        }

        previewCoroutine = StartCoroutine(ShowPreviewForSeconds(previewShowTime));
    }

    IEnumerator ShowPreviewForSeconds(float seconds)
    {
        photoPreviewPanel.SetActive(true);

        yield return new WaitForSeconds(seconds);

        photoPreviewPanel.SetActive(false);

        if (!canTakePhoto && photoCanvas != null)
        {
            photoCanvas.SetActive(false);
        }
    }

    IEnumerator FlashEffect()
    {
        if (flashImage == null)
        {
            yield break;
        }

        SetupFlashImage();

        flashImage.gameObject.SetActive(true);

        float fadeInTime = 0.05f;
        float fadeOutTime = 0.25f;

        float timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.85f, timer / fadeInTime);
            SetFlashAlpha(alpha);
            yield return null;
        }

        timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0.85f, 0f, timer / fadeOutTime);
            SetFlashAlpha(alpha);
            yield return null;
        }

        SetFlashAlpha(0f);
    }

    void SetFlashAlpha(float alpha)
    {
        if (flashImage == null)
        {
            return;
        }

        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }

    void SetupFlashImage()
    {
        if (flashImage == null)
        {
            return;
        }

        flashImage.raycastTarget = false;
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.type = Image.Type.Simple;

        RectTransform flashRect = flashImage.GetComponent<RectTransform>();

        if (flashRect != null)
        {
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.pivot = new Vector2(0.5f, 0.5f);

            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            flashRect.localScale = Vector3.one;
        }

        flashImage.transform.SetAsLastSibling();
    }

    void SetupPreviewUI()
    {
        if (photoCanvas == null || photoPreviewPanel == null || photoPreviewImage == null)
        {
            return;
        }

        photoPreviewPanel.transform.SetParent(photoCanvas.transform, false);
        photoPreviewImage.transform.SetParent(photoPreviewPanel.transform, false);

        Image panelImage = photoPreviewPanel.GetComponent<Image>();

        if (panelImage == null)
        {
            panelImage = photoPreviewPanel.AddComponent<Image>();
        }

        panelImage.sprite = null;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;
        panelImage.type = Image.Type.Simple;

        RectTransform panelRect = photoPreviewPanel.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-previewMarginRight, previewMarginBottom);
            panelRect.localScale = Vector3.one;
        }

        photoPreviewImage.raycastTarget = false;
        photoPreviewImage.color = Color.white;
        photoPreviewImage.type = Image.Type.Simple;
        photoPreviewImage.preserveAspect = true;

        RectTransform imageRect = photoPreviewImage.GetComponent<RectTransform>();

        if (imageRect != null)
        {
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.pivot = new Vector2(0.5f, 0.5f);

            imageRect.offsetMin = new Vector2(previewBorderSize, previewBorderSize);
            imageRect.offsetMax = new Vector2(-previewBorderSize, -previewBorderSize);
            imageRect.localScale = Vector3.one;
        }

        if (flashImage != null)
        {
            flashImage.transform.SetAsLastSibling();
        }
    }

    string GetImgFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, "img");
    }

    void CreateImgFolderIfNeeded()
    {
        string folderPath = GetImgFolderPath();

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }
}
