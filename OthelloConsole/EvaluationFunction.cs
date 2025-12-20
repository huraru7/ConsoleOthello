using static LibraryGameData;
using static OthelloSystem;

public static class EvaluationFunction
{
    /// <summary>
    /// 評価関数を回します
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>その盤面の評価値</returns>
    public static int EvaFunction(MainGameData _gameData, int me)
    {
        MainGameData _newGameData = _gameData.Clone();
        var test = Counting(_newGameData._tiles);
        int PieceCount = test.Item1 + test.Item2;
        int evaluationScore = 0;

        if (PieceCount >= 54) //終盤
        {
            evaluationScore += QuantityDifference(_newGameData, me); //有利性
        }
        else if (PieceCount >= 20) //中盤
        {
            evaluationScore += EvaluatePosition(_newGameData, me) * 25; //安全性
            evaluationScore += PossiDility(_newGameData) * 10; //可能性
            evaluationScore += QuantityDifference(_newGameData, me) * 15; //有利性
        }
        else //序盤
        {
            evaluationScore += EvaluatePosition(_newGameData, me) * 30; //安全性
            evaluationScore += PossiDility(_newGameData) * 10; //可能性
            evaluationScore += QuantityDifference(_newGameData, me) * 10; //有利性
        }

        return evaluationScore;
    }

    /// <summary>
    /// 駒の位置に対する評価
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>駒の位置に対する評価値</returns>
    private static int EvaluatePosition(MainGameData _gameData, int me)
    {
        int score = 0;

        if (_gameData._tilesSize == 8)
        {
            int[,] scoreSheet = new int[,]
            {
                { 250, -30, 000, -01, -01, 000, -30, 250 },
                { -30, -15, -03, -03, -03, -03, -15, -30 },
                { 000, -03, 000, -01, -01, 000, -03, 000 },
                { -01, -03, -01, -01, -01, -01, -03, -01 },
                { -01, -03, -01, -01, -01, -01, -03, -01 },
                { 000, -03, 000, -01, -01, 000, -03, 000 },
                { -30, -15, -03, -03, -03, -03, -15, -30 },
                { 250, -30, 000, -01, -01, 000, -30, 250 },
            };

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (_gameData._tiles[x, y] == me)
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
        _gameData = TurnChange(_gameData);
        var result = InstallationArea(_gameData);
        return result.Count;
    }

    /// <summary>
    /// 駒の数の差に対する評価
    /// </summary>
    /// <param name="_gameData">ゲームデータ</param>
    /// <returns>駒の数の差</returns>
    private static int QuantityDifference(MainGameData _gameData, int me)
    {
        var count = Counting(_gameData._tiles);
        if (me == 1)
            return count.Item1 - count.Item2;
        else
            return count.Item2 - count.Item1;
    }
}