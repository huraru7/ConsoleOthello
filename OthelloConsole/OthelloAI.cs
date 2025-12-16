using static OthelloSystem;
using static EvaluationFunction;
using static OthelloDebugLog;

public class OthelloAI
{
    private int _maxDepth;
    private int _depth;
    private int _recursionsCount; //計算回数
    public (int x, int y) AI(List<(int x, int y)> _validMoves, MainGameData _gamedata, bool _isDebug)
    {
        OthelloDebugLog._isDebug = _isDebug;

        (int, int) bestMove = (0, 0);//仮です
        _recursionsCount = 0;
        StartAILog();

        switch (_gamedata._AIStrength)
        {
            case AIStrength.nuub: _maxDepth = 2; break;
            case AIStrength.normal: _maxDepth = 4; break;
            case AIStrength.expert: _maxDepth = 6; break;
            case AIStrength.professional: _maxDepth = 8; break;
        }
        _depth = _maxDepth;

        DebugLog($"探索深さ: {_depth}");
        DebugLog("=============Ai評価開始=============");
        int maxScore = int.MinValue;
        foreach (var move in _validMoves)
        {
            MainGameData newGameData = _gamedata.Clone();
            _recursionsCount++;
            DebugLog($"試行純手:({move.x},{move.y}) 深さ:{_depth}スタート");
            PlacePiece(move.x, move.y, newGameData);
            // DebugLog($"盤面状態:\n{BoardToString(newGameData._tiles)}");
            int score = -Negamax(newGameData, _depth - 1, int.MinValue, int.MaxValue, false);
            if (score > maxScore)
            {
                DebugLog($"スコア更新:({move.x},{move.y}) スコア:{score}");
                maxScore = score;
                bestMove = move;
            }
        }
        DebugLog("=============Ai評価終了=============");
        DebugLog($"探索深さ:{_maxDepth} 総計算手順回数：{_recursionsCount} 最良手:{bestMove} スコア:{maxScore}");
        GameLog($"AI評価\n探索深さ:{_maxDepth} 総計算手順回数：{_recursionsCount} 最良手:{bestMove} スコア:{maxScore}");

        StopAILog();
        return bestMove;
    }

    private int Negamax(MainGameData _gamedata, int _depth, int alpha, int beta, bool trigger)
    {
        string indent = new string(' ', (_maxDepth - _depth) * 2) + " ";
        _gamedata._gameTurn = _depth % 2 == 0 ? GameTurn.AI : GameTurn.prayer;
        _gamedata._turnNum = _depth % 2 == 0 ? 2 : 1;

        if (_depth <= 0)//深さ0は評価値を返す
        {
            int evaluation = EvaFunction(_gamedata);
            DebugLog($"{indent}▶︎深さ0到達 評価値:{evaluation}");
            return evaluation;
        }

        var validMoves = InstallationArea(_gamedata);
        if (validMoves.Count == 0)
        {
            //2回連続で置けない場合はゲームは終了している。
            if (trigger)
            {
                DebugLog($"{indent}▶︎2回連続スキップでゲーム終了");
                return EvaFunction(_gamedata);
            }

            //スキップする処理
            _recursionsCount++;
            DebugLog($"{indent}▶ スキップ発生");
            int score = -Negamax(_gamedata, _depth - 1, -beta, -alpha, true);
            return Math.Max(alpha, score);
        }

        int bestScore = int.MinValue;

        foreach (var move in validMoves)
        {
            MainGameData newGameData = _gamedata.Clone();
            _recursionsCount++;
            PlacePiece(move.x, move.y, newGameData);
            DebugLog($"{indent}▶︎試行純手:({move.x},{move.y}) 深さ:{_depth}スタート");
            // DebugLog($"{indent}盤面状態:\n{BoardToString(newGameData._tiles)}");
            int score = -Negamax(newGameData, _depth - 1, -beta, -alpha, false);
            bestScore = Math.Max(bestScore, score);
            alpha = Math.Max(alpha, score);

            if (alpha >= beta)
            {
                DebugLog($"{indent}▶︎αβカット発生 深さ:{_depth} α値:{alpha} >= β値:{beta}");
                break;
            }
        }
        DebugLog($"{indent}▶︎深さ:{_depth} 最大スコア:{bestScore}");
        return alpha;
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
    nuub,
    normal,
    expert,
    professional
}