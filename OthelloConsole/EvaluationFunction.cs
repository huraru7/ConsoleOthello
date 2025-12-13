public static class EvaluationFunction
{
    /// <summary>
    /// 評価関数を回します
    /// </summary>
    /// <param name="_gameData"></param>
    /// <returns>その盤面の評価値</returns>
    public static int evaluationFunction(MainGameData _gameData)
    {
        int evaluationScore = 0;
        evaluationScore += EvaluatePosition(_gameData._tiles, _gameData._turnNum);

        return evaluationScore;
    }
    private static int EvaluatePosition(int[,] _tiles, int _turnNum)
    {
        int[,] scoreSheet = new int[,]
        {
            { 150, -20, 0, -1, -1, 0, -20, 150},
            { -20, -15, -3, -3, -3, -3, -15, -20 },
            { 0, -3, 0, -1, -1, 0, -3, 0 },
            { -1, -3, -1, -1, -1, -1, -3, -1 },
            { -1, -3, -1, -1, -1, -1, -3, -1 },
            { 0, -3, 0, -1, -1, 0, -3, 0 },
            { -20, -15, -3, -3, -3, -3, -15, -20 },
            { 150, -20, 0, -1, -1, 0, -20, 150 },
        };

        int score = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (_tiles[x, y] == _turnNum)
                    score += scoreSheet[x, y];
            }
        }
        return score;
    }
}