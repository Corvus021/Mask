using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DocumentState
{
    // Mask 统计
    public int correctMaskCount = 0;   // mask 涂黑计数
    public bool extraMaskError = false; // fixmask 被涂黑
    public bool stamped = false;

    // Fuzzy 改写正确记录
    private Dictionary<string, bool> fuzzyCorrectDict = new Dictionary<string, bool>();

    // mask 涂黑成功
    public void RegisterMask() => correctMaskCount++;

    // fixmask 被涂黑
    public void RegisterExtraMaskError() => extraMaskError = true;

    // 标记 fuzzy 改写是否正确
    public void MarkFuzzy(string linkID, bool correct)
    {
        if (fuzzyCorrectDict.ContainsKey(linkID))
            fuzzyCorrectDict[linkID] = correct;
        else
            fuzzyCorrectDict.Add(linkID, correct);
    }

    // 检查所有 fuzzy 是否都改写正确
    public bool AllFuzzyCorrect()
    {
        foreach (var kv in fuzzyCorrectDict)
            if (!kv.Value) return false;
        return true;
    }

    // 检查 mask 是否正确（需要传入预期 mask 数量）
    public bool AllMaskCorrect(int expectedMaskCount)
    {
        return correctMaskCount == expectedMaskCount && !extraMaskError;
    }

    public bool HasExtraMaskError() => extraMaskError;
    public bool HasStamp()
    {
        return stamped;
    }

    public void Stamp()
    {
        stamped = true;
    }
}