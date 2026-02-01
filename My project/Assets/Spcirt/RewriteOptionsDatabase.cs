using System.Collections.Generic;
using UnityEngine;

public static class RewriteOptionsDatabase
{
    static Dictionary<string, List<string>> options = new Dictionary<string, List<string>>();

    // 根据当天 DocumentDefinition 加载 fuzzy 选项
    public static void LoadDayOptions(DocumentDefinition docDef)
    {
        options.Clear();

        foreach (var entry in docDef.fuzzyEntries)
        {
            if (entry.options.Count != 3)
            {
                Debug.LogWarning($"FuzzyEntry {entry.linkID} does not have exactly 3 options!");
            }

            // 保持顺序，第0个是正确答案
            options[entry.linkID] = new List<string>(entry.options);
        }
    }

    public static List<string> GetOptions(string linkID)
    {
        if (options.ContainsKey(linkID))
            return options[linkID];
        else
            return new List<string> { "Option1", "Option2", "Option3" }; // 防呆
    }
}