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
        int score = 0;
        int sizeX = _tiles.GetLength(0);
        int sizeY = _tiles.GetLength(1);

        if (sizeX == 8 && sizeY == 8)
        {
            int[,] scoreSheet = new int[,]
            {
                { 250, -30, 0, -1, -1, 0, -30, 250},
                { -30, -15, -3, -3, -3, -3, -15, -30 },
                { 0, -3, 0, -1, -1, 0, -3, 0 },
                { -1, -3, -1, -1, -1, -1, -3, -1 },
                { -1, -3, -1, -1, -1, -1, -3, -1 },
                { 0, -3, 0, -1, -1, 0, -3, 0 },
                { -30, -15, -3, -3, -3, -3, -15, -30 },
                { 250, -30, 0, -1, -1, 0, -30, 250 },
            };

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (_tiles[x, y] == _turnNum)
                        score += scoreSheet[x, y];
                }
            }
        }
        else
        {
            // 8x8以外は簡易評価: 自身の駒数をそのままスコアにする
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (_tiles[x, y] == _turnNum) score += 1;
                    else if (_tiles[x, y] != 0) score -= 1;
                }
            }
        }

        return score;
    }
}