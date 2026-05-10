using UnityEngine;

public class ObjectInfoData : MonoBehaviour
{
    [Header("Object Information")]
    public string objectName = "Object Name";

    [TextArea(3, 8)]
    public string description = "Object description here.";
}