using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OthelloConsole.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ─── ゲーム状態 ────────────────────────────────────────────
    private MainGameData         _gameData;
    private readonly OthelloAI   _ai;
    private List<(int x, int y)> _validMoves          = new();
    private int                   _consecutivePassCount = 0;
    private (int row, int col)    _lastMove            = (-1, -1);

    // ─── バインディングプロパティ ───────────────────────────────
    [ObservableProperty] private int    _blackScore;
    [ObservableProperty] private int    _whiteScore;
    [ObservableProperty] private string _currentTurnText = "ゲーム待機中";
    [ObservableProperty] private string _statusMessage   = "ゲームを設定して「ゲーム開始」を押してください";
    [ObservableProperty] private bool   _isPlayerTurn    = false;
    [ObservableProperty] private bool   _isGameRunning   = false;
    [ObservableProperty] private bool   _isAiThinking    = false;

    // ─── 設定プロパティ ─────────────────────────────────────────
    [ObservableProperty] private GameMode   _selectedGameMode   = GameMode.PlayervsAI;
    [ObservableProperty] private AIStrength _selectedAiStrength = AIStrength.normal;
    [ObservableProperty] private bool       _isPlayerFirst      = true;

    // ─── CanExecute 変更通知 ────────────────────────────────────
    partial void OnIsPlayerTurnChanged(bool value)  => CellClickCommand.NotifyCanExecuteChanged();
    partial void OnIsGameRunningChanged(bool value) => CellClickCommand.NotifyCanExecuteChanged();
    partial void OnIsAiThinkingChanged(bool value)  => CellClickCommand.NotifyCanExecuteChanged();

    // ─── コレクション ───────────────────────────────────────────
    public ObservableCollection<CellViewModel> Cells { get; } =
        new(Enumerable.Range(0, 64).Select(i => new CellViewModel(i)));

    public IReadOnlyList<GameMode>   GameModes   { get; } = Enum.GetValues<GameMode>().ToList();
    public IReadOnlyList<AIStrength> AiStrengths { get; } = Enum.GetValues<AIStrength>().ToList();

    public MainWindowViewModel()
    {
        _ai = new OthelloAI(
            File.Exists("weights/best_weights.json")
                ? WeightSet.Load("weights/best_weights.json")
                : WeightSet.Default());
    }

    // ─── NewGame コマンド ────────────────────────────────────────
    [RelayCommand]
    private async Task NewGame()
    {
        _consecutivePassCount = 0;
        _lastMove = (-1, -1);

        bool aiGoesFirst = SelectedGameMode == GameMode.AIvsAI
                        || (SelectedGameMode == GameMode.PlayervsAI && !IsPlayerFirst);

        _gameData = new MainGameData
        {
            _tilesSize   = 8,
            _gameMode    = SelectedGameMode,
            _AIStrength  = SelectedAiStrength,
            _gameTurn    = aiGoesFirst ? GameTurn.AI    : GameTurn.Player,
            _turnNum     = aiGoesFirst ? 2              : 1,
            _turnCounter = 1,
            _tiles       = new int[8, 8]
        };

        int mid = 4; // 8 / 2
        _gameData._tiles[mid - 1, mid - 1] = 2;
        _gameData._tiles[mid - 1, mid]     = 1;
        _gameData._tiles[mid,     mid - 1] = 1;
        _gameData._tiles[mid,     mid]     = 2;

        IsGameRunning = true;
        SyncBoardToUI();
        SyncScoreToUI();

        await ProcessNextTurnAsync();
    }

    // ─── CellClick コマンド ──────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanClickCell))]
    private async Task CellClick(CellViewModel cell)
    {
        if (!_validMoves.Any(m => m.x == cell.Row && m.y == cell.Col))
            return;

        await PlaceAndAdvanceAsync(cell.Row, cell.Col);
    }

    private bool CanClickCell(CellViewModel? _)
        => IsPlayerTurn && IsGameRunning && !IsAiThinking;

    // ─── 石を置いてターンを進める ─────────────────────────────────
    private async Task PlaceAndAdvanceAsync(int row, int col)
    {
        IsPlayerTurn = false;

        OthelloSystem.PlacePiece(row, col, _gameData);
        _lastMove = (row, col);
        _consecutivePassCount = 0;

        _gameData = OthelloSystem.TurnChange(_gameData);
        _gameData._turnCounter++;

        SyncBoardToUI();
        SyncScoreToUI();

        if (CheckGameOver()) return;

        await ProcessNextTurnAsync();
    }

    // ─── 次ターン制御 ───────────────────────────────────────────
    private async Task ProcessNextTurnAsync()
    {
        _validMoves = OthelloSystem.InstallationArea(_gameData);

        if (_validMoves.Count == 0)
        {
            _consecutivePassCount++;
            string passer = _gameData._turnNum == 1 ? "黒" : "白";
            StatusMessage   = $"{passer} は置ける場所がありません。パスします。";
            CurrentTurnText = $"{passer} パス";

            if (CheckGameOver()) return;

            await Task.Delay(1200);

            _gameData = OthelloSystem.TurnChange(_gameData);
            _gameData._turnCounter++;

            await ProcessNextTurnAsync();
            return;
        }

        _consecutivePassCount = 0;
        SyncValidMovesToUI();

        if (IsHumanTurn())
        {
            IsPlayerTurn = true;
            string player = _gameData._turnNum == 1 ? "黒（あなた）" : "白（あなた）";
            CurrentTurnText = $"{player} の番";
            StatusMessage   = "石を置く場所をクリックしてください";
        }
        else
        {
            await ProcessAiTurnAsync();
        }
    }

    // ─── AI ターン（非同期） ─────────────────────────────────────
    private async Task ProcessAiTurnAsync()
    {
        IsPlayerTurn = false;
        IsAiThinking = true;
        string color    = _gameData._turnNum == 1 ? "黒" : "白";
        CurrentTurnText = $"AI（{color}）が思考中...";
        StatusMessage   = "しばらくお待ちください";

        (int x, int y) aiMove;
        try
        {
            var snapshot = _gameData.Clone();
            var moves    = _validMoves.ToList();
            aiMove = await Task.Run(() => _ai.AI(moves, snapshot, _isDebug: false));
        }
        finally
        {
            IsAiThinking = false;
        }

        CurrentTurnText = $"AI（{color}）が ({aiMove.x}, {aiMove.y}) に置きました";
        StatusMessage   = "";
        await Task.Delay(600);

        await PlaceAndAdvanceAsync(aiMove.x, aiMove.y);
    }

    // ─── ゲーム終了判定 ─────────────────────────────────────────
    private bool CheckGameOver()
    {
        bool boardFull  = !_gameData._tiles.Cast<int>().Any(t => t == 0);
        bool doublePass = _consecutivePassCount >= 2;

        if (!boardFull && !doublePass) return false;

        IsGameRunning = false;
        IsPlayerTurn  = false;

        var (black, white) = OthelloSystem.Counting(_gameData._tiles);
        BlackScore = black;
        WhiteScore = white;

        string result = black > white ? "黒の勝ち！"
                      : white > black ? "白の勝ち！"
                      :                 "引き分け！";
        CurrentTurnText = "ゲーム終了";
        StatusMessage   = $"{result}（黒 {black} vs 白 {white}）";
        return true;
    }

    // ─── 現在ターンが人間か ──────────────────────────────────────
    private bool IsHumanTurn() => _gameData._gameMode switch
    {
        GameMode.PlayervsPlayer => true,
        GameMode.PlayervsAI     => _gameData._gameTurn == GameTurn.Player,
        GameMode.AIvsAI         => false,
        _                       => false
    };

    // ─── UI 同期ヘルパー ─────────────────────────────────────────
    private void SyncBoardToUI()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                var cell = Cells[i * 8 + j];
                cell.Value      = _gameData._tiles[i, j];
                cell.IsLastMove = (i + 1 == _lastMove.row && j + 1 == _lastMove.col);
                cell.IsValidMove = false;
            }
        }
    }

    private void SyncValidMovesToUI()
    {
        foreach (var c in Cells) c.IsValidMove = false;
        // InstallationArea は 1ベース座標 (x=row, y=col) を返す
        foreach (var (x, y) in _validMoves)
            Cells[(x - 1) * 8 + (y - 1)].IsValidMove = true;
    }

    private void SyncScoreToUI()
    {
        var (black, white) = OthelloSystem.Counting(_gameData._tiles);
        BlackScore = black;
        WhiteScore = white;
    }
}
