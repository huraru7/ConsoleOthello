using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class Othello
{
    public int[,] tiles = new int[,]
    {
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,2,1,0,0,0},
        {0,0,0,1,2,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
    };

    private StringBuilder builder = new();
    private int _turnConter = 0;
    private int _turn = 1; // 1:プレイヤー  2:敵
    private int _playerFrameConter = 2;
    private int _enemyFrameConter = 2;
    private List<(int x, int y)> _validMoves = new List<(int x, int y)>();
    public GameMode _gameMode;
    public AIStrength _AIStrength = AIStrength.normal;
    public enum GameMode
    {
        AIvsPlayer,
        PlayervsPlayer
    }

    /// <summary>
    /// スタート処理
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("オセロへようこそ！");
        Console.WriteLine("モード選択:AI対戦(1) 2人対戦(2)");
        string input = Console.ReadLine() ?? "";
        Othello game = new Othello();
        switch (input)
        {
            case "1":
                Console.WriteLine("AI対戦モードは現在未実装のため。2人対戦モードに進みます。");
                game._gameMode = GameMode.AIvsPlayer;
                break;
            case "2":
                Console.WriteLine("2人対戦モードを選択しました。");
                game._gameMode = GameMode.PlayervsPlayer;
                break;
            default:
                Console.WriteLine("無効な入力です。2人対戦モードに進みます。");
                game._gameMode = GameMode.PlayervsPlayer;
                break;
        }

        Console.WriteLine("Enterキーを押してゲームを開始...");
        Console.ReadLine();
        Console.Clear();
        await game.RunGame();
    }

    /// <summary>
    /// ゲームを実行する中枢
    /// </summary>
    async Task RunGame()
    {
        while (true)
        {
            _validMoves = InstallationArea();
            if (GameChecker()) return;

            Console.WriteLine(ConsoleWriteBoard());
            Console.Write("置ける場所:");
            if (_validMoves.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("置ける場所がありません。ターンをスキップします。");
                Console.WriteLine("=======================================================");
                _turn = _turn == 1 ? 2 : 1;
                _turnConter++;
                continue;
            }
            else
            {
                foreach (var move in _validMoves)
                {
                    Console.Write($"| {move.x}{move.y} ");
                }
                Console.WriteLine();
            }

            string input;
            if (_gameMode == GameMode.AIvsPlayer && _turn == 2)
            {
                Console.WriteLine("=======================================================");
                OthelloAI ai = new OthelloAI();
                (int x, int y) = ai.AI(_validMoves, tiles, 1, _AIStrength);
                input = $"{x}{y}";

                await Task.Delay(300); // 1秒待機
            }
            else
            {
                Console.WriteLine("置く場所を選んでください (例: 34 は 縦3番 横4番に置く)");
                input = Console.ReadLine() ?? "";
            }

            if (input == "end")
            {
                Console.Clear();
                Console.WriteLine("ゲームを終了しました");
                return;
            }


            if (!TryParseMove(input, out int row, out int col) || !_validMoves.Any(m => m.x == row && m.y == col))
            {
                Console.Clear();
                Console.WriteLine("無効な入力です。縦列、横列の順番で正しく入力してください。例:34");
                Console.WriteLine("=======================================================");
                continue;
            }

            Console.Clear();

            if (_gameMode == GameMode.AIvsPlayer && _turn == 2)
            {
                Console.WriteLine($"AIの選択した手:{row}, {col}");
            }
            else
            {
                Console.WriteLine($"入力を解析しました: {row}, {col}");
            }
            Console.WriteLine("=======================================================");
            PlacePiece(row, col);

            _turn = _turn == 1 ? 2 : 1;//turn変更
            _turnConter++;
        }
    }

    /// <summary>
    /// 駒の処理を行います。設置、反転、
    /// </summary>
    void PlacePiece(int x, int y)
    {
        x -= 1;
        y -= 1;
        tiles[x, y] = _turn;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int direction = 0; direction < dx.Length; direction++)
        {
            List<(int x, int y)> Candidates = new List<(int x, int y)>();

            int nx = x + dx[direction];
            int ny = y + dy[direction];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (tiles[nx, ny] == (_turn == 1 ? 2 : 1)) // 相手の駒
                {
                    Candidates.Add((nx, ny));
                }
                else if (tiles[nx, ny] == (_turn == 1 ? 1 : 2)) // 自分の駒
                {
                    if (Candidates.Count > 0)
                    {
                        foreach (var candidate in Candidates)
                        {
                            tiles[candidate.x, candidate.y] = _turn;
                        }
                    }
                    break;
                }
                else
                {
                    break;
                }

                nx += dx[direction];
                ny += dy[direction];
            }
        }

        _playerFrameConter = 0;
        _enemyFrameConter = 0;
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j] == 1)
                {
                    _playerFrameConter++;
                }
                else if (tiles[i, j] == 2)
                {
                    _enemyFrameConter++;
                }
            }
        }
    }

    /// <summary>
    /// ゲーム終了の判定をします
    /// </summary>
    bool GameChecker()
    {
        bool hasEmpty = tiles.Cast<int>().Any(t => t == 0);

        if (!hasEmpty)
        {
            ConsoleWriteBoard();
            Console.WriteLine("=======================================================");
            Console.WriteLine("ゲーム終了：盤面に空きがありません。");
            if (_enemyFrameConter > _playerFrameConter)
                Console.WriteLine("敵(黒)の勝ち！");
            else if (_playerFrameConter > _enemyFrameConter)
                Console.WriteLine("プレイヤー(白)の勝ち！");
            else
                Console.WriteLine("引き分け");
            Console.WriteLine("=======================================================");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 設置可能エリアを出します
    /// </summary>
    List<(int x, int y)> InstallationArea()
    {
        var moves = new List<(int x, int y)>();

        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                // 既に駒が置いてある場所は候補にしない
                if (tiles[i, j] != 0) continue;

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

                            if (tiles[x, y] == (_turn == 1 ? 2 : 1)) // 相手の駒
                            {
                                hasOpponentPiece = true;
                            }
                            else if (tiles[x, y] == (_turn == 1 ? 1 : 2)) // 自分の駒
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
                    if (tiles[i, j] != 0) continue;
                    moves.Add((i + 1, j + 1)); // 1ベースで保存
                }
            }
        }

        return moves;
    }

    /// <summary>
    /// ユーザー入力文字列を (行, 列) の2つの int に変換します
    /// 戻り値: 成功true(outに値を返します) 失敗ならfalse
    /// </summary>
    bool TryParseMove(string input, out int row, out int col)
    {
        row = 0;
        col = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;// 空入力は無効

        input = input.Trim();//空白を消去

        if (input.Length == 2 && char.IsDigit(input[0]) && char.IsDigit(input[1]))
        {
            row = input[0] - '0';
            col = input[1] - '0';
            return true;
        }

        var parts = System.Text.RegularExpressions.Regex.Split(input, "@\\D+")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        if (parts.Length >= 2)
        {
            if (int.TryParse(parts[0], out row) && int.TryParse(parts[1], out col))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// コンソールにボードを描画します
    /// </summary>
    string ConsoleWriteBoard()
    {
        builder.Clear();
        builder.Append($"ターン数: {_turnConter}  ");
        if (_turn == 1)
            builder.Append("現在のターン: ●\n");
        else
            builder.Append("現在のターン: ○\n");
        builder.Append($"プレイヤー: {_playerFrameConter}個    敵: {_enemyFrameConter}個\n");
        builder.Append("   1 2 3 4 5 6 7 8\n  ┏===============\n");

        for (int i = 0; i < 8; i++)
        {
            builder.Append($"{i + 1} |");
            for (int j = 0; j < 8; j++)
            {
                builder.Append(tiles[i, j] switch
                {
                    0 => "- ",
                    1 => "● ",
                    2 => "○ ",
                    _ => "? "
                });
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }
}