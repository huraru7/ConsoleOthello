using System;

public class EvaluationFunction
{

    /// <summary>
    /// 評価関数
    /// </summary>
    /// <param name="board">評価する盤面</param>
    /// <param name="_player">プレイヤーの番号</param>
    /// <param name="_AI">AIの番号</param>
    /// <returns>boardの評価値を返します</returns>
    public int evaluationFunction(int[,] board, int _player)
    {
        int evaluationScore = 0;
        //今まで認識を間違えていました、minimax法なので評価基準は全て自分にとっての評価を行うべきです。
        //おそらくminimax法を反転させた上でnagamax法を使っていたのが今までのバグの原因かもしれません。
        evaluationScore += EvaluatePosition(board, _player);

        return evaluationScore;
    }
    public int EvaluatePosition(int[,] board, int _player)
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
                if (scoreSheet[x, y] == _player)
                    score += scoreSheet[x, y];
            }
        }
        return score;
    }
}