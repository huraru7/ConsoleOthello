using System.Text.Json;

// TODO: 強化学習機能（将来実装予定）
// - AI同士の対戦による自己学習機能（HillClimbTrainer）
// - 学習結果を weights/best_weights.json に保存・継続学習可能

/// <summary>
/// フェーズ（序盤・中盤）ごとの評価関数の重みセット
/// </summary>
public class PhaseWeights
{
    public int PosWeight    { get; set; }  // 位置評価の重み
    public int MobWeight    { get; set; }  // 機動力の重み
    public int StaWeight    { get; set; }  // 安定石の重み
    public int DiffWeight   { get; set; }  // 駒数差の重み
    public int ParityWeight   { get; set; }  // パリティの重み（奇数空きマス＝現プレイヤー有利）
    public int FrontierWeight { get; set; }  // フロンティアの重み（空きマスに隣接する不安定石を減らす）
    public int PotMobWeight   { get; set; }  // ポテンシャル機動力の重み（将来の合法手数の推定）
}

/// <summary>
/// 1つのAI評価パラメータのセット。JSONで保存・読み込みできる。
/// </summary>
public class WeightSet
{
    public PhaseWeights Early { get; set; } = new();  // 序盤 (総駒数 < 20)
    public PhaseWeights Mid   { get; set; } = new();  // 中盤 (20 <= 総駒数 < 54)
    /// <summary>8x8=64要素の位置評価テーブル。インデックスは row*8+col。</summary>
    public int[] PositionTable { get; set; } = new int[64];

    /// <summary>現在のハードコード値と同じデフォルト重みを返す</summary>
    public static WeightSet Default() => new WeightSet
    {
        Early = new PhaseWeights { PosWeight = 30, MobWeight = 15, StaWeight = 20, DiffWeight = 0, ParityWeight = 0, FrontierWeight = 3, PotMobWeight = 0 },
        Mid   = new PhaseWeights { PosWeight = 25, MobWeight = 10, StaWeight = 15, DiffWeight = 5, ParityWeight = 5, FrontierWeight = 5, PotMobWeight = 0 },
        PositionTable = new int[]
        {
             500, -150,  30,  10,  10,  30, -150,  500,
            -150, -250,  -5,  -5,  -5,  -5, -250, -150,
              30,   -5,  15,   3,   3,  15,   -5,   30,
              10,   -5,   3,   3,   3,   3,   -5,   10,
              10,   -5,   3,   3,   3,   3,   -5,   10,
              30,   -5,  15,   3,   3,  15,   -5,   30,
            -150, -250,  -5,  -5,  -5,  -5, -250, -150,
             500, -150,  30,  10,  10,  30, -150,  500,
        }
    };

    /// <summary>このWeightSetをJSONファイルに保存する</summary>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(this, options));
    }

    /// <summary>JSONファイルからWeightSetを読み込む。失敗時はDefaultを返す</summary>
    public static WeightSet Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<WeightSet>(File.ReadAllText(path)) ?? Default();
        }
        catch
        {
            return Default();
        }
    }

    /// <summary>ディープコピーを返す（摂動用）</summary>
    public WeightSet Clone() => new WeightSet
    {
        Early = new PhaseWeights
        {
            PosWeight      = Early.PosWeight,
            MobWeight      = Early.MobWeight,
            StaWeight      = Early.StaWeight,
            DiffWeight     = Early.DiffWeight,
            ParityWeight   = Early.ParityWeight,
            FrontierWeight = Early.FrontierWeight,
            PotMobWeight   = Early.PotMobWeight,
        },
        Mid = new PhaseWeights
        {
            PosWeight      = Mid.PosWeight,
            MobWeight      = Mid.MobWeight,
            StaWeight      = Mid.StaWeight,
            DiffWeight     = Mid.DiffWeight,
            ParityWeight   = Mid.ParityWeight,
            FrontierWeight = Mid.FrontierWeight,
            PotMobWeight   = Mid.PotMobWeight,
        },
        PositionTable = (int[])PositionTable.Clone()
    };
}

/// <summary>プレイヤー対AIの累積戦績。JSONで保存・読み込みできる。</summary>
public class BattleRecord
{
    public int Wins   { get; set; }
    public int Losses { get; set; }
    public int Draws  { get; set; }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(this, options));
    }

    public static BattleRecord Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<BattleRecord>(File.ReadAllText(path)) ?? new BattleRecord();
        }
        catch
        {
            return new BattleRecord();
        }
    }
}

public static class LibraryGameData
{
}
