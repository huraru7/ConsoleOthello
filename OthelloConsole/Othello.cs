using System.Text;
using static OthelloSystem;
using static OthelloDebugLog;

public class Othello
{
    void DataReset()
    {
        // 盤面サイズが設定されていない(0)場合は既定値8を使用
        int size = _gameData._tilesSize > 0 ? _gameData._tilesSize : 8;

        // 盤面サイズは偶数である必要がある
        if (size % 2 != 0)
        {
            Console.WriteLine($"Error: 盤面サイズ(size={size})は偶数である必要があります。");
            throw new System.ArgumentException("盤面サイズは偶数にしてください。");
        }

        _gameData._tiles = new int[size, size];

        // 全て0で初期化 (int配列は既に0で初期化されるが明示的に残す)
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                _gameData._tiles[i, j] = 0;
            }
        }

        // 中央の初期駒を設定
        int mid = size / 2; // 例: size=8 -> mid=4, 中央は indices 3,4
        _gameData._tiles[mid - 1, mid - 1] = 2;
        _gameData._tiles[mid - 1, mid] = 1;
        _gameData._tiles[mid, mid - 1] = 1;
        _gameData._tiles[mid, mid] = 2;

        _gameData._gameTurn = GameTurn.prayer;
        _gameData._turnConter = 1; //ターン数
        _gameData._turnNum = 1; // 1:prayer1  2:prayer2 or AI
        _gameData._tilesSize = size;
    }

    private StringBuilder builder = new();
    private MainGameData _gameData = new MainGameData();
    public OthelloAI _ai = new OthelloAI();
    private int _player1FrameCounter = 2;
    private int _player2FrameCounter = 2;
    private bool _isDebug = false;
    private List<(int x, int y)> _validMoves = new List<(int x, int y)>();
    private int _consecutivePassCount = 0; // 連続パス回数のカウント

    /// <summary>
    /// スタート処理
    /// </summary>
    public static async Task Main()
    {
        Othello _game = new Othello();
        _game._gameData._tilesSize = 8;
        _game.DataReset();
        StartGameLog();
        Console.WriteLine("オセロへようこそ！");
        Console.WriteLine("モード選択:AI対戦(1) 2人対戦(2) AI対AI(3)");
        string input = Console.ReadLine() ?? "";

        switch (input)
        {
            case "1":
                Console.WriteLine("AI対戦モードを選択しました。");
                _game._gameData._gameMode = GameMode.PlayervsAI;
                break;
            case "2":
                Console.WriteLine("2人対戦モードを選択しました。");
                _game._gameData._gameMode = GameMode.PlayervsPlayer;
                break;
            case "3":
                Console.WriteLine("AI対AIモードを選択しました。");
                _game._gameData._gameMode = GameMode.AIvsAI;
                _game._gameData._gameTurn = GameTurn.AI;
                _game._gameData._turnNum = 2;
                break;
            default:
                Console.WriteLine("無効な入力です。2人対戦モードに進みます。");
                _game._gameData._gameMode = GameMode.PlayervsPlayer;
                break;
        }
        if (_game._gameData._gameMode == GameMode.PlayervsAI)
        {
            Console.WriteLine("先行後攻を選択してください:先行(1) 後攻(2)");
            string sizeInput = Console.ReadLine() ?? "";
            switch (sizeInput)
            {
                case "1":
                    Console.WriteLine("あなたは先行です。");
                    break;
                case "2":
                    Console.WriteLine("あなたは後攻です。");
                    _game._gameData._gameTurn = GameTurn.AI;
                    _game._gameData._turnNum = 2;
                    break;
                default:
                    Console.WriteLine("無効な入力です。先行に進みます。");
                    break;
            }
        }
        if (_game._gameData._gameMode == GameMode.PlayervsAI || _game._gameData._gameMode == GameMode.AIvsAI)
        {
            Console.WriteLine("AIの強さを選択してください:初心者(1) 普通(2) 上級者(3) プロ(4)");
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
                    _game._gameData._AIStrength = AIStrength.expert;
                    break;
                case "4":
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
        if (command == "debug")
        {
            _game._isDebug = true; // デバッグモードon
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
        GameLog($"ゲーム開始 モード;{_gameData._gameMode} 盤面サイズ:{_gameData._tilesSize} AI強さ:{_gameData._AIStrength}");
        GameLog("=======================================================");
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
                _gameData = TurnChange(_gameData);
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
                (int x, int y) = _ai.AI(_validMoves, _gameData, _isDebug);
                Console.WriteLine($"AIの手{x},{y}を選択しました。");
                input = $"{x}{y}";

                await Task.Delay(3000); // 3秒待機
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
                GameLog("ゲームが中断されました");
                StopGameLog();
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

            GameLog($"{row}, {col}に駒を設置");
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
            (_player1FrameCounter, _player2FrameCounter) = Counting(_gameData._tiles);
            _consecutivePassCount = 0; // パスをリセット

            _gameData = TurnChange(_gameData);

            _gameData._turnConter++;
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
            StopGameLog();
            Console.WriteLine("=======================================================");
            if (_consecutivePassCount >= 2)
            {
                Console.WriteLine("ゲーム終了：両者が連続してパスしました。");
            }
            else
            {
                Console.WriteLine("ゲーム終了：盤面に空きがありません。");
            }
            Console.WriteLine($"最終結果: 敵(黒) {_player2FrameCounter}個 - プレイヤー(白) {_player1FrameCounter}個");
            if (_player2FrameCounter > _player1FrameCounter)
                Console.WriteLine("敵(黒)の勝ち！");
            else if (_player1FrameCounter > _player2FrameCounter)
                Console.WriteLine("プレイヤー(白)の勝ち！");
            else
                Console.WriteLine("引き分けです！");
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

        if (_gameData._turnNum == 1)
        {
            builder.Append("現在のターン: ●\n");
        }
        else
        {
            builder.Append("現在のターン: ○\n");
        }
        builder.Append($"プレイヤー: {_player1FrameCounter}個    敵: {_player2FrameCounter}個\n");

        builder.Append("   ");
        for (int i = 0; i < _gameData._tilesSize; i++)
        {
            builder.Append($"{i + 1} ");
        }
        builder.Append("\n  ┏");
        for (int i = 0; i < _gameData._tilesSize - 1; i++)
        {
            builder.Append("==");
        }
        builder.Append("=\n");

        for (int i = 0; i < _gameData._tilesSize; i++)
        {
            builder.Append($"{i + 1} |");
            for (int j = 0; j < _gameData._tilesSize; j++)
            {
                builder.Append(_gameData._tiles[i, j] switch { 0 => "- ", 1 => "● ", 2 => "○ ", 3 => "# ", _ => "? " });
            }
            builder.AppendLine();
        }

        // マークした「3」を0に戻す
        foreach (var move in _validMoves)
        {
            _gameData._tiles[move.x - 1, move.y - 1] = 0;
        }

        GameLog(builder.ToString());
        return builder.ToString();
    }
}