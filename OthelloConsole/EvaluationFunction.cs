using System;

public class EvaluationFunction
{

    /// <summary>
    /// 評価関数
    /// </summary>
    /// <param name="board">評価する盤面</param>
    /// <param name="_player">プレイヤーの番号</param>
    /// <param name="_AI">AIの番号</param>
    /// <param name="BOARD_SIZE">ボードサイズ（定数）</param>
    /// <returns>boardの評価値を返します</returns>
    public int evaluationFunction(int[,] board, int _player, int _AI, int BOARD_SIZE)
    {
        return EvaluatePosition(board, _player, _AI, BOARD_SIZE);
    }
    public int EvaluatePosition(int[,] board, int _player, int _AI, int BOARD_SIZE)
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
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                if (board[y, x] == _AI)
                    score += scoreSheet[y, x];
                else if (board[y, x] == _player)
                    score -= scoreSheet[y, x];
            }
        }
        return score;
    }
}