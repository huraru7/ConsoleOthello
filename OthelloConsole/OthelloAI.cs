using System.Numerics;
using static OthelloDebugLog;

/// <summary>
/// 評価関数の内訳（デバッグ表示用）
/// </summary>
public struct EvalDetail
{
    public int Position;   // 位置評価の生値
    public int Mobility;   // 機動力の生値
    public int Stability;  // 安定石の生値
    public int StoneDiff;  // 駒数差
    public int PosWeight;  // 位置評価の重み
    public int MobWeight;  // 機動力の重み
    public int StaWeight;  // 安定石の重み
    public int DiffWeight; // 駒数差の重み

    public int Total => Position * PosWeight + Mobility * MobWeight
                      + Stability * StaWeight + StoneDiff * DiffWeight;

    public override string ToString()
        => $"位置={Position}×{PosWeight}={Position * PosWeight}  "
         + $"機動力={Mobility}×{MobWeight}={Mobility * MobWeight}  "
         + $"安定石={Stability}×{StaWeight}={Stability * StaWeight}  "
         + $"駒数差={StoneDiff}×{DiffWeight}={StoneDiff * DiffWeight}  "
         + $"合計={Total}";
}

/// <summary>
/// ビットボード + 反復深化 + 置換表 + 完全読み + PVS（NegaScout）による最強オセロAI
/// </summary>
public class OthelloAI
{
    private int _maxDepth;
    private int _recursionsCount;
    private int _abCutCount;      // αβカット発生回数
    private int _pvsResearchCount; // PVS再探索回数
    private bool _perfSolveTriggered; // 完全読みに移行したか
    private int _lastProgressLogNodes; // 完全読み進捗ログ用

    private readonly TranspositionTable _tt = new();

    // 完全読みに切り替える残り空きマス数の閾値
    private const int PerfectSolveThreshold = 14;

    // 評価値の無限大
    private const int Inf = 1_000_000_000;

    public (int x, int y) AI(List<(int x, int y)> _validMoves, MainGameData _gamedata, bool _isDebug)
    {
        OthelloDebugLog._isDebug = _isDebug;
        _recursionsCount = 0;
        _abCutCount = 0;
        _pvsResearchCount = 0;
        _perfSolveTriggered = false;
        _lastProgressLogNodes = 0;
        StartAILog(_gamedata._turnConter);

        _maxDepth = _gamedata._AIStrength switch
        {
            AIStrength.nuub => 3,
            AIStrength.normal => 6,
            AIStrength.expert => 10,
            AIStrength.professional => 16,
            _ => 6
        };

        BitBoard board = BitBoard.FromMainGameData(_gamedata);
        InfoLog($"AI思考開始 ({_gamedata._AIStrength}: 深さ{_maxDepth}  残りマス={board.EmptyCount})");
        _tt.Clear();

        int bestMove = -1;
        EvalDetail bestDetail = default;

        // 反復深化
        for (int depth = 1; depth <= _maxDepth; depth++)
        {
            // ルートが完全読み閾値以下なら完全読みを直接実行（NegaScout途中からの完全読みを防ぐ）
            if (board.EmptyCount <= PerfectSolveThreshold)
            {
                _perfSolveTriggered = true;
                InfoLog($"残り{board.EmptyCount}マス → 完全読みモードへ切り替え");
                int perfScore = SolvePerfect(board, -Inf, Inf);
                // 置換表からルートの最善手を取得
                ulong rootHash = _tt.ComputeHash(board);
                if (_tt.TryGetBestMoveOnly(rootHash, out int ttBest) && ttBest >= 0)
                    bestMove = ttBest;
                string perfScoreStr = Math.Abs(perfScore) >= Inf - 1
                    ? (perfScore > 0 ? $"勝ち(+{perfScore - (Inf - 1)}石差)" : $"負け(-{-perfScore - (Inf - 1)}石差)")
                    : $"{perfScore}（引き分け）";
                InfoLog($"完全読み完了: 最善手={BitBoard.BitToCoord(bestMove)}  スコア={perfScoreStr}  ノード={_recursionsCount}");
                break;
            }

            int iterBest = -1;
            int score = NegaScout(board, depth, -Inf, Inf, ref iterBest);
            if (iterBest >= 0) bestMove = iterBest;

            // 反復深化の進捗表示（InfoLog: デバッグOFFでも表示）
            InfoLog($"深さ {depth,2} 完了: 最善手={BitBoard.BitToCoord(bestMove)}  スコア={score,10}  ノード={_recursionsCount}");
        }

        // 最善手の評価内訳をデバッグ表示（DoMove後=相手視点のため符号反転して表示）
        if (bestMove >= 0)
        {
            BitBoardEvaluate(board.DoMove(bestMove), out bestDetail);
            DebugLog($"評価内訳(最善手{BitBoard.BitToCoord(bestMove)}, AI視点): 位置={-bestDetail.Position * bestDetail.PosWeight}  機動力={-bestDetail.Mobility * bestDetail.MobWeight}  安定石={-bestDetail.Stability * bestDetail.StaWeight}  駒数差={-bestDetail.StoneDiff * bestDetail.DiffWeight}  合計={-bestDetail.Total}");
        }

        // 探索統計
        DebugLog($"探索統計: αβカット={_abCutCount}  PVS再探索={_pvsResearchCount}  完全読み={(_perfSolveTriggered ? "あり" : "なし")}");
        DebugLog($"置換表: ヒット={_tt.HitCount} ({_tt.HitRate:F1}%)  ミス={_tt.MissCount}");

        GameLog($"AI評価\n探索深さ:{_maxDepth} 総計算手順回数：{_recursionsCount} 最良手:{BitBoard.BitToCoord(bestMove)}");
        StopAILog();

        return BitBoard.BitToCoord(bestMove);
    }

    /// <summary>
    /// PVS（NegaScout）探索。置換表・完全読みを統合。
    /// board.Player = 現在の手番プレイヤーの石。
    /// </summary>
    private int NegaScout(BitBoard board, int depth, int alpha, int beta, ref int bestMove)
    {
        ulong hash = _tt.ComputeHash(board);

        // 置換表参照
        if (_tt.TryGet(hash, depth, alpha, beta, out int ttScore, out int ttBestMove))
        {
            if (bestMove == -1 && ttBestMove >= 0)
                bestMove = ttBestMove;
            return ttScore;
        }

        // 葉ノード
        if (depth <= 0)
        {
            _recursionsCount++;
            int eval = BitBoardEvaluate(board, out _);
            _tt.Store(hash, 0, eval, alpha, beta, -1);
            return eval;
        }

        ulong movesBit = board.GetMoves();

        // パスの処理
        if (movesBit == 0)
        {
            BitBoard passed = new BitBoard(board.Opponent, board.Player);
            if (passed.GetMoves() == 0)
            {
                int finalScore = FinalScore(board);
                _tt.Store(hash, depth, finalScore, alpha, beta, -1);
                return finalScore;
            }
            _recursionsCount++;
            int passScore = -NegaScout(new BitBoard(board.Opponent, board.Player), depth - 1, -beta, -alpha, ref bestMove);
            _tt.Store(hash, depth, passScore, alpha, beta, -1);
            return passScore;
        }

        List<int> moves = BitBoard.GetMoveList(movesBit);

        // Move Ordering: 置換表の最善手を先頭に
        if (ttBestMove >= 0 && moves.Contains(ttBestMove))
        {
            moves.Remove(ttBestMove);
            moves.Insert(0, ttBestMove);
        }

        int localBestMove = moves[0];
        int bestScore = -Inf;

        for (int i = 0; i < moves.Count; i++)
        {
            int move = moves[i];
            BitBoard next = board.DoMove(move);
            _recursionsCount++;

            int score;
            if (i == 0)
            {
                // PV手はフルウィンドウで探索
                int dummy = -1;
                score = -NegaScout(next, depth - 1, -beta, -alpha, ref dummy);
            }
            else
            {
                // null window 探索（PVS）
                int dummy = -1;
                score = -NegaScout(next, depth - 1, -alpha - 1, -alpha, ref dummy);
                // null windowを超えた場合のみ再探索
                if (score > alpha && score < beta)
                {
                    _pvsResearchCount++;
                    score = -NegaScout(next, depth - 1, -beta, -alpha, ref dummy);
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                localBestMove = move;
                if (depth == _maxDepth || board.EmptyCount > PerfectSolveThreshold)
                    bestMove = move;
            }

            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
            {
                _abCutCount++;
                _tt.Store(hash, depth, bestScore, alpha, beta, localBestMove);
                return bestScore;
            }
        }

        _tt.Store(hash, depth, bestScore, alpha, beta, localBestMove);
        return bestScore;
    }

    /// <summary>
    /// 完全読み（終盤ソルバー）。評価関数を使わず駒数差のみで探索。
    /// </summary>
    private int SolvePerfect(BitBoard board, int alpha, int beta)
    {
        // 100万ノードごとに進捗ログ
        if (_recursionsCount - _lastProgressLogNodes >= 1_000_000)
        {
            _lastProgressLogNodes = _recursionsCount;
            InfoLog($"完全読み進捗: ノード={_recursionsCount}  残り{board.EmptyCount}マス");
        }

        ulong hash = _tt.ComputeHash(board);
        if (_tt.TryGet(hash, 64, alpha, beta, out int ttScore, out int ttBestMove))
            return ttScore;

        ulong movesBit = board.GetMoves();

        if (movesBit == 0)
        {
            BitBoard passed = new BitBoard(board.Opponent, board.Player);
            if (passed.GetMoves() == 0)
            {
                int final = FinalScore(board);
                _tt.Store(hash, 64, final, alpha, beta, -1);
                return final;
            }
            _recursionsCount++;
            int passScore = -SolvePerfect(new BitBoard(board.Opponent, board.Player), -beta, -alpha);
            _tt.Store(hash, 64, passScore, alpha, beta, -1);
            return passScore;
        }

        List<int> moves = BitBoard.GetMoveList(movesBit);

        // Move Ordering: 置換表の最善手を先頭に
        if (ttBestMove >= 0 && moves.Contains(ttBestMove))
        {
            moves.Remove(ttBestMove);
            moves.Insert(0, ttBestMove);
        }

        int bestScore = -Inf;
        int localBestMove = moves[0];

        for (int i = 0; i < moves.Count; i++)
        {
            BitBoard next = board.DoMove(moves[i]);
            _recursionsCount++;

            int score;
            if (i == 0)
            {
                score = -SolvePerfect(next, -beta, -alpha);
            }
            else
            {
                score = -SolvePerfect(next, -alpha - 1, -alpha);
                if (score > alpha && score < beta)
                {
                    _pvsResearchCount++;
                    score = -SolvePerfect(next, -beta, -alpha);
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                localBestMove = moves[i];
            }
            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
            {
                _abCutCount++;
                _tt.Store(hash, 64, bestScore, alpha, beta, localBestMove);
                return bestScore;
            }
        }

        _tt.Store(hash, 64, bestScore, alpha, beta, localBestMove);
        return bestScore;
    }

    /// <summary>
    /// ゲーム終了時のスコア（現プレイヤー視点の駒数差）
    /// </summary>
    private static int FinalScore(BitBoard board)
    {
        int p = board.PlayerCount;
        int o = board.OpponentCount;
        if (p > o) return Inf - 1 + (p - o);
        if (p < o) return -(Inf - 1) - (o - p);
        return 0;
    }

    /// <summary>
    /// ビットボード用評価関数（現プレイヤー視点）。内訳を detail に返す。
    /// </summary>
    private static int BitBoardEvaluate(BitBoard board, out EvalDetail detail)
    {
        int total = board.PlayerCount + board.OpponentCount;
        detail = default;

        if (total >= 54) // 終盤: 駒数差のみ
        {
            detail.StoneDiff = board.PlayerCount - board.OpponentCount;
            detail.DiffWeight = 1;
            return detail.Total;
        }
        else if (total >= 20) // 中盤
        {
            detail.Position = PositionScore(board);
            detail.Mobility = MobilityScore(board);
            detail.Stability = StabilityScore(board);
            detail.StoneDiff = board.PlayerCount - board.OpponentCount;
            detail.PosWeight = 25;
            detail.MobWeight = 10;
            detail.StaWeight = 15;
            detail.DiffWeight = 5;
        }
        else // 序盤
        {
            detail.Position = PositionScore(board);
            detail.Mobility = MobilityScore(board);
            detail.Stability = StabilityScore(board);
            detail.StoneDiff = board.PlayerCount - board.OpponentCount;
            detail.PosWeight = 30;
            detail.MobWeight = 15;
            detail.StaWeight = 20;
            detail.DiffWeight = 0;
        }

        return detail.Total;
    }

    // 位置評価テーブル（研究された重みを使用）
    private static readonly int[] PositionWeight = {
        500, -150,  30,  10,  10,  30, -150,  500,
       -150, -250,  -5,  -5,  -5,  -5, -250, -150,
         30,   -5,  15,   3,   3,  15,   -5,   30,
         10,   -5,   3,   3,   3,   3,   -5,   10,
         10,   -5,   3,   3,   3,   3,   -5,   10,
         30,   -5,  15,   3,   3,  15,   -5,   30,
       -150, -250,  -5,  -5,  -5,  -5, -250, -150,
        500, -150,  30,  10,  10,  30, -150,  500,
    };

    private static int PositionScore(BitBoard board)
    {
        int score = 0;
        ulong p = board.Player;
        ulong o = board.Opponent;
        while (p != 0)
        {
            int pos = BitOperations.TrailingZeroCount(p);
            score += PositionWeight[pos];
            p &= p - 1;
        }
        while (o != 0)
        {
            int pos = BitOperations.TrailingZeroCount(o);
            score -= PositionWeight[pos];
            o &= o - 1;
        }
        return score;
    }

    private static int MobilityScore(BitBoard board)
    {
        int myMoves = BitOperations.PopCount(board.GetMoves());
        BitBoard swapped = new BitBoard(board.Opponent, board.Player);
        int oppMoves = BitOperations.PopCount(swapped.GetMoves());
        int total = myMoves + oppMoves;
        return total == 0 ? 0 : 100 * (myMoves - oppMoves) / total;
    }

    /// <summary>
    /// 安定石評価：コーナーおよびコーナーから連続している安定石を評価
    /// </summary>
    private static int StabilityScore(BitBoard board)
    {
        int myStable = CountStable(board.Player, board.Opponent);
        int oppStable = CountStable(board.Opponent, board.Player);
        return myStable - oppStable;
    }

    private static int CountStable(ulong mine, ulong opp)
    {
        ulong stable = 0UL;
        ulong all = mine | opp;

        ulong corners = 0x8100000000000081UL;
        stable |= corners & mine;

        if (stable == 0) return 0;

        bool changed = true;
        while (changed)
        {
            changed = false;
            ulong newStable = stable;

            for (int row = 0; row < 8; row++)
            {
                ulong rowMask = 0xFFUL << (row * 8);
                ulong rowAll = all & rowMask;
                ulong rowMine = mine & rowMask;
                ulong rowStable = stable & rowMask;

                if (rowAll == rowMask)
                    newStable |= rowMine;
                else if (rowStable != 0)
                {
                    ulong leftFill = rowStable;
                    for (int i = 0; i < 7; i++)
                        leftFill |= rowMine & (leftFill << 1) & rowMask;
                    ulong rightFill = rowStable;
                    for (int i = 0; i < 7; i++)
                        rightFill |= rowMine & (rightFill >> 1) & rowMask;
                    newStable |= leftFill & rightFill;
                }
            }

            if (newStable != stable) { stable = newStable; changed = true; }
        }

        return BitOperations.PopCount(stable);
    }
}

public enum AIStrength
{
    nuub,
    normal,
    expert,
    professional
}
