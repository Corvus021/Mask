using UnityEngine;

public class HandMove : MonoBehaviour
{
    public Camera cam;
    public float depth = 0.5f;

    // 手活动的屏幕范围（百分比）
    [Range(0f, 1f)] public float minX = 0.1f;
    [Range(0f, 1f)] public float maxX = 0.9f;
    [Range(0f, 1f)] public float minY = 0.1f;
    [Range(0f, 1f)] public float maxY = 0.9f;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
    void Update()
    {
        Vector3 mouse = Input.mousePosition;

        // 限制鼠标在屏幕百分比内
        mouse.x = Mathf.Clamp(mouse.x, Screen.width * minX, Screen.width * maxX);
        mouse.y = Mathf.Clamp(mouse.y, Screen.height * minY, Screen.height * maxY);
        mouse.z = depth;

        transform.position = cam.ScreenToWorldPoint(mouse);
    }
}
