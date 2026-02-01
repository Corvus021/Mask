using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BossCutsceneController : MonoBehaviour
{
    [Header("References")]
    public Animator bossAnimator;
    public TMP_Text bossDialogueText;

    [Header("Cutscene Settings")]
    public float lineDelay = 3f; // 每句对白显示时间

    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;

    private Queue<string> dialogueQueue = new Queue<string>();
    private bool isCutscenePlaying = false;

    /// <summary>
    /// 播放 Cutscene 并在播放结束时回调 onComplete
    /// </summary>
    /// <param name="dialogueLines">对白列表</param>
    /// <param name="onComplete">播放完成回调</param>
    public void PlayCutscene(List<string> dialogueLines, Action onComplete = null)
    {
        if (isCutscenePlaying) return;

        isCutscenePlaying = true;

        // 显示文本框
        if (bossDialogueText != null)
            bossDialogueText.gameObject.SetActive(true);

        // 清空队列并加入新的对白
        dialogueQueue.Clear();
        foreach (var line in dialogueLines)
            dialogueQueue.Enqueue(line);

        StartCoroutine(CutsceneRoutine(onComplete));
    }

    private IEnumerator CutsceneRoutine(Action onComplete)
    {
        // 入场动画
        if (bossAnimator != null)
            bossAnimator.SetTrigger("走过来");
        if (footstepSource != null && footstepClip != null)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.Play();
        }

        yield return new WaitForSeconds(1.5f); // 假设动画长度

        // 停止脚步声
        if (footstepSource != null)
            footstepSource.Stop();

        // 等待动画 + 逐句对白
        if (bossAnimator != null)
            bossAnimator.SetTrigger("等待");

        while (dialogueQueue.Count > 0)
        {
            string line = dialogueQueue.Dequeue();
            if (bossDialogueText != null)
                bossDialogueText.text = line;
            yield return new WaitForSeconds(lineDelay);
        }

        // 退场动画
        if (bossAnimator != null)
            bossAnimator.SetTrigger("走过去");
        if (footstepSource != null && footstepClip != null)
        {
            footstepSource.Play();
        }

        yield return new WaitForSeconds(1.5f); // 动画长度

        if (footstepSource != null)
            footstepSource.Stop();

        // 隐藏文本框
        if (bossDialogueText != null)
            bossDialogueText.gameObject.SetActive(false);

        isCutscenePlaying = false;

        // 调用回调，让 GM 知道 Cutscene 已完成
        onComplete?.Invoke();
    }
}