using System;

public class OthelloSystem
{

    /// <summary>
    /// 駒をおき、反転の処理をします。
    /// </summary>
    /// <param name="x">駒を置く座標(x座標)</param>
    /// <param name="y">駒を置く座標(y座標)</param>
    /// <param name="tiles">盤面</param>
    /// <param name="_turnNum">ターン(数字)</param>
    public void PlacePiece(int x, int y, int[,] tiles, int _turnNum)
    {
        x -= 1; y -= 1;//内部座標に修正
        tiles[x, y] = _turnNum;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int i = 0; i < dx.Length; i++)
        {
            var Candidates = new List<(int x, int y)>();

            int nx = x + dx[i];
            int ny = y + dy[i];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (tiles[nx, ny] == (_turnNum == 1 ? 2 : 1)) // 相手の駒
                {
                    Candidates.Add((nx, ny));
                }
                else if (tiles[nx, ny] == (_turnNum == 1 ? 1 : 2)) // 自分の駒
                {
                    if (Candidates.Count > 0)
                    {
                        foreach (var candidate in Candidates)
                        {
                            tiles[candidate.x, candidate.y] = _turnNum;
                        }
                    }
                    break;
                }
                else
                {
                    break;
                }

                nx += dx[i];
                ny += dy[i];
            }
        }
    }

    public (int, int) counting(int[,] tiles)
    {
        int counter1 = 0;
        int counter2 = 0;
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j] == 1)
                {
                    counter1++;
                }
                else if (tiles[i, j] == 2)
                {
                    counter2++;
                }
            }
        }
        return (counter1, counter2);
    }
}