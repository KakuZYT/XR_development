using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class SimpleXRHoverInfo : MonoBehaviour
{
    [Header("Object Info")]
    [SerializeField] private string objectName = "Object Name";

    [TextArea(3, 8)]
    [SerializeField] private string description = "Object description here.";

    [Header("UI References")]
    [SerializeField] private GameObject objectInfoCanvas;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject progressCircle;
    [SerializeField] private Image progressImage;

    [Header("Settings")]
    [SerializeField] private float showDelay = 3f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Coroutine hoverCoroutine;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        interactable.firstHoverEntered.AddListener(OnXRHoverEntered);
        interactable.lastHoverExited.AddListener(OnXRHoverExited);

        HideAllUI();

        Debug.Log("[SimpleXRHoverInfo] Ready on: " + gameObject.name);
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.firstHoverEntered.RemoveListener(OnXRHoverEntered);
            interactable.lastHoverExited.RemoveListener(OnXRHoverExited);
        }
    }

    private void OnXRHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log("[SimpleXRHoverInfo] XR Hover Entered: " + gameObject.name);

        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }

        hoverCoroutine = StartCoroutine(PlayProgressThenShowInfo());
    }

    private void OnXRHoverExited(HoverExitEventArgs args)
    {
        Debug.Log("[SimpleXRHoverInfo] XR Hover Exited: " + gameObject.name);

        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        HideAllUI();
    }

    private IEnumerator PlayProgressThenShowInfo()
    {
        float timer = 0f;

        if (objectInfoCanvas != null)
        {
            objectInfoCanvas.SetActive(true);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (progressCircle != null)
        {
            progressCircle.SetActive(true);
        }

        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
        }

        while (timer < showDelay)
        {
            timer += Time.deltaTime;

            if (progressImage != null)
            {
                progressImage.fillAmount = Mathf.Clamp01(timer / showDelay);
            }

            yield return null;
        }

        if (progressCircle != null)
        {
            progressCircle.SetActive(false);
        }

        if (infoText != null)
        {
            infoText.text = $"<b>{objectName}</b>\n\n{description}";
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }

        hoverCoroutine = null;
    }

    private void HideAllUI()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (progressCircle != null)
        {
            progressCircle.SetActive(false);
        }

        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;
        }
    }
}