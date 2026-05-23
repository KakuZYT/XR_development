using System;
using UnityEngine;

public class ObjectInfoData : MonoBehaviour
{
    [Header("Object Information")]
    public string objectName = "Object Name";

    [TextArea(3, 8)]
    public string description = "Object description here.";

    public bool IsPlaceholderOrGeneric()
    {
        return string.IsNullOrWhiteSpace(objectName)
            || string.Equals(objectName.Trim(), "Object Name", StringComparison.OrdinalIgnoreCase)
            || string.Equals(objectName.Trim(), "Reef Fish", StringComparison.OrdinalIgnoreCase);
    }

    public static ObjectInfoData ResolveKnownAnimalInfo(Transform hitTransform)
    {
        Transform candidate = hitTransform;

        while (candidate != null)
        {
            if (TryGetKnownAnimalDescription(candidate.name, out string title, out string body))
            {
                return GetOrCreateKnownAnimalInfo(candidate, title, body);
            }

            candidate = candidate.parent;
        }

        return null;
    }

    public static void EnsureKnownAnimalInfoInScene()
    {
        Transform[] sceneObjects = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (Transform sceneObject in sceneObjects)
        {
            if (TryGetKnownAnimalDescription(sceneObject.name, out string title, out string body))
            {
                GetOrCreateKnownAnimalInfo(sceneObject, title, body);
            }
        }
    }

    private static ObjectInfoData GetOrCreateKnownAnimalInfo(Transform target, string title, string body)
    {
        ObjectInfoData infoData = target.GetComponent<ObjectInfoData>();

        if (infoData == null)
        {
            infoData = target.gameObject.AddComponent<ObjectInfoData>();
        }

        if (infoData.IsPlaceholderOrGeneric())
        {
            infoData.objectName = title;
            infoData.description = body;
        }

        return infoData;
    }

    private static bool TryGetKnownAnimalDescription(string objectNameValue, out string title, out string body)
    {
        if (objectNameValue.IndexOf("HammerHead_Shark", StringComparison.OrdinalIgnoreCase) >= 0
            || objectNameValue.IndexOf("Hammerhead Shark", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            title = "Great Hammerhead Shark";
            body = "Great hammerhead sharks use their broad, sensory-rich heads to detect prey and navigate reef waters. They are powerful but generally avoid people and help keep the marine ecosystem balanced.";
            return true;
        }

        if (objectNameValue.IndexOf("SM_Shark_00", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            title = "Reef Shark";
            body = "Reef sharks patrol coral habitats with streamlined speed and sharp senses. As predators, they help maintain healthy balance among fish populations on the reef.";
            return true;
        }

        title = null;
        body = null;
        return false;
    }
}
