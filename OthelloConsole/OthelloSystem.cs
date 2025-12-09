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
    public void PlacePiece(int x, int y, MainGameData _gameData)
    {
        x -= 1; y -= 1;//内部座標に修正
        _gameData._tiles[x, y] = _gameData._turnNum;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int i = 0; i < dx.Length; i++)
        {
            var Candidates = new List<(int x, int y)>();

            int nx = x + dx[i];
            int ny = y + dy[i];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (_gameData._tiles[nx, ny] == (_gameData._turnNum == 1 ? 2 : 1)) // 相手の駒
                {
                    Candidates.Add((nx, ny));
                }
                else if (_gameData._tiles[nx, ny] == (_gameData._turnNum == 1 ? 1 : 2)) // 自分の駒
                {
                    if (Candidates.Count > 0)
                    {
                        foreach (var candidate in Candidates)
                        {
                            _gameData._tiles[candidate.x, candidate.y] = _gameData._turnNum;
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

    /// <summary>
    /// 設置可能な場所を出します
    /// </summary>
    /// <returns>おける場所の座標リスト</returns>
    public List<(int x, int y)> InstallationArea(MainGameData _gameData)
    {
        var moves = new List<(int x, int y)>();

        for (int i = 0; i < _gameData._tiles.GetLength(0); i++)
        {
            for (int j = 0; j < _gameData._tiles.GetLength(1); j++)
            {
                // 既に駒が置いてある場所は候補にしない
                if (_gameData._tiles[i, j] != 0) continue;

                bool canPlace = false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // 自分は飛ばす

                        int x = i + dx;
                        int y = j + dy;
                        bool hasOpponentPiece = false;

                        while (x >= 0 && x < 8 && y >= 0 && y < 8)
                        {

                            if (_gameData._tiles[x, y] == (_gameData._turnNum == 1 ? 2 : 1)) // 相手の駒
                            {
                                hasOpponentPiece = true;
                            }
                            else if (_gameData._tiles[x, y] == (_gameData._turnNum == 1 ? 1 : 2)) // 自分の駒
                            {
                                if (hasOpponentPiece)
                                {
                                    canPlace = true;
                                }
                                break;
                            }
                            else // 空きマス
                            {
                                break;
                            }
                            x += dx;
                            y += dy;
                        }
                    }
                }
                if (canPlace)
                {
                    if (_gameData._tiles[i, j] != 0) continue;
                    moves.Add((i + 1, j + 1)); // 1ベースで保存
                }
            }
        }
        return moves;
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