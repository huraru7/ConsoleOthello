using System;
using System.Text;

public class Othello : OthelloSystem
{
    void DataReset()
    {
        _gameData._tiles = new int[,]
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
        _gameData._gameTurn = GameTurn.prayer1;
        _gameData._turnConter = 1; //ターン数
        _gameData._turnNum = 1; // 1:prayer1  2:prayer2 or AI
    }

    private StringBuilder builder = new();
    private MainGameData _gameData = new MainGameData();
    public OthelloAI _ai = new OthelloAI();
    private int _player1FrameConter = 2;
    private int _player2FrameConter = 2;
    private List<(int x, int y)> _validMoves = new List<(int x, int y)>();
    private int _consecutivePassCount = 0; // 連続パス回数のカウント

    /// <summary>
    /// スタート処理
    /// </summary>
    public static async Task Main()
    {
        Othello _game = new Othello();
        _game.DataReset();
        Console.WriteLine("オセロへようこそ！");
        Console.WriteLine("モード選択:AI対戦(1) 2人対戦(2)");
        string input = Console.ReadLine() ?? "";

        switch (input)
        {
            case "1":
                Console.WriteLine("AI対戦モードを選択しました。");
                _game._gameData._gameMode = GameMode.AIvsPlayer;
                break;
            case "2":
                Console.WriteLine("2人対戦モードを選択しました。");
                _game._gameData._gameMode = GameMode.PlayervsPlayer;
                break;
            default:
                Console.WriteLine("無効な入力です。2人対戦モードに進みます。");
                _game._gameData._gameMode = GameMode.PlayervsPlayer;
                break;
        }
        if (_game._gameData._gameMode == GameMode.AIvsPlayer)
        {
            Console.WriteLine("AIの強さを選択してください:初心者(1) 普通(2) 上級者(3) プロ(未実装)");
            string aiInput = Console.ReadLine() ?? "";
            switch (aiInput)
            {
                case "1":
                    _game._gameData._AIStrength = AIStrength.nuub;
                    break;
                case "2":
                    _game._gameData._AIStrength = AIStrength.normal;
                    break;
                case "3":
                    _game._gameData._AIStrength = AIStrength.professional;
                    break;
                default:
                    Console.WriteLine("無効な入力です。普通モードに進みます。");
                    _game._gameData._AIStrength = AIStrength.normal;
                    break;
            }
        }

        Console.WriteLine("Enterキーを押してゲームを開始...");
        string command = Console.ReadLine() ?? "";
        if (command == "huraru")
        {
            _game._ai._isDebug = true; // デバッグモードon
            Console.WriteLine("デバッグモードを有効化しました。");
            Console.ReadLine();
        }

        Console.Clear();
        await _game.RunGame();
    }

    /// <summary>
    /// ゲームを実行する中枢
    /// </summary>
    async Task RunGame()
    {
        while (true)
        {
            _validMoves = InstallationArea(_gameData);
            if (GameChecker()) return;

            Console.WriteLine(ConsoleWriteBoard());
            Console.Write("置ける場所:");
            if (_validMoves.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("置ける場所がありません。ターンをスキップします。");
                Console.WriteLine("=======================================================");
                _consecutivePassCount++;
                TurnChange();
                _gameData._turnConter++;
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
            if (_gameData._gameTurn == GameTurn.AI)
            {
                Console.WriteLine("=======================================================");
                (int x, int y) = _ai.AI(_validMoves, _gameData);
                Console.WriteLine($"AIの手{x},{y}を選択しました。");
                input = $"{x}{y}";

                if (_ai._isDebug) Console.ReadLine();
                await Task.Delay(3500); // 3.5秒待機
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

            if (_gameData._gameTurn == GameTurn.AI)
            {
                Console.WriteLine($"AIの選択した手:{row}, {col}");
            }
            else
            {
                Console.WriteLine($"入力を解析しました: {row}, {col}");
            }
            Console.WriteLine("=======================================================");
            PlacePiece(row, col, _gameData);
            _consecutivePassCount = 0; // パスをリセット

            TurnChange();
            _gameData._turnConter++;
        }
    }

    /// <summary>
    /// ターンを変更します
    /// </summary>
    void TurnChange()
    {
        switch (_gameData._gameTurn)
        {
            case GameTurn.prayer1:
                switch (_gameData._gameMode)
                {
                    case GameMode.AIvsPlayer:
                        _gameData._gameTurn = GameTurn.AI;
                        break;
                    case GameMode.PlayervsPlayer:
                        _gameData._gameTurn = GameTurn.prayer2;
                        break;
                }
                _gameData._turnNum = 2;
                break;
            case GameTurn.prayer2 or GameTurn.AI:
                _gameData._gameTurn = GameTurn.prayer1;
                _gameData._turnNum = 1;
                break;
        }
    }

    /// <summary>
    /// ゲーム終了の判定をします
    /// </summary>
    bool GameChecker()
    {
        bool hasEmpty;
        hasEmpty = _gameData._tiles.Cast<int>().Any(t => t == 0);
        if (_consecutivePassCount >= 2) hasEmpty = false;

        if (!hasEmpty)
        {
            Console.WriteLine(ConsoleWriteBoard());
            Console.WriteLine("=======================================================");
            if (_consecutivePassCount >= 2)
            {
                Console.WriteLine("ゲーム終了：両者が連続してパスしました。");
            }
            else
            {
                Console.WriteLine("ゲーム終了：盤面に空きがありません。");
            }
            Console.WriteLine($"最終結果: 敵(黒) {_player2FrameConter}個 - プレイヤー(白) {_player1FrameConter}個");
            if (_player2FrameConter > _player1FrameConter)
                Console.WriteLine("敵(黒)の勝ち！");
            else if (_player1FrameConter > _player2FrameConter)
                Console.WriteLine("プレイヤー(白)の勝ち！");
            else
                Console.WriteLine("引き分け");
            Console.WriteLine("=======================================================");
            return true;
        }
        return false;
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
        // 設置可能な場所を一時的に「3」にマーク
        foreach (var move in _validMoves)
        {
            _gameData._tiles[move.x - 1, move.y - 1] = 3;
        }

        builder.Clear();
        builder.Append($"ターン数: {_gameData._turnConter}  ");
        if (_gameData._gameTurn == GameTurn.prayer1)
            builder.Append("現在のターン: ●\n");
        else
            builder.Append("現在のターン: ○\n");
        builder.Append($"プレイヤー: {_player1FrameConter}個    敵: {_player2FrameConter}個\n");
        builder.Append("   1 2 3 4 5 6 7 8\n  ┏===============\n");

        for (int i = 0; i < 8; i++)
        {
            builder.Append($"{i + 1} |");
            for (int j = 0; j < 8; j++)
            {
                builder.Append(_gameData._tiles[i, j] switch
                {
                    0 => "- ",
                    1 => "● ",
                    2 => "○ ",
                    3 => "# ",
                    _ => "? "
                });
            }
            builder.AppendLine();
        }

        // マークした「3」を0に戻す
        foreach (var move in _validMoves)
        {
            _gameData._tiles[move.x - 1, move.y - 1] = 0;
        }

        return builder.ToString();
    }
}