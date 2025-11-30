using System;

public class OthelloSystem
{

    void PlacePiece(int x, int y, int[,] tiles, int _turnNum)
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
}