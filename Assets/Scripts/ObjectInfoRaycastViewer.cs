using TMPro;
using UnityEngine;

public class ObjectInfoRaycastViewer : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float showDelay = 3f;

    [Header("UI References")]
    [SerializeField] private GameObject infoCanvas;
    [SerializeField] private TMP_Text infoText;

    [Header("UI Position")]
    [SerializeField] private float distanceFromCamera = 2f;
    [SerializeField] private Vector3 screenOffset = new Vector3(0f, -0.2f, 0f);

    private ObjectInfoData currentTarget;
    private float lookTimer;
    private bool isShowing;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        DetectObjectInFront();

        if (isShowing)
        {
            KeepCanvasInFrontOfPlayer();
        }
    }

    private void DetectObjectInFront()
    {
        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, rayDistance);

        if (!hitSomething)
        {
            ResetPanel();
            return;
        }

        ObjectInfoData infoData = hit.collider.GetComponentInParent<ObjectInfoData>();

        if (infoData == null)
        {
            ResetPanel();
            return;
        }

        if (currentTarget != infoData)
        {
            currentTarget = infoData;
            lookTimer = 0f;
            isShowing = false;

            if (infoCanvas != null)
            {
                infoCanvas.SetActive(false);
            }
        }

        lookTimer += Time.deltaTime;

        if (lookTimer >= showDelay && !isShowing)
        {
            ShowPanel(infoData);
        }
    }

    private void ShowPanel(ObjectInfoData infoData)
    {
        if (infoCanvas == null || infoText == null)
        {
            return;
        }

        infoText.text =
            $"<b>{infoData.objectName}</b>\n\n" +
            infoData.description;

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

    private void ResetPanel()
    {
        currentTarget = null;
        lookTimer = 0f;
        isShowing = false;

        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);
        }
    }
}