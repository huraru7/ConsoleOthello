using System.Numerics;

/// <summary>
/// 置換表エントリの種類（αβウィンドウとの関係）
/// </summary>
public enum TTEntryType : byte
{
    Exact,       // 正確な評価値（ウィンドウ内）
    LowerBound,  // βカット発生（真の値 >= score）
    UpperBound,  // αカット発生（真の値 <= score）
}

/// <summary>
/// 置換表の1エントリ
/// </summary>
public struct TTEntry
{
    public ulong Hash;
    public int Score;
    public int BestMove;   // 最善手のビット位置（-1=なし）
    public byte Depth;
    public TTEntryType Type;
}

/// <summary>
/// Zobristハッシュを用いた置換表。
/// 同一局面の再評価を防ぎ探索を大幅に高速化する。
/// </summary>
public class TranspositionTable
{
    private const int TableSizeBits = 22; // 約400万エントリ
    private const int TableSize = 1 << TableSizeBits;
    private const int TableMask = TableSize - 1;

    private readonly TTEntry[] _table = new TTEntry[TableSize];
    private readonly ulong[,] _zobristTable; // [位置64][色2]

    // 統計カウンター
    public int HitCount { get; private set; }
    public int MissCount { get; private set; }
    public int Total => HitCount + MissCount;
    public float HitRate => Total == 0 ? 0f : (float)HitCount / Total * 100f;

    public void ResetStats()
    {
        HitCount = 0;
        MissCount = 0;
    }

    public TranspositionTable()
    {
        _zobristTable = new ulong[64, 2];
        var rng = new Random(12345); // 固定シードで再現性確保
        for (int i = 0; i < 64; i++)
        {
            _zobristTable[i, 0] = NextUInt64(rng);
            _zobristTable[i, 1] = NextUInt64(rng);
        }
    }

    private static ulong NextUInt64(Random rng)
    {
        byte[] buf = new byte[8];
        rng.NextBytes(buf);
        return BitConverter.ToUInt64(buf, 0);
    }

    /// <summary>
    /// BitBoardからZobristハッシュを計算する
    /// </summary>
    public ulong ComputeHash(BitBoard board)
    {
        ulong hash = 0UL;
        ulong player = board.Player;
        ulong opponent = board.Opponent;
        while (player != 0)
        {
            int pos = BitOperations.TrailingZeroCount(player);
            hash ^= _zobristTable[pos, 0];
            player &= player - 1;
        }
        while (opponent != 0)
        {
            int pos = BitOperations.TrailingZeroCount(opponent);
            hash ^= _zobristTable[pos, 1];
            opponent &= opponent - 1;
        }
        return hash;
    }

    /// <summary>
    /// 置換表を引く。ヒットした場合は true を返し entry に結果を設定する
    /// </summary>
    public bool TryGet(ulong hash, int depth, int alpha, int beta, out int score, out int bestMove)
    {
        ref TTEntry entry = ref _table[hash & TableMask];
        score = 0;
        bestMove = entry.BestMove;

        if (entry.Hash != hash || entry.Depth < depth)
        {
            MissCount++;
            return false;
        }

        if (entry.Type == TTEntryType.Exact)
        {
            score = entry.Score;
            HitCount++;
            return true;
        }
        if (entry.Type == TTEntryType.LowerBound && entry.Score >= beta)
        {
            score = entry.Score;
            HitCount++;
            return true;
        }
        if (entry.Type == TTEntryType.UpperBound && entry.Score <= alpha)
        {
            score = entry.Score;
            HitCount++;
            return true;
        }
        MissCount++;
        return false;
    }

    /// <summary>
    /// 置換表にエントリを書き込む
    /// </summary>
    public void Store(ulong hash, int depth, int score, int alpha, int beta, int bestMove)
    {
        TTEntryType type;
        if (score <= alpha)
            type = TTEntryType.UpperBound;
        else if (score >= beta)
            type = TTEntryType.LowerBound;
        else
            type = TTEntryType.Exact;

        ref TTEntry entry = ref _table[hash & TableMask];
        // 深い探索結果を優先して上書き
        if (entry.Hash == 0 || entry.Depth <= depth)
        {
            entry.Hash = hash;
            entry.Score = score;
            entry.Depth = (byte)depth;
            entry.Type = type;
            entry.BestMove = bestMove;
        }
    }

    /// <summary>
    /// ETC（Enhanced Transposition Cutoff）用：深さチェックなしで置換表エントリを取得する。
    /// 子局面のカットオフ可能性を事前に検出するために使用する。
    /// </summary>
    public bool TryGetRaw(ulong hash, out TTEntryType type, out int score)
    {
        ref TTEntry entry = ref _table[hash & TableMask];
        if (entry.Hash == hash)
        {
            type  = entry.Type;
            score = entry.Score;
            return true;
        }
        type  = default;
        score = 0;
        return false;
    }

    /// <summary>
    /// 置換表からスコアを見ずに最善手だけを取得する（完全読み後のルート最善手取得用）
    /// </summary>
    public bool TryGetBestMoveOnly(ulong hash, out int bestMove)
    {
        ref TTEntry entry = ref _table[hash & TableMask];
        bestMove = entry.BestMove;
        return entry.Hash == hash && bestMove >= 0;
    }

    /// <summary>
    /// テーブルをクリアする（統計もリセット）
    /// </summary>
    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
        ResetStats();
    }
}
