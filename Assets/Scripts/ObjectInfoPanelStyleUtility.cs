using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ObjectInfoPanelStyleUtility
{
    private static readonly Color PanelColor = new Color(0.86f, 0.94f, 0.90f, 0.96f);
    private static readonly Color BorderColor = new Color(0.04f, 0.42f, 0.15f, 0.45f);
    private static readonly Color TitleColor = new Color(0.02f, 0.10f, 0.05f, 1f);
    private static readonly Color BodyColor = new Color(0.08f, 0.16f, 0.11f, 1f);

    public static void Apply(GameObject panelRoot, TMP_Text infoText)
    {
        if (panelRoot == null && infoText == null)
        {
            return;
        }

        GameObject panel = ResolvePanel(panelRoot, infoText);

        if (panel != null)
        {
            ApplyPanel(panel);
        }

        if (infoText != null)
        {
            ApplyText(infoText);
        }
    }

    public static string Format(string title, string description)
    {
        string safeTitle = EscapeRichText(string.IsNullOrWhiteSpace(title) ? "Object Information" : title.Trim());
        string safeDescription = EscapeRichText(string.IsNullOrWhiteSpace(description) ? "No description available." : description.Trim());

        return "<b><size=130%><color=#051A0D>" + safeTitle + "</color></size></b>\n\n"
            + "<size=92%><color=#14291B>" + safeDescription + "</color></size>";
    }

    private static GameObject ResolvePanel(GameObject panelRoot, TMP_Text infoText)
    {
        if (infoText != null && infoText.transform.parent != null)
        {
            GameObject parent = infoText.transform.parent.gameObject;

            if (panelRoot == null || parent != panelRoot)
            {
                return parent;
            }
        }

        return panelRoot;
    }

    private static void ApplyPanel(GameObject panel)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one;

            Vector2 size = panelRect.sizeDelta;
            if (Mathf.Abs(size.x) < 420f)
            {
                size.x = 520f;
            }

            if (Mathf.Abs(size.y) < 190f)
            {
                size.y = 230f;
            }

            panelRect.sizeDelta = size;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panel.AddComponent<Image>();
        }

        panelImage.color = PanelColor;
        panelImage.raycastTarget = false;

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }

        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

    private static void ApplyText(TMP_Text infoText)
    {
        infoText.color = BodyColor;
        infoText.richText = true;
        infoText.enableWordWrapping = true;
        infoText.overflowMode = TextOverflowModes.Truncate;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.fontSize = Mathf.Max(infoText.fontSize, 24f);
        infoText.fontSizeMin = 18f;
        infoText.fontSizeMax = 30f;
        infoText.enableAutoSizing = true;
        infoText.margin = new Vector4(26f, 22f, 26f, 20f);

        RectTransform textRect = infoText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;
        }
    }

    private static string EscapeRichText(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
