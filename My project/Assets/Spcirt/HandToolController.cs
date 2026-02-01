using UnityEngine;
public enum HandState
{
    Idle,
    MaskPen,
    RewritePen,
    Stamp
}
public class HandToolController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource; // 放在玩家手上或者Canvas上
    public AudioClip pickupSound;   // 拾取工具音效

    public HandState currentState = HandState.Idle;
    ToolItem currentTool;

    [System.Serializable]
    public class HandVisual
    {
        public HandState state;
        public GameObject handObject;
        public Transform pointer;
    }

    [Header("Hand Visuals")]
    public HandVisual[] handVisuals;

    public float interactRadius = 0.1f;

    Transform currentPointer;

    void Start()
    {
        UpdateHandVisual();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPickTool();
        }
    }

    void TryPickTool()
    {
        if (currentPointer == null) return;

        Collider[] hits = Physics.OverlapSphere(currentPointer.position, interactRadius);

        foreach (var hit in hits)
        {
            ToolItem tool = hit.GetComponent<ToolItem>();
            if (tool != null)
            {
                EquipTool(tool);
                break;
            }
        }
    }

    void EquipTool(ToolItem newTool)
    {
        // 如果已经拿着东西，先放回去
        if (currentTool != null)
        {
            currentTool.OnDropped();
        }

        currentTool = newTool;
        currentState = newTool.toolType;

        UpdateHandVisual();
        newTool.OnPicked();
    }

    void UpdateHandVisual()
    {
        currentPointer = null;

        foreach (var hv in handVisuals)
        {
            bool active = hv.state == currentState;
            hv.handObject.SetActive(active);
            if (pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }

            if (active)
                currentPointer = hv.pointer;
        }
    }

    public Transform GetCurrentPointer()
    {
        return currentPointer;
    }

    void OnDrawGizmos()
    {
        if (currentPointer == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentPointer.position, interactRadius);
    }
}