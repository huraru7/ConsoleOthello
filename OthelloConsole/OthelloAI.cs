using System;

public class OthelloAI
{
    // int depth = 0;//探索の深さ
    private int _player;
    private int _AI;

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
        _AI = (player == 1) ? 2 : 1;
        Console.WriteLine($"AIは{_AI} playerは{_player}");

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

        int bestScore = int.MinValue;
        (int x, int y) bestMove = (0, 0);
        Console.WriteLine($"合法手の数: {_validMoves.Count}");
        Console.WriteLine();
        foreach (var result in _validMoves)
        {
            var futureBoard = AIPlacePiece(result.x, result.y, _board, _AI);
            int score = EvaluatePosition(futureBoard);

            Console.WriteLine($"配置{result.x},{result.y}に置くと評価値は{score}になります。");
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = (result.x, result.y);
            }
        }
        return bestMove;
    }

    private int[,] AIPlacePiece(int x, int y, int[,] _board, int _turn)
    {
        int[,] copy = (int[,])_board.Clone();

        x -= 1;
        y -= 1;
        copy[x, y] = _turn;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int direction = 0; direction < dx.Length; direction++)
        {
            List<(int x, int y)> Candidates = new List<(int x, int y)>();

            int nx = x + dx[direction];
            int ny = y + dy[direction];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (copy[nx, ny] == (_turn == 1 ? 2 : 1)) // 相手の駒
                {
                    Candidates.Add((nx, ny));
                }
                else if (copy[nx, ny] == (_turn == 1 ? 1 : 2)) // 自分の駒
                {
                    if (Candidates.Count > 0)
                    {
                        foreach (var candidate in Candidates)
                        {
                            copy[candidate.x, candidate.y] = _turn;
                        }
                    }
                    break;
                }
                else
                {
                    break;
                }

                nx += dx[direction];
                ny += dy[direction];
            }
        }
        return copy;
    }


    int EvaluatePosition(int[,] board)
    {
        int[,] scoreSheet = new int[,]
        {
            {  30, -12,   0,  -1,  -1,   0, -12,  30},
            { -12, -15,  -3,  -3,  -3,  -3, -15, -12},
            {   0,  -3,   0,  -1,  -1,   0,  -3,   0},
            {  -1,  -3,  -1,  -1,  -1,  -1,  -3,  -1},
            {  -1,  -3,  -1,  -1,  -1,  -1,  -3,  -1},
            {   0,  -3,   0,  -1,  -1,   0,  -3,   0},
            { -12, -15,  -3,  -3,  -3,  -3, -15, -12},
            {  30, -12,   0,  -1,  -1,   0, -12,  30},
        };

        int playerscore = 0;
        int AIscore = 0;
        int totalscore = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (board[y, x] == _player)
                {
                    playerscore += scoreSheet[y, x];
                }
                else if (board[y, x] == _AI)
                {
                    AIscore += scoreSheet[y, x];
                }
            }
        }
        totalscore = AIscore - playerscore;
        return totalscore;
    }
}
public enum AIStrength
{
    whet,
    nuub,
    normal,
    professional
}