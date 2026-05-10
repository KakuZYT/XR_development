using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ObjectInfoPanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject infoCanvas;
    [SerializeField] private TMP_Text infoText;

    [Header("Camera Reference")]
    [SerializeField] private Camera playerCamera;

    [Header("Display Settings")]
    [SerializeField] private float showDelay = 3f;
    [SerializeField] private float distanceFromCamera = 2f;
    [SerializeField] private Vector3 screenOffset = new Vector3(0f, -0.2f, 0f);

    private ObjectInfoData currentInfoData;
    private float hoverTimer;
    private bool isHovering;
    private bool isShowing;

    private void Awake()
    {
        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (isHovering && !isShowing)
        {
            hoverTimer += Time.deltaTime;

            if (hoverTimer >= showDelay)
            {
                ShowCurrentInfo();
            }
        }

        if (isShowing)
        {
            KeepCanvasInFrontOfPlayer();
        }
    }

    public void OnHoverEntered(HoverEnterEventArgs args)
    {
        GameObject targetObject = args.interactableObject.transform.gameObject;

        ObjectInfoData infoData = targetObject.GetComponent<ObjectInfoData>();

        if (infoData == null)
        {
            return;
        }

        currentInfoData = infoData;
        hoverTimer = 0f;
        isHovering = true;
        isShowing = false;

        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);
        }
    }

    public void OnHoverExited(HoverExitEventArgs args)
    {
        ResetInfoPanel();
    }

    private void ShowCurrentInfo()
    {
        if (currentInfoData == null || infoCanvas == null || infoText == null)
        {
            return;
        }

        infoText.text =
            $"<b>{currentInfoData.objectName}</b>\n\n" +
            currentInfoData.description;

        KeepCanvasInFrontOfPlayer();

        infoCanvas.SetActive(true);
        isShowing = true;
    }

    private void KeepCanvasInFrontOfPlayer()
    {
        if (playerCamera == null || infoCanvas == null)
        {
            return;
        }

        Transform cameraTransform = playerCamera.transform;

        Vector3 targetPosition =
            cameraTransform.position +
            cameraTransform.forward * distanceFromCamera +
            cameraTransform.TransformDirection(screenOffset);

        infoCanvas.transform.position = targetPosition;

        Vector3 lookDirection = infoCanvas.transform.position - cameraTransform.position;
        infoCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    private void ResetInfoPanel()
    {
        currentInfoData = null;
        hoverTimer = 0f;
        isHovering = false;
        isShowing = false;

        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);
        }
    }
}