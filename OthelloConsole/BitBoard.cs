using System.Numerics;

/// <summary>
/// ビットボード表現によるオセロ盤面。
/// Player: 現在の手番プレイヤーの石, Opponent: 相手の石。
/// 座標(x,y)はビット位置 y*8+x に対応（左上=ビット0）。
/// </summary>
public readonly struct BitBoard
{
    public readonly ulong Player;
    public readonly ulong Opponent;

    // 盤面全体マスク（8x8=64ビット）
    public const ulong FullBoard = 0xFFFFFFFFFFFFFFFFUL;

    // 左右の端列のマスク（水平シフト時にラップアラウンドを防ぐ）
    private const ulong NotAFile = 0xFEFEFEFEFEFEFEFEUL; // x!=0 の列（右端折り返し防止）
    private const ulong NotHFile = 0x7F7F7F7F7F7F7F7FUL; // x!=7 の列（左端折り返し防止）

    public BitBoard(ulong player, ulong opponent)
    {
        Player = player;
        Opponent = opponent;
    }

    /// <summary>
    /// MainGameDataからBitBoardに変換する
    /// </summary>
    public static BitBoard FromMainGameData(MainGameData data)
    {
        ulong player = 0UL;
        ulong opponent = 0UL;
        int me = data._turnNum;
        int opp = me == 1 ? 2 : 1;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int tile = data._tiles[y, x]; // _tiles[行, 列] の順で格納されている
                int bit = y * 8 + x;
                if (tile == me) player |= 1UL << bit;
                else if (tile == opp) opponent |= 1UL << bit;
            }
        }
        return new BitBoard(player, opponent);
    }

    /// <summary>
    /// BitBoardをMainGameDataのtilesに書き戻す（表示用）
    /// </summary>
    public void ToTiles(int[,] tiles, int me)
    {
        int opp = me == 1 ? 2 : 1;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int bit = y * 8 + x;
                if (((Player >> bit) & 1) == 1) tiles[x, y] = me;
                else if (((Opponent >> bit) & 1) == 1) tiles[x, y] = opp;
                else tiles[x, y] = 0;
            }
        }
    }

    /// <summary>
    /// 現在プレイヤーの合法手をビットボードで返す（8方向シフト演算）
    /// </summary>
    public ulong GetMoves()
    {
        ulong empty = ~(Player | Opponent);
        ulong moves = 0UL;

        // 上
        moves |= GetMovesInDirection(Player, Opponent, 8, FullBoard);
        // 下
        moves |= GetMovesInDirection(Player, Opponent, unchecked((int)-8), FullBoard);
        // 左（x方向-1: HFileをマスク）
        moves |= GetMovesInDirection(Player, Opponent, 1, NotHFile);
        // 右（x方向+1: AFileをマスク）
        moves |= GetMovesInDirection(Player, Opponent, unchecked((int)-1), NotAFile);
        // 左上
        moves |= GetMovesInDirection(Player, Opponent, 9, NotHFile);
        // 右上
        moves |= GetMovesInDirection(Player, Opponent, 7, NotAFile);
        // 左下
        moves |= GetMovesInDirection(Player, Opponent, unchecked((int)-7), NotHFile);
        // 右下
        moves |= GetMovesInDirection(Player, Opponent, unchecked((int)-9), NotAFile);

        return moves & empty;
    }

    // 指定方向に合法手候補を求める
    private static ulong GetMovesInDirection(ulong player, ulong opponent, int shift, ulong mask)
    {
        ulong maskedOpponent = opponent & mask;
        ulong candidates;
        if (shift > 0)
            candidates = maskedOpponent & ((player & mask) << shift);
        else
            candidates = maskedOpponent & ((player & mask) >> (-shift));

        for (int i = 0; i < 5; i++) // 最大6連続なので5回展開
        {
            if (shift > 0)
                candidates |= maskedOpponent & ((candidates & mask) << shift);
            else
                candidates |= maskedOpponent & ((candidates & mask) >> (-shift));
        }

        if (shift > 0)
            return candidates << shift;
        else
            return candidates >> (-shift);
    }

    /// <summary>
    /// 指定ビット位置に石を置いた後の新しいBitBoardを返す（イミュータブル）
    /// </summary>
    public BitBoard DoMove(int pos)
    {
        ulong move = 1UL << pos;
        ulong flipped = GetFlipped(pos);
        ulong newPlayer = Opponent ^ flipped;        // 相手視点で引き継ぐ（プレイヤー交代）
        ulong newOpponent = Player | move | flipped; // 現プレイヤーの石+置いた石+反転した石
        return new BitBoard(newPlayer, newOpponent);
    }

    // 石を置いたときに反転すべき石のビットマスクを計算
    private ulong GetFlipped(int pos)
    {
        ulong move = 1UL << pos;
        ulong flipped = 0UL;

        flipped |= GetFlippedInDirection(move, Player, Opponent, 8, FullBoard);
        flipped |= GetFlippedInDirection(move, Player, Opponent, unchecked((int)-8), FullBoard);
        flipped |= GetFlippedInDirection(move, Player, Opponent, 1, NotHFile);
        flipped |= GetFlippedInDirection(move, Player, Opponent, unchecked((int)-1), NotAFile);
        flipped |= GetFlippedInDirection(move, Player, Opponent, 9, NotHFile);
        flipped |= GetFlippedInDirection(move, Player, Opponent, 7, NotAFile);
        flipped |= GetFlippedInDirection(move, Player, Opponent, unchecked((int)-7), NotHFile);
        flipped |= GetFlippedInDirection(move, Player, Opponent, unchecked((int)-9), NotAFile);

        return flipped;
    }

    private static ulong GetFlippedInDirection(ulong move, ulong player, ulong opponent, int shift, ulong mask)
    {
        ulong maskedOpponent = opponent & mask;
        ulong candidates;
        if (shift > 0)
            candidates = maskedOpponent & ((move & mask) << shift);
        else
            candidates = maskedOpponent & ((move & mask) >> (-shift));

        for (int i = 0; i < 5; i++)
        {
            if (shift > 0)
                candidates |= maskedOpponent & ((candidates & mask) << shift);
            else
                candidates |= maskedOpponent & ((candidates & mask) >> (-shift));
        }

        // 自分の石で挟まれているか確認
        ulong end;
        if (shift > 0)
            end = player & (candidates << shift);
        else
            end = player & (candidates >> (-shift));

        return end != 0 ? candidates : 0UL;
    }

    /// <summary>
    /// 空きマス数を返す
    /// </summary>
    public int EmptyCount => BitOperations.PopCount(~(Player | Opponent));

    /// <summary>
    /// プレイヤーの石数
    /// </summary>
    public int PlayerCount => BitOperations.PopCount(Player);

    /// <summary>
    /// 相手の石数
    /// </summary>
    public int OpponentCount => BitOperations.PopCount(Opponent);

    /// <summary>
    /// 合法手ビットマスクをリストに展開する
    /// </summary>
    public static List<int> GetMoveList(ulong moves)
    {
        var list = new List<int>(BitOperations.PopCount(moves));
        while (moves != 0)
        {
            int pos = BitOperations.TrailingZeroCount(moves);
            list.Add(pos);
            moves &= moves - 1; // 最下位ビットを消す
        }
        return list;
    }

    /// <summary>
    /// ビット位置を(x,y)座標（1ベース）に変換
    /// </summary>
    public static (int x, int y) BitToCoord(int pos) => (pos / 8 + 1, pos % 8 + 1); // (行+1, 列+1) の順で返す

    /// <summary>
    /// (x,y)座標（1ベース）をビット位置に変換
    /// </summary>
    public static int CoordToBit(int x, int y) => (y - 1) * 8 + (x - 1);
}
