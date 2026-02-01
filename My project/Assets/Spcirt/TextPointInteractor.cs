using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum LinkType
{
    Mask,
    FixMask,
    Fuzzy,
    Unknown
}

public class TextPointInteractor : MonoBehaviour
{
    [Header("Manager")]
    public GameManager gameManager;

    [Header("Text")]
    public TMP_Text documentText;

    [Header("Hand")]
    public HandToolController handController;

    [Header("Rewrite UI Prefab")]
    public GameObject rewriteUIPrefab; // Panel 内有 3 个按钮
    private GameObject currentRewriteUI;

    [Header("Audio")]
    public AudioSource audioSource; // 放在玩家手上或者Canvas上
    public AudioClip paintSound;    // 涂抹/改写音效

    public DocumentState documentState = new DocumentState();
    public DocumentDefinition documentDefinition;

    void Update()
    {
        Transform pointer = handController.GetCurrentPointer();
        if (pointer == null) return;

        if (Input.GetMouseButtonDown(0)) // 只在点击时触发
        {
            if (handController.currentState == HandState.Stamp)
            {
                // 左键点击并且手尖在文档碰撞体内
                if (IsStampPointOverDocument())
                {
                    TryStampDocument();
                }
            }
            else
            {
                HandlePointerClick(pointer.position); // Mask / Rewrite
            }
        }
    }
    void TryStampDocument()
    {
        if (documentState.HasStamp())
        {
            Debug.Log("Already stamped.");
            return;
        }

        documentState.Stamp();

        Debug.Log("📌 Document stamped.");

        // 通知 GameManager
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
            gm.OnDocumentComplete();
    }
    bool IsStampPointOverDocument()
    {
        Transform stampPoint = handController.GetCurrentPointer();
        if (stampPoint == null) return false;

        float detectRadius = 0.01f; // 可以根据手模型调整
        Collider[] hits = Physics.OverlapSphere(stampPoint.position, detectRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("DocumentCanvas")) // 文档 Canvas 必须有这个 Tag
            {
                return true;
            }
        }
        return false;
    }
    void HandlePointerClick(Vector3 pointerWorldPos)
    {
        // 点击 UI 不做 TMP 检测
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(pointerWorldPos);
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(documentText, screenPos, Camera.main);

        if (linkIndex != -1)
        {
            // 已经显示 UI，阻止重复弹窗
            if (currentRewriteUI != null) return;

            TMP_LinkInfo linkInfo = documentText.textInfo.linkInfo[linkIndex];
            LinkType type = GetLinkType(linkInfo);

            if (handController.currentState == HandState.MaskPen)
            {
                MaskLink(linkInfo,type);
            }
            else if (handController.currentState == HandState.RewritePen)
            {
                if (type == LinkType.Fuzzy)
                    ShowRewriteOptions(linkInfo, pointerWorldPos);
                else
                    Debug.Log("❌ This word cannot be rewritten.");
            }
        }
        else
        {
            // 点击非 link 区域，隐藏 UI
            HideRewriteUI();
        }
    }

    #region Mask Word
    LinkType GetLinkType(TMP_LinkInfo link)
    {
        string id = link.GetLinkID();
        if (id.StartsWith("mask")) return LinkType.Mask;
        if (id.StartsWith("fixmask")) return LinkType.FixMask;
        if (id.StartsWith("fuzzy")) return LinkType.Fuzzy;
        return LinkType.Unknown;
    }

    void MaskLink(TMP_LinkInfo link, LinkType type)
    {
        // 只处理 mask/fixmask
        if (type == LinkType.Fuzzy)
        {
            Debug.LogWarning("Tried to mask a fuzzy word. Ignored.");
            return;
        }
        if (type == LinkType.Mask)
        {
            documentState.RegisterMask();
        }
        else if (type == LinkType.FixMask)
        {
            documentState.RegisterExtraMaskError();
        }

        // 替换文本显示为黑格子
        int start = link.linkTextfirstCharacterIndex;
        int length = link.linkTextLength;
        string visibleText = link.GetLinkText();
        string mask = new string('■', visibleText.Length);
        if (paintSound != null)
        {
            audioSource.PlayOneShot(paintSound);
        }

        string original = documentText.text;
        int linkStart = original.IndexOf(visibleText, start);
        if (linkStart == -1) return;

        string before = original.Substring(0, linkStart);
        string after = original.Substring(linkStart + visibleText.Length);
        documentText.text = before + mask + after;
        documentText.ForceMeshUpdate();
    }

    #endregion

    #region Rewrite Word
    void ShowRewriteOptions(TMP_LinkInfo link, Vector3 pointerWorldPos)
    {
        if (rewriteUIPrefab == null)
        {
            Debug.LogError("rewriteUIPrefab is not assigned!");
            return;
        }

        // 删除旧 UI
        HideRewriteUI();

        currentRewriteUI = Instantiate(rewriteUIPrefab, documentText.canvas.transform);
        currentRewriteUI.transform.position = pointerWorldPos;

        string linkID = link.GetLinkID();
        List<string> options = RewriteOptionsDatabase.GetOptions(linkID);

        if (options == null || options.Count < 3)
        {
            Debug.LogError("No rewrite options for linkID: " + linkID);
            return;
        }

        Button[] buttons = currentRewriteUI.GetComponentsInChildren<Button>();
        if (buttons.Length < 3)
        {
            Debug.LogError("rewriteUIPrefab must have at least 3 buttons");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = options[i];

            int idx = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() =>
            {
                ApplyRewrite(link, options[idx]);
                HideRewriteUI();
            });
        }
    }

    void ApplyRewrite(TMP_LinkInfo link, string newWord)
    {
        string original = documentText.text;
        string visibleText = link.GetLinkText(); // 获取 link 内文字

        // 找到 link 在文本里的起始位置
        int linkStart = original.IndexOf(visibleText, link.linkTextfirstCharacterIndex);
        if (linkStart == -1) return;

        string before = original.Substring(0, linkStart);
        string after = original.Substring(linkStart + visibleText.Length);

        // 替换 link 内文字为 newWord
        documentText.text = before + newWord + after;
        documentText.ForceMeshUpdate();
        if (paintSound != null)
        {
            audioSource.PlayOneShot(paintSound);
        }

        // 判断改写是否正确
        bool correct = false;
        if (documentDefinition != null && documentDefinition.fuzzyEntries != null)
        {
            // 根据 linkID 查 fuzzyEntry
            FuzzyEntry entry = documentDefinition.fuzzyEntries.Find(e => e.linkID == link.GetLinkID());
            if (entry != null && entry.options.Count > 0)
            {
                // 正确答案在 options[0]
                correct = entry.options[0] == newWord;
            }
        }

        // 标记 fuzzy 成功/失败
        documentState.MarkFuzzy(link.GetLinkID(), correct);

        Debug.Log($"Rewritten {link.GetLinkID()} -> {newWord}, Correct: {correct}");
    }
    #endregion

    #region Hide UI
    void HideRewriteUI()
    {
        if (currentRewriteUI != null)
        {
            Destroy(currentRewriteUI);
            currentRewriteUI = null;
        }
    }
    #endregion

    #region Complete Document
    void CompleteDocument()
    {
        DocumentResult result =
            ScoreEvaluator.Evaluate(documentState, documentDefinition);

        Debug.Log("Document Result: " + result);

        if (gameManager != null)
            gameManager.OnDocumentComplete();
        else
            Debug.LogError("GameManager not assigned!");
    }
    #endregion
}