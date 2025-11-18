using System;

public class EvaluationFunction
{
    public int EvaluatePosition(int[,] board, int _player, int _AI, int BOARD_SIZE)
    {
        int[,] scoreSheet = new int[,]
        {
            { 30, -12, 0, -1, -1, 0, -12, 30 },
            { -12, -15, -3, -3, -3, -3, -15, -12 },
            { 0, -3, 0, -1, -1, 0, -3, 0 },
            { -1, -3, -1, -1, -1, -1, -3, -1 },
            { -1, -3, -1, -1, -1, -1, -3, -1 },
            { 0, -3, 0, -1, -1, 0, -3, 0 },
            { -12, -15, -3, -3, -3, -3, -15, -12 },
            { 30, -12, 0, -1, -1, 0, -12, 30 },
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