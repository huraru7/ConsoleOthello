using System;

public class OthelloAI
{
    int depth = 0;//探索の深さ

    private int _player;
    private int _opponent;



    /// <summary>
    /// AIの手を決定するメソッド
    /// </summary>
    /// <param name="_validMoves">駒を置くことができる候補地</param>
    /// <param name="_board">現在の盤面</param>
    /// <param name="player">player側の_turnの数字</param>
    /// <param name="_AIStrength">AIの強さ(enum)</param>
    /// <returns>置くべき場所を座標で返します</returns>
    public (int x, int y) AI(List<(int x, int y)> _validMoves, int[,] _board, int player, AIStrength _AIStrength)
    {
        _player = player;
        _opponent = (player == 1) ? 2 : 1;

        switch (_AIStrength)
        {
            case AIStrength.whet:
                // 初級AIのロジックをここに実装
                break;
            case AIStrength.nuub:
                // 中級AIのロジックをここに実装
                break;
            case AIStrength.normal:
                // 上級AIのロジックをここに実装
                break;
            case AIStrength.professional:
                // プロフェッショナルAIのロジックをここに実装
                break;
        }



        return (0, 0); // 仮の戻り値
    }

    public int Evaluationfunction(int[,] board, int player)
    {
        //仮のやつ
        _player = player;
        _opponent = (player == 1) ? 2 : 1;

        int positional = EvaluatePosition(board);//位置の評価
        Console.WriteLine($"位置の評価値:{positional}");
        int mobility = TileCountEvaluation(board);//駒数の評価
        Console.WriteLine($"駒数の評価値:{mobility}");
        int stability = EvaluateStability(board);//安定石の評価
        Console.WriteLine($"安定石の評価値:{stability}");

        int score =
            positional * 1 +
            mobility * 10 +
            stability * 50
            ;

        return score;
    }
    int EvaluatePosition(int[,] board)
    {
        int[,] scoreSheet = new int[,]
        {
            {120,-20, 20,  5,  5, 20,-20,120},
            {-20,-40,-5, -5, -5, -5,-40,-20},
            {20, -5, 15,  3,  3, 15, -5, 20},
            {5,  -5,  3,  0,  0,  3, -5,  5},
            {5,  -5,  3,  0,  0,  3, -5,  5},
            {20, -5, 15,  3,  3, 15, -5, 20},
            {-20,-40,-5, -5, -5, -5,-40,-20},
            {120,-20, 20,  5,  5, 20,-20,120},
        };

        int score = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (board[y, x] == _player)
                {
                    score += scoreSheet[y, x];
                }
                else if (board[y, x] == _opponent)
                {
                    score -= scoreSheet[y, x];
                }
            }
        }
        return score;
    }

    private int EvaluateStability(int[,] board)
    {
        return CountStableDiscs(board, _player) - CountStableDiscs(board, _opponent);
    }

    /// <summary>
    /// 安定した石を調査します
    /// </summary>
    /// <param name="board">現在の盤面</param>
    /// <returns>安定石の個数</returns>
    private int CountStableDiscs(int[,] board, int turn)
    {
        int count = 0;
        // 角の座標
        int[,] corners = new int[,]
        {{0,0}, {0,7}, {7,0}, {7,7}};

        for (int i = 0; i < corners.GetLength(0); i++)
        {
            int cornerY = corners[i, 0];
            int cornerX = corners[i, 1];

            if (board[cornerY, cornerX] != turn) continue;

            // 横探索
            int startX = cornerX;
            int stepX = (cornerX == 0) ? 1 : -1; // 0なら右、7なら左

            int x = startX;
            while (x >= 0 && x < 8 && board[cornerY, x] == turn)
            {
                count++;
                x += stepX;
            }

            // 縦探索
            int startY = cornerY;
            int stepY = (cornerY == 0) ? 1 : -1; // 0なら下、7なら上

            int y = startY;
            while (y >= 0 && y < 8 && board[y, cornerX] == turn)
            {
                count++;
                y += stepY;
            }

        }
        return count;
    }

    private int TileCountEvaluation(int[,] board)
    {
        int playerCount = 0, opponentCount = 0;
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                if (board[y, x] == _player) playerCount++;
                else if (board[y, x] == _opponent) opponentCount++;
            }

        return playerCount - opponentCount;
    }

}
public enum AIStrength
{
    whet,
    nuub,
    normal,
    professional
}