public static class ScoreEvaluator
{
    // 评估结果
    // expectedMaskCount: 该文档应涂黑的 mask 数量
    public static DocumentResult Evaluate(DocumentState state, DocumentDefinition def)
    {
        if (state.HasExtraMaskError() || !state.AllMaskCorrect(def.expectedMaskCount))
        {
            // mask 做错或涂了不该涂的 → 死亡结局
            return DocumentResult.DeathEnding;
        }

        if (!state.AllFuzzyCorrect())
        {
            // 改写错误 → 叛徒结局
            return DocumentResult.BetrayEnding;
        }

        // 全部正确 → 好结局
        return DocumentResult.GoodEnding;
    }
}