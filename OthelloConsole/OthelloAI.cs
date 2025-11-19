using System;
using System.Collections.Generic;

public class OthelloAI
{
    // 探索の深さ
    private int _depth = 0;
    public bool _isDebug = false; // デバッグログ出力フラグ
    private int _recursionsCount;
    private int _player;
    private int _AI;
    private const int BOARD_SIZE = 8;
    public EvaluationFunction evalFunc = new EvaluationFunction();
    private StreamWriter? sw;

    public (int x, int y) AI(List<(int x, int y)> _validMoves, int[,] _board, int player, AIStrength _AIStrength)
    {
        _player = player;
        _AI = (player == 1) ? 2 : 1;
        StartLogging();

        switch (_AIStrength)
        {
            case AIStrength.whet: _depth = 0; break;
            case AIStrength.nuub: _depth = 2; break;
            case AIStrength.normal: _depth = 4; break;
            case AIStrength.professional: _depth = 6; break;
        }

        int bestScore = int.MinValue;
        (int x, int y) bestMove = (0, 0);

        foreach (var move in _validMoves)
        {
            _recursionsCount++;
            int[,] newBoard = CopyBoard(_board);
            DebugLog($"置き場所候補： ({move.x},{move.y}) の評価を開始します。");
            int score = -Negamax(AIPlacePiece(move.x, move.y, newBoard, _AI), _depth - 1, _player, int.MinValue + 1, int.MaxValue - 1);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
            else if (score == bestScore)
            {
                //scoreが一緒の場合は確率で変更する
                Random rand = new Random();
                if (rand.Next(0, 2) == 0) // 50%の確率で変更
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            DebugLog($"置き場所候補： ({move.x},{move.y}) の評価が終了しました。 評価値: {score}");
        }
        DebugLog("=============Ai評価終了=============");
        DebugLog($"総計算手順回数：{_recursionsCount}");

        StopLogging();
        return bestMove;
    }

    private int Negamax(int[,] board, int depth, int player, int alpha, int beta, int level = 0)
    {
        _recursionsCount++;
        string indent = new string(' ', level * 4);
        DebugLog($"{indent}▶ 深さ {depth} 開始 (プレイヤー: {player})");

        // 終端条件：深さ0なら評価
        if (depth == 0)
        {
            int eval = evalFunc.evaluationFunction(board, player, _AI, BOARD_SIZE);
            DebugLog($"{indent}  └ 深さ0 評価値: {eval} (プレイヤー: {player})");
            return eval;
        }

        var validMoves = GetValidMoves(board, player);
        if (validMoves.Count == 0)
        {
            // 相手の合法手を確認（"パス" 処理）
            int opponent = (player == 1) ? 2 : 1;
            var oppMoves = GetValidMoves(board, opponent);

            if (oppMoves.Count == 0)
            {
                // 両者打てない -> 終局
                int eval = evalFunc.evaluationFunction(board, player, _AI, BOARD_SIZE);
                DebugLog($"{indent}  └ 両者手なし（終局） → 評価値: {eval}");
                DebugLog($"{indent}  {BoardToString(board)}");
                return eval;
            }
            else
            {
                // 自分は打てないが相手は打てる -> パスして相手番を探索（深さを減らす）
                DebugLog($"{indent}  └ プレイヤー {player} はパス (相手に移行)");
                int val = -Negamax(board, depth - 1, opponent, -beta, -alpha, level + 1);
                DebugLog($"{indent}  └ パス後の戻り値: {val}");
                return val;
            }
        }

        int opponent2 = (player == 1) ? 2 : 1;
        int maxEval = int.MinValue;

        foreach (var move in validMoves)
        {
            // newBoard を確実に使う（AIPlacePiece は clone して返すこと）
            int[,] newBoard = AIPlacePiece(move.x, move.y, board, player);
            DebugLog($"{indent}  手 ({move.x},{move.y}) を試す");
            DebugLog($"{indent}  -- 置後の盤面 --\n{BoardToString(newBoard)}");

            int eval = -Negamax(newBoard, depth - 1, opponent2, -beta, -alpha, level + 1);

            DebugLog($"{indent}  ← 手 ({move.x},{move.y}) 結果: {eval}");

            if (eval > maxEval)
            {
                maxEval = eval;
                DebugLog($"{indent}     ↳ 現在のベスト更新: {maxEval}");
            }

            alpha = Math.Max(alpha, eval);
            if (alpha >= beta)
            {
                DebugLog($"{indent}  ✂ 枝刈り発生！ alpha={alpha}, beta={beta}");
                break;
            }
        }

        DebugLog($"{indent}◀ 深さ {depth} 終了: 戻り値 = {maxEval}");
        return maxEval;
    }



    private List<(int x, int y)> GetValidMoves(int[,] board, int player)
    {
        List<(int x, int y)> moves = new();
        int opponent = (player == 1) ? 2 : 1;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                if (board[y, x] != 0) continue;

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = x + dx[dir];
                    int ny = y + dy[dir];
                    bool hasOpponent = false;

                    while (nx >= 0 && nx < BOARD_SIZE && ny >= 0 && ny < BOARD_SIZE)
                    {
                        if (board[ny, nx] == opponent)
                        {
                            hasOpponent = true;
                        }
                        else if (board[ny, nx] == player && hasOpponent)
                        {
                            moves.Add((x + 1, y + 1)); // 1-indexで返す
                            goto NextCell;
                        }
                        else break;

                        nx += dx[dir];
                        ny += dy[dir];
                    }
                }
            NextCell:;
            }
        }
        return moves;
    }

    private int[,] AIPlacePiece(int x, int y, int[,] board, int turn)
    {
        int[,] newBoard = new int[8, 8];
        Array.Copy(board, newBoard, board.Length);

        int current = turn;
        int opponent = (turn == 1) ? 2 : 1;

        x -= 1;
        y -= 1;

        newBoard[x, y] = current;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int dir = 0; dir < 8; dir++)
        {
            List<(int x, int y)> candidates = new List<(int x, int y)>();
            int nx = x + dx[dir];
            int ny = y + dy[dir];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (newBoard[nx, ny] == opponent)
                {
                    candidates.Add((nx, ny));
                }
                else if (newBoard[nx, ny] == current)
                {
                    foreach (var c in candidates)
                        newBoard[c.x, c.y] = current;
                    break;
                }
                else break;

                nx += dx[dir];
                ny += dy[dir];
            }
        }

        return newBoard;
    }

    private int[,] CopyBoard(int[,] board)
    {
        int[,] copy = new int[BOARD_SIZE, BOARD_SIZE];
        Array.Copy(board, copy, board.Length);
        return copy;
    }
    public void StartLogging()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFile = $"AI_log_{timestamp}.txt";
        sw = new StreamWriter(logFile, false); // 新規作成
        Console.WriteLine($"ログファイル生成先: {Path.GetFullPath(logFile)}");
    }
    public void StopLogging()
    {
        sw?.Flush();
        sw?.Close();
    }

    private void DebugLog(string message)
    {
        sw?.WriteLine(message);
    }
    private string BoardToString(int[,] board)
    {
        int size = board.GetLength(0);
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                sb.Append(board[y, x] switch { 0 => "-", 1 => "●", 2 => "○", _ => "?" });
                sb.Append(" ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

}

public enum AIStrength
{
    whet,
    nuub,
    normal,
    professional
}