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

    [Header("Target Support")]
    [SerializeField] private bool createMissingTargetColliders = true;
    [SerializeField] private float minimumGeneratedColliderSize = 0.15f;

    [Header("UI References")]
    [SerializeField] private GameObject progressCircleObject;
    [SerializeField] private Image progressCircleImage;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;

    [Header("Laser Visual")]
    [SerializeField] private Color normalLaserColor = Color.white;
    [SerializeField] private Color targetLaserColor = Color.cyan;
    [SerializeField] private float laserWidth = 0.015f;
    [SerializeField] private bool showLaserWithoutTarget = false;

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

    private void Start()
    {
        ObjectInfoData.EnsureKnownAnimalInfoInScene();

        if (createMissingTargetColliders)
        {
            EnsureInfoTargetsHaveColliders();
        }
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
            if (showLaserWithoutTarget)
            {
                DrawLaser(start, end, normalLaserColor);
            }
            else
            {
                HideLaser();
            }

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
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            ObjectInfoData infoData = FindObjectInfoData(hit.collider.gameObject);

            if (infoData != null)
            {
                validHit = hit;
                return infoData;
            }
        }

        return null;
    }

    private ObjectInfoData FindObjectInfoData(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return null;
        }

        ObjectInfoData knownAnimalInfo = ObjectInfoData.ResolveKnownAnimalInfo(hitObject.transform);
        if (knownAnimalInfo != null && knownAnimalInfo.isActiveAndEnabled)
        {
            return knownAnimalInfo;
        }

        ObjectInfoData[] parentInfoData = hitObject.GetComponentsInParent<ObjectInfoData>(true);
        foreach (ObjectInfoData infoData in parentInfoData)
        {
            if (infoData != null && infoData.isActiveAndEnabled)
            {
                return infoData;
            }
        }

        ObjectInfoData[] childInfoData = hitObject.GetComponentsInChildren<ObjectInfoData>(true);
        foreach (ObjectInfoData infoData in childInfoData)
        {
            if (infoData != null && infoData.isActiveAndEnabled)
            {
                return infoData;
            }
        }

        return null;
    }

    private void EnsureInfoTargetsHaveColliders()
    {
        ObjectInfoData[] targets = FindObjectsByType<ObjectInfoData>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (ObjectInfoData target in targets)
        {
            if (target == null || !target.isActiveAndEnabled || HasEnabledCollider(target.transform))
            {
                continue;
            }

            if (!TryGetLocalRendererBounds(target.transform, out Bounds localBounds))
            {
                continue;
            }

            BoxCollider collider = target.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = localBounds.center;
            collider.size = new Vector3(
                Mathf.Max(localBounds.size.x, minimumGeneratedColliderSize),
                Mathf.Max(localBounds.size.y, minimumGeneratedColliderSize),
                Mathf.Max(localBounds.size.z, minimumGeneratedColliderSize)
            );
        }
    }

    private static bool HasEnabledCollider(Transform targetRoot)
    {
        Collider[] colliders = targetRoot.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            if (collider != null && collider.enabled)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetLocalRendererBounds(Transform targetRoot, out Bounds localBounds)
    {
        Renderer[] renderers = targetRoot.GetComponentsInChildren<Renderer>(true);
        localBounds = default;
        bool foundRenderer = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z)
                        );
                        Vector3 localCorner = targetRoot.InverseTransformPoint(worldCorner);

                        if (!foundRenderer)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            foundRenderer = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return foundRenderer;
    }

    private void ShowInfoPanel(ObjectInfoData targetInfo)
    {
        if (targetInfo == null)
        {
            return;
        }

        if (infoText != null)
        {
            infoText.text = ObjectInfoPanelStyleUtility.Format(
                targetInfo.objectName,
                targetInfo.description
            );
        }

        ObjectInfoPanelStyleUtility.Apply(infoPanel, infoText);

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
