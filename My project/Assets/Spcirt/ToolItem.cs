using UnityEngine;
public class ToolItem : MonoBehaviour
{
    public HandState toolType;

    Vector3 originalPosition;
    Quaternion originalRotation;

    void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    public void OnPicked()
    {
        gameObject.SetActive(false);
    }

    public void OnDropped()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        gameObject.SetActive(true);
    }
}