using CommunityToolkit.Mvvm.ComponentModel;

namespace OthelloConsole.ViewModels;

public partial class CellViewModel : ObservableObject
{
    public int Index { get; }

    // OthelloSystem の座標系は 1 ベース
    public int Row => Index / 8 + 1;
    public int Col => Index % 8 + 1;

    [ObservableProperty] private int  _value;        // 0=空, 1=黒, 2=白
    [ObservableProperty] private bool _isValidMove;  // 有効手マーカー
    [ObservableProperty] private bool _isLastMove;   // 直前に置かれた手

    public CellViewModel(int index) => Index = index;
}
