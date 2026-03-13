using System.Numerics;
using static OthelloDebugLog;

/// <summary>
/// 評価関数の内訳（デバッグ表示用）
/// </summary>
public struct EvalDetail
{
    public int Position;      // 位置評価の生値
    public int Mobility;      // 機動力の生値
    public int Stability;     // 安定石の生値
    public int StoneDiff;     // 駒数差
    public int Parity;          // パリティ (+1=有利, -1=不利)
    public int Frontier;        // フロンティアスコア（相手フロンティア多-自分フロンティア多 を正規化）
    public int PosWeight;       // 位置評価の重み
    public int MobWeight;       // 機動力の重み
    public int StaWeight;       // 安定石の重み
    public int DiffWeight;      // 駒数差の重み
    public int ParityWeight;    // パリティの重み
    public int FrontierWeight;  // フロンティアの重み

    public int Total => Position * PosWeight + Mobility * MobWeight
                      + Stability * StaWeight + StoneDiff * DiffWeight
                      + Parity * ParityWeight + Frontier * FrontierWeight;

    public override string ToString()
        => $"位置={Position}×{PosWeight}={Position * PosWeight}  "
         + $"機動力={Mobility}×{MobWeight}={Mobility * MobWeight}  "
         + $"安定石={Stability}×{StaWeight}={Stability * StaWeight}  "
         + $"駒数差={StoneDiff}×{DiffWeight}={StoneDiff * DiffWeight}  "
         + $"パリティ={Parity}×{ParityWeight}={Parity * ParityWeight}  "
         + $"フロンティア={Frontier}×{FrontierWeight}={Frontier * FrontierWeight}  "
         + $"合計={Total}";
}

/// <summary>
/// ビットボード + 反復深化 + 置換表 + 完全読み + PVS（NegaScout）による最強オセロAI
/// </summary>
public class OthelloAI
{
    private readonly WeightSet _weights;
    private readonly int[] _positionTable;

    /// <summary>デフォルト重みで生成（既存コードとの後方互換）</summary>
    public OthelloAI() : this(WeightSet.Default()) { }

    /// <summary>WeightSetを注入して生成（学習モード用）</summary>
    public OthelloAI(WeightSet weights)
    {
        _weights = weights;
        _positionTable = weights.PositionTable;
    }

    private int _maxDepth;
    private int _recursionsCount;
    private int _abCutCount;      // αβカット発生回数
    private int _pvsResearchCount; // PVS再探索回数
    private bool _perfSolveTriggered; // 完全読みに移行したか
    private int _lastProgressLogNodes; // 完全読み進捗ログ用

    // Killer Move: 各深さで2手記録（βカットを起こした手）
    private int[,] _killerMoves = new int[0, 2];

    // History Heuristic: 各マスのカットスコア（深さ²を加算、反復深化ごとに老化）
    private readonly int[] _historyTable = new int[64];

    private readonly TranspositionTable _tt = new();

    // 完全読みに切り替える残り空きマス数の閾値。
    // 残り18マス以下で完全読みに移行する根拠:
    // - αβ枝刈り + 置換表 + Move Orderingにより professional強度でも数秒以内に完了（実測値ベース）
    // - 14→18 に拡張: 終盤精度が大幅に向上。思考時間が伸びる場合は14〜16に戻すこと
    private const int PerfectSolveThreshold = 18;

    // 評価値の無限大（FinalScore の ±Inf±石数差 表現と区別するため -1 を使う）
    private const int Inf = 1_000_000_000;

    public (int x, int y) AI(List<(int x, int y)> _validMoves, MainGameData _gamedata, bool _isDebug)
    {
        OthelloDebugLog._isDebug = _isDebug;
        _recursionsCount = 0;
        _abCutCount = 0;
        _pvsResearchCount = 0;
        _perfSolveTriggered = false;
        _lastProgressLogNodes = 0;
        StartAILog(_gamedata._turnCounter);

        _maxDepth = _gamedata._AIStrength switch
        {
            AIStrength.Beginner => 3,
            AIStrength.normal => 6,
            AIStrength.expert => 10,
            AIStrength.professional => 16,
            _ => 6
        };

        BitBoard board = BitBoard.FromMainGameData(_gamedata);
        InfoLog($"AI思考開始 ({_gamedata._AIStrength}: 深さ{_maxDepth}  残りマス={board.EmptyCount})");
        _tt.Clear();

        // Killer Move テーブル初期化
        _killerMoves = new int[_maxDepth + 2, 2];
        for (int d = 0; d < _maxDepth + 2; d++) { _killerMoves[d, 0] = -1; _killerMoves[d, 1] = -1; }

        // History Heuristic テーブルリセット
        Array.Clear(_historyTable, 0, 64);

        int bestMove = -1;
        EvalDetail bestDetail = default;
        int prevScore = 0; // Aspiration Window 用（前の深さのスコア）

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

            // History Heuristic 老化（深さが進むたびに半減）
            for (int i = 0; i < 64; i++) _historyTable[i] >>= 1;

            int iterBest = -1;
            int score;

            // Aspiration Window: depth>=3 は前の深さのスコアを基準に探索窓を絞る
            // Delta=50 の根拠: 評価値の通常変動幅（50〜200程度）に対して適度な窓幅。
            // 小さすぎると再探索が頻発し、大きすぎると窓の効果が薄れる。
            const int AspirationDelta = 50;
            if (depth <= 2)
            {
                score = NegaScout(board, depth, -Inf, Inf, ref iterBest);
            }
            else
            {
                int lo = prevScore - AspirationDelta;
                int hi = prevScore + AspirationDelta;
                score = NegaScout(board, depth, lo, hi, ref iterBest);
                if (score <= lo)        // fail-low: 下限を広げて再探索
                {
                    iterBest = -1;
                    score = NegaScout(board, depth, -Inf, hi, ref iterBest);
                }
                else if (score >= hi)   // fail-high: 上限を広げて再探索
                {
                    iterBest = -1;
                    score = NegaScout(board, depth, lo, Inf, ref iterBest);
                }
            }
            prevScore = score;

            if (iterBest >= 0) bestMove = iterBest;

            // 反復深化の進捗表示（InfoLog: デバッグOFFでも表示）
            InfoLog($"深さ {depth,2} 完了: 最善手={BitBoard.BitToCoord(bestMove)}  スコア={score,10}  ノード={_recursionsCount}");
        }

        // 最善手の評価内訳をデバッグ表示（完全読みの場合は中間状態の評価は意味が薄いので省略）
        if (bestMove >= 0 && !_perfSolveTriggered)
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

        // stackalloc でGCフリーな合法手バッファを確保
        Span<int> movesBuffer = stackalloc int[64];
        int moveCount = BitBoard.GetMoveArray(movesBit, movesBuffer);
        Span<int> moves = movesBuffer[..moveCount];

        // Move Ordering 1: 置換表の最善手を先頭にスワップ
        int insertPos = 0;
        if (ttBestMove >= 0)
        {
            for (int k = 0; k < moveCount; k++)
            {
                if (moves[k] == ttBestMove)
                {
                    moves[k] = moves[0]; moves[0] = ttBestMove;
                    insertPos = 1;
                    break;
                }
            }
        }

        // Move Ordering 2: Killer Move（TT手の直後に配置）
        int killer1 = depth < _killerMoves.GetLength(0) ? _killerMoves[depth, 0] : -1;
        int killer2 = depth < _killerMoves.GetLength(0) ? _killerMoves[depth, 1] : -1;
        if (killer1 >= 0 && killer1 != ttBestMove)
        {
            for (int k = insertPos; k < moveCount; k++)
            {
                if (moves[k] == killer1)
                {
                    if (k != insertPos) { moves[k] = moves[insertPos]; moves[insertPos] = killer1; }
                    insertPos++;
                    break;
                }
            }
        }
        if (killer2 >= 0 && killer2 != ttBestMove && killer2 != killer1)
        {
            for (int k = insertPos; k < moveCount; k++)
            {
                if (moves[k] == killer2)
                {
                    if (k != insertPos) { moves[k] = moves[insertPos]; moves[insertPos] = killer2; }
                    insertPos++;
                    break;
                }
            }
        }

        // Move Ordering 3: 残り手を History スコア降順で挿入ソート（手数が少ないためO(n²)で十分）
        for (int k = insertPos + 1; k < moveCount; k++)
        {
            int key = moves[k];
            int keyScore = _historyTable[key];
            int j = k - 1;
            while (j >= insertPos && _historyTable[moves[j]] < keyScore)
            {
                moves[j + 1] = moves[j];
                j--;
            }
            moves[j + 1] = key;
        }

        int localBestMove = moves[0];
        int bestScore = -Inf;

        for (int i = 0; i < moveCount; i++)
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
                // LMR（Late Move Reductions）:
                // Move Ordering で後半の手（TT/Killer以外）は外れの可能性が高い。
                // depth-2 で Null Window 探索し、alpha を超えた場合のみ full depth で再探索する。
                // 条件: depth>=3 かつ insertPos（TT/Killer枠）より後の手
                int dummy = -1;
                bool canLMR = depth >= 3 && i >= Math.Max(2, insertPos);
                int lmrDepth = canLMR ? depth - 2 : depth - 1;

                score = -NegaScout(next, lmrDepth, -alpha - 1, -alpha, ref dummy);

                // LMR で alpha を超えた → full depth の Null Window で再確認
                if (canLMR && score > alpha)
                    score = -NegaScout(next, depth - 1, -alpha - 1, -alpha, ref dummy);

                // PVS再探索: Null Window を超えた場合のみ（この手が PV 候補）
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
                // Killer Move 更新（静寂手のみ：TT手でなければ登録）
                if (move != ttBestMove && depth < _killerMoves.GetLength(0))
                {
                    if (move != _killerMoves[depth, 0])
                    {
                        _killerMoves[depth, 1] = _killerMoves[depth, 0];
                        _killerMoves[depth, 0] = move;
                    }
                }
                // History Heuristic 更新
                _historyTable[move] += depth * depth;
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

        // stackalloc でGCフリーな合法手バッファを確保
        Span<int> movesBuffer = stackalloc int[64];
        int moveCount = BitBoard.GetMoveArray(movesBit, movesBuffer);
        Span<int> moves = movesBuffer[..moveCount];

        // Move Ordering: 置換表の最善手を先頭にスワップ
        int sortStart = 0;
        if (ttBestMove >= 0)
        {
            for (int k = 0; k < moveCount; k++)
            {
                if (moves[k] == ttBestMove)
                {
                    moves[k] = moves[0]; moves[0] = ttBestMove;
                    sortStart = 1;
                    break;
                }
            }
        }

        // Move Ordering: コーナー優先（終盤は角を確実に取る）
        const ulong CornerMask = 0x8100000000000081UL;
        for (int ci = moveCount - 1; ci >= sortStart; ci--)
        {
            if (((1UL << moves[ci]) & CornerMask) != 0)
            {
                int tmp = moves[sortStart]; moves[sortStart] = moves[ci]; moves[ci] = tmp;
                sortStart++;
            }
        }

        // Move Ordering: 残り12マス以下のみ最小機動力原理（相手の応手数昇順）で挿入ソート
        if (board.EmptyCount <= 12 && moveCount - sortStart > 1)
        {
            // 機動力を事前計算（挿入ソート内での重複呼び出しを避ける）
            Span<int> mobBuffer = stackalloc int[64];
            for (int k = sortStart; k < moveCount; k++)
                mobBuffer[k] = BitOperations.PopCount(board.DoMove(moves[k]).GetMoves());

            for (int k = sortStart + 1; k < moveCount; k++)
            {
                int keyMove = moves[k];
                int keyMob  = mobBuffer[k];
                int j = k - 1;
                while (j >= sortStart && mobBuffer[j] > keyMob)
                {
                    moves[j + 1]    = moves[j];
                    mobBuffer[j + 1] = mobBuffer[j];
                    j--;
                }
                moves[j + 1]    = keyMove;
                mobBuffer[j + 1] = keyMob;
            }
        }

        int bestScore = -Inf;
        int localBestMove = moves[0];

        for (int i = 0; i < moveCount; i++)
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
    private int BitBoardEvaluate(BitBoard board, out EvalDetail detail)
    {
        int total = board.PlayerCount + board.OpponentCount;
        detail = default;

        if (total >= 54) // 終盤: 駒数差だけで勝敗を正確に判定する
        {
            detail.StoneDiff = board.PlayerCount - board.OpponentCount;
            detail.DiffWeight = 1;
            return detail.Total;
        }
        else if (total >= 20) // 中盤: バランス型。安定石・位置・機動力・駒数差を総合評価
        {
            detail.Position  = PositionScore(board);
            detail.Mobility  = MobilityScore(board);
            detail.Stability = StabilityScore(board);
            detail.StoneDiff = board.PlayerCount - board.OpponentCount;
            detail.Parity    = board.EmptyCount % 2 == 1 ? 1 : -1;
            detail.Frontier  = FrontierScore(board);
            detail.PosWeight      = _weights.Mid.PosWeight;
            detail.MobWeight      = _weights.Mid.MobWeight;
            detail.StaWeight      = _weights.Mid.StaWeight;
            detail.DiffWeight     = _weights.Mid.DiffWeight;
            detail.ParityWeight   = _weights.Mid.ParityWeight;
            detail.FrontierWeight = _weights.Mid.FrontierWeight;
        }
        else // 序盤: 位置と安定石を重視。序盤の駒数差は終盤で容易に逆転されるため無視
        {
            detail.Position  = PositionScore(board);
            detail.Mobility  = MobilityScore(board);
            detail.Stability = StabilityScore(board);
            detail.StoneDiff = board.PlayerCount - board.OpponentCount;
            detail.Parity    = board.EmptyCount % 2 == 1 ? 1 : -1;
            detail.Frontier  = FrontierScore(board);
            detail.PosWeight      = _weights.Early.PosWeight;
            detail.MobWeight      = _weights.Early.MobWeight;
            detail.StaWeight      = _weights.Early.StaWeight;
            detail.DiffWeight     = _weights.Early.DiffWeight;
            detail.ParityWeight   = _weights.Early.ParityWeight;
            detail.FrontierWeight = _weights.Early.FrontierWeight;
        }

        return detail.Total;
    }

    // 位置評価テーブルは _positionTable（WeightSetから注入）を使用

    private int PositionScore(BitBoard board)
    {
        int score = 0;
        ulong p = board.Player;
        ulong o = board.Opponent;
        while (p != 0)
        {
            int pos = BitOperations.TrailingZeroCount(p);
            score += _positionTable[pos];
            p &= p - 1;
        }
        while (o != 0)
        {
            int pos = BitOperations.TrailingZeroCount(o);
            score -= _positionTable[pos];
            o &= o - 1;
        }

        // X-square動的補正: コーナーが埋まっていれば対応X-squareの重みを緩和（-250→+5）
        // X-square: bit9(B2), bit14(G2), bit49(B7), bit54(G7)
        // コーナー: bit0(A1), bit7(H1), bit56(A8), bit63(H8)
        ulong all = board.Player | board.Opponent;
        const int XSquareCorrection = 255; // -250 を +5 にする差分
        ReadOnlySpan<(int xSq, int corner)> pairs = [(9, 0), (14, 7), (49, 56), (54, 63)];
        foreach (var (xSq, corner) in pairs)
        {
            if (((all >> corner) & 1) == 1) // コーナーが埋まっている
            {
                if (((board.Player >> xSq) & 1) == 1)   score += XSquareCorrection;
                if (((board.Opponent >> xSq) & 1) == 1) score -= XSquareCorrection;
            }
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
    /// フロンティア評価: 空きマスに隣接する石（ひっくり返されやすい不安定石）を評価。
    /// 自分のフロンティアが少ない＝有利。スコアは [-100, +100] の正規化値。
    /// </summary>
    private static int FrontierScore(BitBoard board)
    {
        ulong empty = ~(board.Player | board.Opponent);

        // 空きマスを全8方向に1マス拡張し、石に隣接するマスを求める
        const ulong NotAFile = 0xFEFEFEFEFEFEFEFEUL; // 左端列ラップ防止
        const ulong NotHFile = 0x7F7F7F7F7F7F7F7FUL; // 右端列ラップ防止
        ulong adj = 0UL;
        adj |= empty << 8;               // 下
        adj |= empty >> 8;               // 上
        adj |= (empty & NotHFile) << 1;  // 右
        adj |= (empty & NotAFile) >> 1;  // 左
        adj |= (empty & NotHFile) << 9;  // 右下
        adj |= (empty & NotAFile) << 7;  // 左下
        adj |= (empty & NotHFile) >> 7;  // 右上
        adj |= (empty & NotAFile) >> 9;  // 左上

        int myFrontier  = BitOperations.PopCount(adj & board.Player);
        int oppFrontier = BitOperations.PopCount(adj & board.Opponent);
        int total = myFrontier + oppFrontier;
        // 自分のフロンティアが少ないほど正の値（有利）
        return total == 0 ? 0 : 100 * (oppFrontier - myFrontier) / total;
    }

    /// <summary>
    /// 安定石評価：コーナーおよびコーナーから連続している安定石を評価。
    /// 両プレイヤーを一度のスキャンで計算することで重複処理を排除。
    /// </summary>
    private static int StabilityScore(BitBoard board)
    {
        var (myStable, oppStable) = CountStableBoth(board.Player, board.Opponent);
        return myStable - oppStable;
    }

    /// <summary>
    /// 自分と相手の安定石数を一度のスキャンで同時に計算する。
    /// コーナーを起点に水平・垂直方向へ連結している確定石を伝播で求める。
    /// </summary>
    private static (int myStable, int oppStable) CountStableBoth(ulong mine, ulong opp)
    {
        ulong stableMine = 0UL;
        ulong stableOpp  = 0UL;
        ulong all = mine | opp;

        // コーナー（確定的に安定）を起点にする
        ulong corners = 0x8100000000000081UL;
        stableMine |= corners & mine;
        stableOpp  |= corners & opp;

        if (stableMine == 0 && stableOpp == 0) return (0, 0);

        bool changed = true;
        while (changed)
        {
            changed = false;
            ulong newStableMine = stableMine;
            ulong newStableOpp  = stableOpp;

            // 行方向（水平）の安定石伝播
            for (int row = 0; row < 8; row++)
            {
                ulong rowMask    = 0xFFUL << (row * 8);
                ulong rowAll     = all  & rowMask;
                ulong rowMine    = mine & rowMask;
                ulong rowOpp     = opp  & rowMask;
                ulong rowStableM = stableMine & rowMask;
                ulong rowStableO = stableOpp  & rowMask;

                if (rowAll == rowMask)
                {
                    // 行が全て埋まっている場合は全ての自石・相手石が安定
                    newStableMine |= rowMine;
                    newStableOpp  |= rowOpp;
                }
                else
                {
                    // 既存の安定石から左右に連結伝播
                    if (rowStableM != 0)
                    {
                        ulong lf = rowStableM;
                        for (int i = 0; i < 7; i++) lf |= rowMine & ((lf << 1) & rowMask);
                        ulong rf = rowStableM;
                        for (int i = 0; i < 7; i++) rf |= rowMine & ((rf >> 1) & rowMask);
                        newStableMine |= lf & rf;
                    }
                    if (rowStableO != 0)
                    {
                        ulong lf = rowStableO;
                        for (int i = 0; i < 7; i++) lf |= rowOpp & ((lf << 1) & rowMask);
                        ulong rf = rowStableO;
                        for (int i = 0; i < 7; i++) rf |= rowOpp & ((rf >> 1) & rowMask);
                        newStableOpp |= lf & rf;
                    }
                }
            }

            // 列方向（垂直）の安定石伝播
            for (int col = 0; col < 8; col++)
            {
                ulong colMask    = 0x0101010101010101UL << col;
                ulong colAll     = all  & colMask;
                ulong colMine    = mine & colMask;
                ulong colOpp     = opp  & colMask;
                ulong colStableM = stableMine & colMask;
                ulong colStableO = stableOpp  & colMask;

                if (colAll == colMask)
                {
                    newStableMine |= colMine;
                    newStableOpp  |= colOpp;
                }
                else
                {
                    if (colStableM != 0)
                    {
                        ulong uf = colStableM;
                        for (int i = 0; i < 7; i++) uf |= colMine & ((uf << 8) & colMask);
                        ulong df = colStableM;
                        for (int i = 0; i < 7; i++) df |= colMine & ((df >> 8) & colMask);
                        newStableMine |= uf & df;
                    }
                    if (colStableO != 0)
                    {
                        ulong uf = colStableO;
                        for (int i = 0; i < 7; i++) uf |= colOpp & ((uf << 8) & colMask);
                        ulong df = colStableO;
                        for (int i = 0; i < 7; i++) df |= colOpp & ((df >> 8) & colMask);
                        newStableOpp |= uf & df;
                    }
                }
            }

            if (newStableMine != stableMine || newStableOpp != stableOpp)
            {
                stableMine = newStableMine;
                stableOpp  = newStableOpp;
                changed = true;
            }
        }

        return (BitOperations.PopCount(stableMine), BitOperations.PopCount(stableOpp));
    }
}

public enum AIStrength
{
    Beginner,
    normal,
    expert,
    professional
}
