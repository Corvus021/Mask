using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FuzzyEntry
{
    public string linkID;            // fuzzy_1, fuzzy_2 ...
    public List<string> options;     // 这个 link 的三个改写选项，第0个是正确答案
}

[CreateAssetMenu(menuName = "Mask/Document Definition")]
public class DocumentDefinition : ScriptableObject
{
    [TextArea(10, 50)]
    public string textContent; // 文本内容（TMP）

    [Header("Mask Info")]
    public int expectedMaskCount = 0; // 这篇文档中 mask 的总数

    [Header("Fuzzy Rewrite Rules")]
    public List<FuzzyEntry> fuzzyEntries = new List<FuzzyEntry>(); // 每个 fuzzy 单词的ID+选项
}