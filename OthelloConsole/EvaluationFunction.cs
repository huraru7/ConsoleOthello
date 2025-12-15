using static LibraryGameData;
using static OthelloSystem;

public static class EvaluationFunction
{
    /// <summary>
    /// 評価関数を回します
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>その盤面の評価値</returns>
    public static int EvaFunction(MainGameData _gameData)
    {
        MainGameData _newGameData = _gameData.Clone();
        var test = Counting(_newGameData._tiles);
        int PieceCount = test.Item1 + test.Item2;
        int evaluationScore = 0;

        evaluationScore += EvaluatePosition(_newGameData) * 24; //安全性
        evaluationScore += PossiDility(_newGameData) * 10; //可能性
        evaluationScore += QuantityDifference(_newGameData) * 1; //有利性

        return evaluationScore;
    }

    /// <summary>
    /// 駒の位置に対する評価
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>駒の位置に対する評価値</returns>
    private static int EvaluatePosition(MainGameData _gameData)
    {
        int score = 0;

        if (_gameData._tilesSize == 8)
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
                    if (_gameData._tiles[x, y] == _gameData._turnNum)
                        score += scoreSheet[x, y];
                }
            }
        }

        return score;
    }

    /// <summary>
    /// 設置可能な場所の数に対する評価
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>相手が設置可能な場所の数</returns>
    private static int PossiDility(MainGameData _gameData)
    {
        _gameData._gameTurn = TurnChange(_gameData._gameTurn);
        _gameData._turnNum = _gameData._gameTurn == GameTurn.prayer ? 1 : 2;
        var result = InstallationArea(_gameData);
        return result.Count;
    }

    /// <summary>
    /// 駒の数の差に対する評価
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>駒の数の差</returns>
    private static int QuantityDifference(MainGameData _gameData)
    {
        var count = Counting(_gameData._tiles);
        if (_gameData._turnNum == 1)
            return count.Item1 - count.Item2;
        else
            return count.Item2 - count.Item1;
    }
}