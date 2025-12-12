using System;
using static OthelloSystem;
using static EvaluationFunction;
using static OthelloDebugLog;

public class OthelloAI
{
    private int _depth = 0;
    private int _recursionsCount; //計算回数

    public (int x, int y) AI(List<(int x, int y)> _validMoves, MainGameData _gamedata, bool _isDebug)
    {
        OthelloDebugLog._isDebug = _isDebug;

        (int, int) bestMove = (0, 0);//仮です
        _recursionsCount = 0;
        StartLogging();

        switch (_gamedata._AIStrength)
        {
            case AIStrength.nuub: _depth = 2; break;
            case AIStrength.normal: _depth = 4; break;
            case AIStrength.professional: _depth = 6; break;
        }

        DebugLog("=============Ai評価開始=============");
        int maxScore = int.MinValue;
        foreach (var move in _validMoves)
        {
            MainGameData newGameData = _gamedata.Clone();
            _recursionsCount++;
            PlacePiece(move.x, move.y, newGameData);
            int score = -Negamax(_depth - 1, newGameData, false);
            if (score > maxScore)
            {
                maxScore = score;
                bestMove = move;
            }
        }
        DebugLog("=============Ai評価終了=============");
        DebugLog($"総計算手順回数：{_recursionsCount}");

        StopLogging();
        return bestMove;
    }

    private int Negamax(int _depth, MainGameData _gamedata, bool trigger)
    {
        if (_depth <= 0)//深さ0は評価値を返す
        {
            return evaluationFunction(_gamedata);
        }

        int maxScore = int.MinValue;
        List<(int x, int y)> validMoves = InstallationArea(_gamedata);
        if (validMoves.Count == 0)
        {
            //2回連続で置けない場合はゲームは終了している。
            if (trigger) return evaluationFunction(_gamedata);

            //スキップする処理
            _recursionsCount++;
            int score = -Negamax(_depth - 1, _gamedata, true);
            maxScore = Math.Max(maxScore, score);
        }

        foreach (var move in validMoves)
        {
            MainGameData newGameData = _gamedata.Clone();
            _recursionsCount++;
            PlacePiece(move.x, move.y, newGameData);
            int score = -Negamax(_depth - 1, newGameData, false);
            maxScore = Math.Max(maxScore, score);
        }
        return maxScore;
    }

    // private string BoardToString(int[,] board)
    // {
    //     int size = board.GetLength(0);
    //     var sb = new System.Text.StringBuilder();
    //     for (int y = 0; y < size; y++)
    //     {
    //         for (int x = 0; x < size; x++)
    //         {
    //             sb.Append(board[y, x] switch { 0 => "-", 1 => "●", 2 => "○", _ => "?" });
    //             sb.Append(" ");
    //         }
    //         sb.AppendLine();
    //     }
    //     return sb.ToString();
    // }
}

public enum AIStrength
{
    nuub,
    normal,
    professional
}