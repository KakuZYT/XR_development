using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class LaserInfoViewer : MonoBehaviour
{
    [Header("Laser Source")]
    [SerializeField] private Transform rayOrigin;

    [Header("Detection Settings")]
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float sphereRadius = 0.25f;
    [SerializeField] private float showDelay = 3f;
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("UI References")]
    [SerializeField] private GameObject progressCircleObject;
    [SerializeField] private Image progressCircleImage;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;

    [Header("Laser Visual")]
    [SerializeField] private Color normalLaserColor = Color.white;
    [SerializeField] private Color targetLaserColor = Color.cyan;
    [SerializeField] private float laserWidth = 0.015f;

    private LineRenderer lineRenderer;
    private ObjectInfoData currentTarget;
    private float hoverTimer;
    private bool infoShown;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.useWorldSpace = true;

        Material laserMaterial = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = laserMaterial;

        HideAllUI();
    }

    private void Update()
    {
        if (rayOrigin == null)
        {
            HideLaser();
            ClearTarget();
            return;
        }

        CheckLaserTarget();
    }

    private void CheckLaserTarget()
    {
        Vector3 start = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;
        Vector3 end = start + direction * rayDistance;

        Ray ray = new Ray(start, direction);

        ObjectInfoData targetInfo = FindTarget(ray, out RaycastHit targetHit);

        if (targetInfo == null)
        {
            DrawLaser(start, end, normalLaserColor);
            ClearTarget();
            return;
        }

        DrawLaser(start, targetHit.point, targetLaserColor);

        if (currentTarget != targetInfo)
        {
            currentTarget = targetInfo;
            hoverTimer = 0f;
            infoShown = false;

            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }

            if (progressCircleObject != null)
            {
                progressCircleObject.SetActive(true);
            }

            if (progressCircleImage != null)
            {
                progressCircleImage.fillAmount = 0f;
            }
        }

        if (!infoShown)
        {
            hoverTimer += Time.deltaTime;

            float progress = Mathf.Clamp01(hoverTimer / showDelay);

            if (progressCircleObject != null)
            {
                progressCircleObject.SetActive(true);
            }

            if (progressCircleImage != null)
            {
                progressCircleImage.fillAmount = progress;
            }

            if (hoverTimer >= showDelay)
            {
                ShowInfoPanel(targetInfo);
            }
        }
    }

    private ObjectInfoData FindTarget(Ray ray, out RaycastHit validHit)
    {
        validHit = default;

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            sphereRadius,
            rayDistance,
            raycastLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            ObjectInfoData infoData = hit.collider.GetComponentInParent<ObjectInfoData>();

            if (infoData != null)
            {
                validHit = hit;
                return infoData;
            }
        }

        return null;
    }

    private void ShowInfoPanel(ObjectInfoData targetInfo)
    {
        if (targetInfo == null)
        {
            return;
        }

        if (infoText != null)
        {
            infoText.text =
                $"<b>{targetInfo.objectName}</b>\n\n" +
                targetInfo.description;
        }

        if (progressCircleObject != null)
        {
            progressCircleObject.SetActive(false);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }

        infoShown = true;
    }

    private void ClearTarget()
    {
        currentTarget = null;
        hoverTimer = 0f;
        infoShown = false;
        HideAllUI();
    }

    private void HideAllUI()
    {
        if (progressCircleObject != null)
        {
            progressCircleObject.SetActive(false);
        }

        if (progressCircleImage != null)
        {
            progressCircleImage.fillAmount = 0f;
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    private void DrawLaser(Vector3 start, Vector3 end, Color color)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void HideLaser()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}