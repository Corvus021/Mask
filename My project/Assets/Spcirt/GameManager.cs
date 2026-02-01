using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EndingType
{
    None,
    Good,
    Betray,
    Bad
}
public class GameManager : MonoBehaviour
{
    [Header("Text & Interaction")]
    public TextPointInteractor textInteractor;

    [Header("Documents")]
    public DocumentDefinition day1Document;
    public DocumentDefinition day2GoodVariant;
    public DocumentDefinition day2BadVariant;
    public DocumentDefinition day3GoodVariant;
    public DocumentDefinition day3BadVariant;

    [Header("UI")]
    public TMP_Text bossDialogueText;
    public TMP_Text endingText;
    public GameObject endingPanel;
    public Button restartButton;

    [Header("Game State")]
    private int currentDay = 0;
    private DocumentResult previousDayResult = DocumentResult.None;

    [Header("Boss Cutscene")]
    public BossCutsceneController bossCutsceneController;

    void Start()
    {
        // 隐藏结局 Panel
        if (endingPanel != null)
            endingPanel.SetActive(false);

        // 绑定重启按钮
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        StartDay(currentDay);
    }

    #region Day Flow
    void StartDay(int dayIndex)
    {
        DocumentDefinition docDef = GetDocumentForDay(dayIndex, previousDayResult);
        if (docDef == null)
        {
            Debug.LogError($"No document defined for day {dayIndex + 1}");
            return;
        }

        // 初始化文档状态
        textInteractor.documentState = new DocumentState();
        textInteractor.documentDefinition = docDef;

        // 设置 TMP 文本
        textInteractor.documentText.text = docDef.textContent;

        // 加载 fuzzy 选项
        RewriteOptionsDatabase.LoadDayOptions(docDef);
        // 禁用玩家操作，等待老板演出
        textInteractor.enabled = false;

        // 显示文本框
        bossDialogueText.gameObject.SetActive(true);

        // 播放老板开场对白
        List<string> dayDialogue = GetDayDialogue(dayIndex, previousDayResult);
        bossCutsceneController.PlayCutscene(dayDialogue, () =>
        {
            // 演出结束后开启玩家操作
            textInteractor.enabled = true;
        });
    }

    DocumentDefinition GetDocumentForDay(int dayIndex, DocumentResult prevResult)
    {
        switch (dayIndex)
        {
            case 0: return day1Document;
            case 1: return prevResult == DocumentResult.GoodEnding ? day2GoodVariant : day2BadVariant;
            case 2: return prevResult == DocumentResult.GoodEnding ? day3GoodVariant : day3BadVariant;
            default: return null;
        }
    }
    #endregion

    #region Document Completion
    public void OnDocumentComplete()
    {
        textInteractor.enabled = false;

        DocumentResult result = ScoreEvaluator.Evaluate(
            textInteractor.documentState,
            textInteractor.documentDefinition
        );

        Debug.Log($"Day {currentDay + 1} result: {result}");
        previousDayResult = result;
        StartCoroutine(PlayBossFeedback(result));
    }

    IEnumerator PlayBossFeedback(DocumentResult result)
    {
        bossDialogueText.gameObject.SetActive(true);
        bossCutsceneController.bossAnimator.SetTrigger("等待");
        yield return new WaitForSeconds(3f);

        if (result == DocumentResult.DeathEnding || result == DocumentResult.BetrayEnding)
        {
            EndGame(result);
        }
        else
        {
            currentDay++;
            StartDay(currentDay);
        }
    }
    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    #endregion

    #region Helper
    List<string> GetDayDialogue(int dayIndex, DocumentResult prevResult)
    {
        switch (dayIndex)
        {
            case 0: // Day1 开场
                return new List<string>
                {
                    "Hey, Agent. You will assume the alias Mr.Mask. As a confidential document editor, you will infiltrate this emerging tech company to steal information for our organization.",
                    "Your new identity will give you easy access to secrets, but be careful—mislabeling any files might arouse suspicion from your new boss. Good luck.",
                    "Boss: So, you are our new editor? Mr.Mask... what an interesting name.",
                    "Boss: Although it’s probably in the handbook, I’ll explain again. You see the thick marker? Use it to black out sensitive information—names, locations, or dates.",
                    "Boss: The thin pen is for rewriting words. If you have any literary stubbornness—though I don’t think it matters, these files will be destroyed—use it to modify words.",
                    "Boss: Once you are done, stamp the documents, and they will be safely shredded.",
                    "Boss: I hope you can keep secrets. Don’t try any tricks, or you will end up like your predecessor.",
                    "Boss: Now, start your work.",
                    ""
                };
            case 1: // Day2，根据上一日评分不同
                if (prevResult == DocumentResult.GoodEnding)
                    return new List<string> { "Looks like you enjoy this work.", "Your effort couldn't protect all our secrets, but it's better than nothing.", ""};
                else if (prevResult == DocumentResult.BetrayEnding)
                    return new List<string> { "Well done Mask.", "I trust I wasn't mistaken about you.", ""};
                else
                    return new List<string> { "You treated us like fools, didn't you? ", "As a spy, you should hide better next time.", ""};
            case 2: // Day3
                if (prevResult == DocumentResult.GoodEnding)
                    return new List<string> { "Our operation has failed again.", "Don’t worry, I’m not doubting you, just venting a little.", "" };
                else if (prevResult == DocumentResult.BetrayEnding)
                    return new List<string> { "This position suits you well, doesn’t it?", "Now I should start considering your promotion.", "" };
                else
                    return new List<string> { "You treated us like fools, didn't you? ", "As a spy, you should hide better next time.", "" };
            default:
                return new List<string>();
        }
    }

    void CheckEnding()
    {
        EndingType ending = EndingType.None;

        // 坏结局：任意一天 mask 不全
        if (!textInteractor.documentState.AllMaskCorrect(textInteractor.documentDefinition.expectedMaskCount))
        {
            ending = EndingType.Bad;
        }
        else if (currentDay >= 2) // 第三天结束
        {
            bool allRewriteCorrect = textInteractor.documentState.AllFuzzyCorrect();

            if (allRewriteCorrect)
                ending = EndingType.Good;
            else
                ending = EndingType.Betray;
        }

        if (ending != EndingType.None)
            ShowEnding(ending);
    }

    void ShowEnding(EndingType ending)
    {
        if (endingPanel == null || endingText == null)
        {
            Debug.LogError("Ending UI not assigned!");
            return;
        }

        endingPanel.SetActive(true);

        switch (ending)
        {
            case EndingType.Bad:
                endingText.color = Color.red;
                endingText.text = "You treated us like fools, didn't you?\nAs a spy, you should hide better next time.";
                break;
            case EndingType.Good:
                endingText.color = new Color(1f, 0.84f, 0f); // 金色
                endingText.text = "You successfully gained your boss's trust and secretly thwarted their plan to sell new weapons. Thank you, Agent. You made a major contribution to world peace.";
                break;
            case EndingType.Betray:
                endingText.color = Color.white;
                endingText.text = "You tried hard to cover up these crimes, but the false information you spread caused the death of countless agents. As a consequence, your remaining life will be hunted by C.H.F., though you don't care.";
                break;
        }
    }
    #endregion

    #region Helper
    string GetBossDialogue(int dayIndex, DocumentResult? previousResult)
    {
        if (previousResult == null)
        {
            return $"Day {dayIndex + 1}: Prepare to review the document.";
        }

        switch (previousResult.Value)
        {
            case DocumentResult.GoodEnding: return "Excellent work, keep it up.";
            case DocumentResult.BetrayEnding: return "Hmm, some data seems off… stay cautious.";
            case DocumentResult.DeathEnding: return "This is unacceptable! You are exposed!";
            default: return "";
        }
    }

    void EndGame(DocumentResult result)
    {
        Debug.Log("Game ended with result: " + result);
        // TODO: 可以跳转场景或播放结局动画
        CheckEnding();
    }
    #endregion
}