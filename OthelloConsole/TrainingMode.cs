using System.Numerics;
using static OthelloSystem;

/// <summary>
/// Console出力なしで2つのAIを対局させ、最終的な駒数を返す。
/// 強化学習の評価用に使用する。
/// </summary>
public static class GameRunner
{
    private static readonly Random _rng = new();

    /// <summary>
    /// w1（黒・turnNum=1）と w2（白・turnNum=2）で1ゲームを自動実行する。
    /// 手番は内部で自動的に交代する。
    /// openingMoves: 序盤の何手をランダムに打つか（0=完全決定論的）
    /// </summary>
    /// <returns>(黒の最終駒数, 白の最終駒数)</returns>
    public static (int black, int white) Play(WeightSet w1, WeightSet w2, AIStrength strength, int openingMoves = 4)
    {
        var gameData = InitGameData(strength);
        var ai1 = new OthelloAI(w1);
        var ai2 = new OthelloAI(w2);

        OthelloDebugLog.SuppressOutput = true;
        try
        {
            int passCount = 0;
            const int MaxTurns = 200; // オセロの最大手数は60手程度。安全上限として200を設定
            while (gameData._turnCounter <= MaxTurns)
            {
                var validMoves = InstallationArea(gameData);

                // 終了判定：盤面が埋まった or 両者連続パス
                bool boardFull = !gameData._tiles.Cast<int>().Any(t => t == 0);
                if (boardFull || passCount >= 2) break;

                if (validMoves.Count == 0)
                {
                    passCount++;
                    gameData = TurnChange(gameData);
                    gameData._turnCounter++;
                    continue;
                }
                passCount = 0;

                // 序盤のランダム手：決定論的ゲームによる勝率0/50/100%固定を防ぐ
                int row, col;
                if (gameData._turnCounter <= openingMoves)
                {
                    var pick = validMoves[_rng.Next(validMoves.Count)];
                    (row, col) = pick;
                }
                else
                {
                    OthelloAI currentAI = gameData._turnNum == 1 ? ai1 : ai2;
                    (row, col) = currentAI.AI(validMoves, gameData, false);
                }
                PlacePiece(row, col, gameData);
                gameData = TurnChange(gameData);
                gameData._turnCounter++;
            }
        }
        finally
        {
            OthelloDebugLog.SuppressOutput = false;
        }

        var (p1, p2) = Counting(gameData._tiles);
        return (p1, p2); // (黒=player1, 白=player2)
    }

    /// <summary>ゲームデータを初期状態（8x8盤面、黒先手）に初期化して返す</summary>
    private static MainGameData InitGameData(AIStrength strength)
    {
        var data = new MainGameData();
        data._tilesSize = 8;
        data._tiles = new int[8, 8];
        data._tiles[3, 3] = 2; data._tiles[3, 4] = 1;
        data._tiles[4, 3] = 1; data._tiles[4, 4] = 2;
        data._gameTurn    = GameTurn.AI;     // AIvsAI なので常にAI
        data._gameMode    = GameMode.AIvsAI;
        data._turnNum     = 1;               // 黒が先手
        data._turnCounter = 1;
        data._AIStrength  = strength;
        return data;
    }
}

/// <summary>
/// ランダムヒルクライミング法による評価重みの自動最適化。
/// 候補重みをランダムに摂動させ、AI同士の対戦結果で採否を決定する。
/// </summary>
public static class HillClimbTrainer
{
    private static readonly Random _rng = new();
    private const string WeightsDir = "weights";

    /// <summary>
    /// ヒルクライミング学習を実行する。
    /// </summary>
    /// <param name="maxIter">最大イテレーション数</param>
    /// <param name="gamesPerEval">1イテレーションあたりの対局数（多いほど安定するが遅い）</param>
    /// <param name="strength">使用するAI強度（normalが速度と精度のバランスが良い）</param>
    public static void Run(int maxIter, int gamesPerEval, AIStrength strength)
    {
        Directory.CreateDirectory(WeightsDir);
        string savePath = Path.Combine(WeightsDir, "best_weights.json");

        WeightSet current = File.Exists(savePath)
            ? WeightSet.Load(savePath)
            : WeightSet.Default();

        if (File.Exists(savePath))
            Console.WriteLine($"既存の重みを読み込みました: {Path.GetFullPath(savePath)}");
        else
            Console.WriteLine("デフォルト重みから学習を開始します。");

        Console.WriteLine($"設定: {maxIter}イテレーション / {gamesPerEval}ゲーム/評価 / AI強度:{strength}");
        Console.WriteLine("Ctrl+C で中断できます（中断前の最善重みは自動保存済み）\n");

        int totalAdopted = 0;
        var startTime = DateTime.Now;

        try
        {
            for (int iter = 1; iter <= maxIter; iter++)
            {
                WeightSet previous  = current;
                WeightSet candidate = Perturb(current);

                int winsCandidate = 0, winsCurrent = 0, draws = 0;
                int totalMargin = 0; // 候補の駒数差マージン合計（正=有利、負=不利）
                for (int g = 0; g < gamesPerEval; g++)
                {
                    // 手番を交互に入れ替えて公平な評価（先手有利・後手有利を相殺）
                    bool candidateIsBlack = (g % 2 == 0);
                    (int b, int w) = candidateIsBlack
                        ? GameRunner.Play(candidate, current, strength)
                        : GameRunner.Play(current, candidate, strength);

                    int candidateScore = candidateIsBlack ? b : w;
                    int currentScore   = candidateIsBlack ? w : b;

                    totalMargin += candidateScore - currentScore;

                    if      (candidateScore > currentScore) winsCandidate++;
                    else if (candidateScore < currentScore) winsCurrent++;
                    else                                    draws++;
                }

                // 採用基準: 平均駒数差マージンが正（勝敗だけでなく石差の大きさも考慮）
                double avgMargin = (double)totalMargin / gamesPerEval;
                bool adopted = avgMargin > 0;
                if (adopted)
                {
                    current = candidate;
                    current.Save(savePath);
                    totalAdopted++;
                }

                string mark    = adopted ? "✓ 採用" : "  棄却";
                var    elapsed = DateTime.Now - startTime;
                string elapsedStr = elapsed.ToString(@"hh\:mm\:ss");

                // 残り時間を推定
                var avgPerIter = elapsed / iter;
                var remaining  = avgPerIter * (maxIter - iter);
                string etaStr  = iter < 3 ? "--:--:--" : remaining.ToString(@"hh\:mm\:ss");

                // 進捗バー（20文字幅）
                int filled = (int)(iter * 20.0 / maxIter);
                string bar = "[" + new string('#', filled) + new string('.', 20 - filled) + "]";

                // 変更内容の差分（previous = 採用前の重み、candidate = 試した重み）
                string diff = DescribeDiff(previous, candidate);

                Console.WriteLine(
                    $"{bar} {iter * 100.0 / maxIter,5:F1}%  [{iter,5}/{maxIter}] {mark}  " +
                    $"平均差={avgMargin:+0.0;-0.0}石  (勝:{winsCandidate,2} 負:{winsCurrent,2} 引:{draws,2})  " +
                    $"採用率={totalAdopted * 100.0 / iter:F1}%  経過:{elapsedStr}  残り:{etaStr}  変更:{diff}");

                // 100イテレーションごとにサマリーを表示
                if (iter % 100 == 0)
                {
                    Console.WriteLine($"\n  ── {iter}イテレーション完了 ──  採用回数:{totalAdopted}  " +
                                      $"平均:{avgPerIter.TotalSeconds:F2}秒/iter\n");
                }
            }

            Console.WriteLine($"\n学習完了。採用率={totalAdopted * 100.0 / maxIter:F1}%");
            Console.WriteLine($"最善重みを保存しました: {Path.GetFullPath(savePath)}");
            PrintWeightSummary(current);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[エラー] 学習中に例外が発生しました: {ex.Message}");
            Console.WriteLine($"  種類: {ex.GetType().Name}");
            Console.WriteLine($"最後に採用された重みは {Path.GetFullPath(savePath)} に保存済みです。");
        }
    }

    /// <summary>重みをランダムに微小摂動させたコピーを返す</summary>
    private static WeightSet Perturb(WeightSet src)
    {
        var next = src.Clone();
        if (_rng.NextDouble() < 0.4)  // 40%: フェーズ重みを変更
            PerturbPhaseWeights(next);
        else                          // 60%: 位置評価テーブルを変更
            PerturbPositionTable(next);
        return next;
    }

    /// <summary>序盤・中盤のいずれか1つのフェーズ重みをランダムに変化させる</summary>
    private static void PerturbPhaseWeights(WeightSet ws)
    {
        int delta = _rng.Next(2, 8) * (_rng.NextDouble() < 0.5 ? 1 : -1);
        PhaseWeights phase = _rng.NextDouble() < 0.5 ? ws.Early : ws.Mid;
        switch (_rng.Next(7))
        {
            case 0: phase.PosWeight      = Math.Max(0, phase.PosWeight      + delta); break;
            case 1: phase.MobWeight      = Math.Max(0, phase.MobWeight      + delta); break;
            case 2: phase.StaWeight      = Math.Max(0, phase.StaWeight      + delta); break;
            case 3: phase.DiffWeight     = Math.Max(0, phase.DiffWeight     + delta); break;
            case 4: phase.ParityWeight   = Math.Max(0, phase.ParityWeight   + delta); break;
            case 5: phase.FrontierWeight = Math.Max(0, phase.FrontierWeight + delta); break;
            case 6: phase.PotMobWeight   = Math.Max(0, phase.PotMobWeight   + delta); break;
        }
    }

    /// <summary>
    /// 4回転対称を維持しながら位置評価テーブルの1グループを変化させる。
    /// 同じ「対称グループ」の4マスを同時に変更することで、
    /// 盤面の回転対称性を崩さないようにする。
    /// </summary>
    private static void PerturbPositionTable(WeightSet ws)
    {
        int delta = _rng.Next(5, 26) * (_rng.NextDouble() < 0.5 ? 1 : -1);
        // 盤面の左上4分の1（row: 0-3, col: 0-3）からランダムに1マス選択
        int row = _rng.Next(4);
        int col = _rng.Next(4);
        // 4回転対称な4マスを同時更新
        foreach (int idx in GetSymmetricCells(row, col))
            ws.PositionTable[idx] += delta;
    }

    /// <summary>指定マスと4回転対称な4マスのインデックスを返す</summary>
    private static int[] GetSymmetricCells(int r, int c) => new[]
    {
        r       * 8 + c,       // 元のマス
        r       * 8 + (7 - c), // 左右対称
        (7 - r) * 8 + c,       // 上下対称
        (7 - r) * 8 + (7 - c)  // 点対称
    };

    /// <summary>
    /// before と after の差分を1行の文字列で返す。
    /// フェーズ重みが変わっていれば "序盤 機動力 15→18" のように、
    /// 位置テーブルが変わっていれば "位置[2,3] 100→125" のように表示する。
    /// </summary>
    private static string DescribeDiff(WeightSet before, WeightSet after)
    {
        // フェーズ重みの差分チェック
        var phaseChecks = new (string label, int bVal, int aVal)[]
        {
            ("序盤 位置",         before.Early.PosWeight,      after.Early.PosWeight),
            ("序盤 機動力",       before.Early.MobWeight,      after.Early.MobWeight),
            ("序盤 安定石",       before.Early.StaWeight,      after.Early.StaWeight),
            ("序盤 駒数差",       before.Early.DiffWeight,     after.Early.DiffWeight),
            ("序盤 パリティ",     before.Early.ParityWeight,   after.Early.ParityWeight),
            ("序盤 フロンティア", before.Early.FrontierWeight, after.Early.FrontierWeight),
            ("序盤 潜在機動力",   before.Early.PotMobWeight,   after.Early.PotMobWeight),
            ("中盤 位置",         before.Mid.PosWeight,        after.Mid.PosWeight),
            ("中盤 機動力",       before.Mid.MobWeight,        after.Mid.MobWeight),
            ("中盤 安定石",       before.Mid.StaWeight,        after.Mid.StaWeight),
            ("中盤 駒数差",       before.Mid.DiffWeight,       after.Mid.DiffWeight),
            ("中盤 パリティ",     before.Mid.ParityWeight,     after.Mid.ParityWeight),
            ("中盤 フロンティア", before.Mid.FrontierWeight,   after.Mid.FrontierWeight),
            ("中盤 潜在機動力",   before.Mid.PotMobWeight,     after.Mid.PotMobWeight),
        };
        foreach (var (label, bVal, aVal) in phaseChecks)
        {
            if (bVal != aVal)
                return $"{label} {bVal}→{aVal}";
        }

        // 位置テーブルの差分チェック
        for (int i = 0; i < 64; i++)
        {
            if (before.PositionTable[i] != after.PositionTable[i])
            {
                int r = i / 8, c = i % 8;
                return $"位置[{r},{c}] {before.PositionTable[i]}→{after.PositionTable[i]}";
            }
        }

        return "-";
    }

    /// <summary>学習後の重み内容を見やすく表示する</summary>
    private static void PrintWeightSummary(WeightSet ws)
    {
        Console.WriteLine("\n--- 学習後の重み ---");
        Console.WriteLine($"序盤: 位置={ws.Early.PosWeight,3}  機動力={ws.Early.MobWeight,3}  " +
                          $"安定石={ws.Early.StaWeight,3}  駒数差={ws.Early.DiffWeight,3}  " +
                          $"パリティ={ws.Early.ParityWeight,3}  フロンティア={ws.Early.FrontierWeight,3}  " +
                          $"潜在機動力={ws.Early.PotMobWeight,3}");
        Console.WriteLine($"中盤: 位置={ws.Mid.PosWeight,3}  機動力={ws.Mid.MobWeight,3}  " +
                          $"安定石={ws.Mid.StaWeight,3}  駒数差={ws.Mid.DiffWeight,3}  " +
                          $"パリティ={ws.Mid.ParityWeight,3}  フロンティア={ws.Mid.FrontierWeight,3}  " +
                          $"潜在機動力={ws.Mid.PotMobWeight,3}");
        Console.WriteLine("位置評価テーブル:");
        for (int r = 0; r < 8; r++)
        {
            Console.Write("  ");
            for (int c = 0; c < 8; c++)
                Console.Write($"{ws.PositionTable[r * 8 + c],5}");
            Console.WriteLine();
        }
    }
}

/// <summary>
/// 焼きなまし法（Simulated Annealing）による評価重みの自動最適化。
/// 悪化した解も確率的に受け入れることで局所最適を脱出し、
/// 適応ステップサイズで探索・活用のバランスを自動調整する。
/// </summary>
public static class SATrainer
{
    private static readonly Random _rng = new();
    private const string WeightsDir = "weights";

    /// <summary>
    /// 焼きなまし法による学習を実行する。
    /// </summary>
    /// <param name="maxIter">最大イテレーション数</param>
    /// <param name="gamesPerEval">1イテレーションあたりの対局数</param>
    /// <param name="strength">使用するAI強度</param>
    /// <param name="t0">初期温度（石数単位。大きいほど序盤に多様探索）</param>
    /// <param name="tf">終了温度（デフォルト0.1で終盤はほぼ貪欲探索）</param>
    public static void Run(int maxIter, int gamesPerEval, AIStrength strength,
                           double t0 = 5.0, double tf = 0.1)
    {
        Directory.CreateDirectory(WeightsDir);
        string savePath = Path.Combine(WeightsDir, "best_weights.json");

        WeightSet current = File.Exists(savePath)
            ? WeightSet.Load(savePath)
            : WeightSet.Default();
        // best は歴代最高の重みを保持する（current とは独立）
        WeightSet best = current.Clone();

        if (File.Exists(savePath))
            Console.WriteLine($"既存の重みを読み込みました: {Path.GetFullPath(savePath)}");
        else
            Console.WriteLine("デフォルト重みから学習を開始します。");

        Console.WriteLine($"設定: {maxIter}イテレーション / {gamesPerEval}ゲーム/評価 / AI強度:{strength}");
        Console.WriteLine($"アルゴリズム: 焼きなまし法  T0={t0:F1}石 → Tf={tf:F2}石（幾何冷却）");
        Console.WriteLine("Ctrl+C で中断できます（中断前の最善重みは自動保存済み）\n");

        int totalAdopted  = 0;
        double stepMul    = 1.0;   // 適応ステップ倍率（1/5則で自動調整）
        var recentAccepted = new Queue<bool>(); // 直近50回の採用/棄却履歴
        var startTime = DateTime.Now;

        // 定期ベスト更新：50イテレーションごとに current vs best を対戦し best を更新
        const int BestUpdateInterval = 50;

        try
        {
            for (int iter = 1; iter <= maxIter; iter++)
            {
                // 温度スケジュール（幾何冷却）: T0 → Tf
                double progress    = (double)(iter - 1) / Math.Max(maxIter - 1, 1);
                double temperature = t0 * Math.Pow(tf / t0, progress);

                WeightSet previous  = current;
                WeightSet candidate = Perturb(current, stepMul);

                // 評価：candidate vs current で gamesPerEval 回対戦
                int winsCandidate = 0, winsCurrent = 0, draws = 0;
                int totalMargin = 0;
                for (int g = 0; g < gamesPerEval; g++)
                {
                    bool candidateIsBlack = (g % 2 == 0);
                    (int b, int w) = candidateIsBlack
                        ? GameRunner.Play(candidate, current, strength)
                        : GameRunner.Play(current, candidate, strength);

                    int candidateScore = candidateIsBlack ? b : w;
                    int currentScore   = candidateIsBlack ? w : b;

                    totalMargin += candidateScore - currentScore;
                    if      (candidateScore > currentScore) winsCandidate++;
                    else if (candidateScore < currentScore) winsCurrent++;
                    else                                    draws++;
                }

                double avgMargin = (double)totalMargin / gamesPerEval;

                // SA採用判定：改善は必ず採用、悪化は exp(Δ/T) の確率で採用
                bool adopted;
                if (avgMargin >= 0)
                    adopted = true;
                else
                {
                    double prob = Math.Exp(avgMargin / temperature);
                    adopted = _rng.NextDouble() < prob;
                }

                if (adopted)
                {
                    current = candidate;
                    totalAdopted++;
                    // 改善時のみ即座にベスト更新
                    if (avgMargin > 0)
                    {
                        best = candidate.Clone();
                        best.Save(savePath);
                    }
                }

                // 定期ベスト更新（current が best より強いか確認）
                if (iter % BestUpdateInterval == 0)
                {
                    int bestMargin = 0;
                    for (int g = 0; g < gamesPerEval; g++)
                    {
                        bool currentIsBlack = (g % 2 == 0);
                        (int b, int w) = currentIsBlack
                            ? GameRunner.Play(current, best, strength)
                            : GameRunner.Play(best, current, strength);
                        int cs = currentIsBlack ? b : w;
                        int bs = currentIsBlack ? w : b;
                        bestMargin += cs - bs;
                    }
                    if (bestMargin > 0)
                    {
                        best = current.Clone();
                        best.Save(savePath);
                        Console.WriteLine($"  [定期検証] current が best を更新（差={bestMargin / (double)gamesPerEval:+0.0;-0.0}石）");
                    }
                }

                // 適応ステップサイズ（直近50回の採用率を監視し 1/5 則で調整）
                recentAccepted.Enqueue(adopted);
                if (recentAccepted.Count > 50) recentAccepted.Dequeue();
                if (recentAccepted.Count >= 20)
                {
                    double rate = recentAccepted.Count(r => r) / (double)recentAccepted.Count;
                    if      (rate > 0.30) stepMul = Math.Min(3.0, stepMul * 1.2);
                    else if (rate < 0.10) stepMul = Math.Max(0.3, stepMul * 0.8);
                }

                // 進捗表示
                string mark    = adopted ? "✓ 採用" : "  棄却";
                var    elapsed = DateTime.Now - startTime;
                string elapsedStr = elapsed.ToString(@"hh\:mm\:ss");

                var avgPerIter = elapsed / iter;
                var remaining  = avgPerIter * (maxIter - iter);
                string etaStr  = iter < 3 ? "--:--:--" : remaining.ToString(@"hh\:mm\:ss");

                int    filled = (int)(iter * 20.0 / maxIter);
                string bar    = "[" + new string('#', filled) + new string('.', 20 - filled) + "]";

                string diff = DescribeDiff(previous, candidate);

                Console.WriteLine(
                    $"{bar} {iter * 100.0 / maxIter,5:F1}%  [{iter,5}/{maxIter}] {mark}  " +
                    $"平均差={avgMargin:+0.0;-0.0}石  (勝:{winsCandidate,2} 負:{winsCurrent,2} 引:{draws,2})  " +
                    $"採用率={totalAdopted * 100.0 / iter:F1}%  T={temperature:F2}  step={stepMul:F2}  " +
                    $"経過:{elapsedStr}  残り:{etaStr}  変更:{diff}");

                if (iter % 100 == 0)
                {
                    Console.WriteLine($"\n  ── {iter}イテレーション完了 ──  採用回数:{totalAdopted}  " +
                                      $"平均:{avgPerIter.TotalSeconds:F2}秒/iter\n");
                }
            }

            Console.WriteLine($"\n学習完了。採用率={totalAdopted * 100.0 / maxIter:F1}%");
            Console.WriteLine($"最善重みを保存しました: {Path.GetFullPath(savePath)}");
            PrintWeightSummary(best);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[エラー] 学習中に例外が発生しました: {ex.Message}");
            Console.WriteLine($"  種類: {ex.GetType().Name}");
            Console.WriteLine($"最後に保存された重みは {Path.GetFullPath(savePath)} にあります。");
        }
    }

    /// <summary>重みをランダムに摂動させたコピーを返す。stepMul で摂動量をスケールする。</summary>
    private static WeightSet Perturb(WeightSet src, double stepMul = 1.0)
    {
        var next = src.Clone();
        if (_rng.NextDouble() < 0.4)
            PerturbPhaseWeights(next, stepMul);
        else
            PerturbPositionTable(next, stepMul);
        return next;
    }

    /// <summary>序盤・中盤のいずれか1つのフェーズ重みをランダムに変化させる</summary>
    private static void PerturbPhaseWeights(WeightSet ws, double stepMul)
    {
        int rawDelta = _rng.Next(2, 8);
        int delta    = (int)Math.Max(1, rawDelta * stepMul) * (_rng.NextDouble() < 0.5 ? 1 : -1);
        PhaseWeights phase = _rng.NextDouble() < 0.5 ? ws.Early : ws.Mid;
        switch (_rng.Next(7))
        {
            case 0: phase.PosWeight      = Math.Max(0, phase.PosWeight      + delta); break;
            case 1: phase.MobWeight      = Math.Max(0, phase.MobWeight      + delta); break;
            case 2: phase.StaWeight      = Math.Max(0, phase.StaWeight      + delta); break;
            case 3: phase.DiffWeight     = Math.Max(0, phase.DiffWeight     + delta); break;
            case 4: phase.ParityWeight   = Math.Max(0, phase.ParityWeight   + delta); break;
            case 5: phase.FrontierWeight = Math.Max(0, phase.FrontierWeight + delta); break;
            case 6: phase.PotMobWeight   = Math.Max(0, phase.PotMobWeight   + delta); break;
        }
    }

    /// <summary>4回転対称を維持しながら位置評価テーブルの1グループを変化させる</summary>
    private static void PerturbPositionTable(WeightSet ws, double stepMul)
    {
        int rawDelta = _rng.Next(5, 26);
        int delta    = (int)Math.Max(1, rawDelta * stepMul) * (_rng.NextDouble() < 0.5 ? 1 : -1);
        int row = _rng.Next(4);
        int col = _rng.Next(4);
        foreach (int idx in GetSymmetricCells(row, col))
            ws.PositionTable[idx] += delta;
    }

    /// <summary>指定マスと4回転対称な4マスのインデックスを返す</summary>
    private static int[] GetSymmetricCells(int r, int c) => new[]
    {
        r       * 8 + c,
        r       * 8 + (7 - c),
        (7 - r) * 8 + c,
        (7 - r) * 8 + (7 - c)
    };

    /// <summary>before と after の差分を1行の文字列で返す</summary>
    private static string DescribeDiff(WeightSet before, WeightSet after)
    {
        var phaseChecks = new (string label, int bVal, int aVal)[]
        {
            ("序盤 位置",         before.Early.PosWeight,      after.Early.PosWeight),
            ("序盤 機動力",       before.Early.MobWeight,      after.Early.MobWeight),
            ("序盤 安定石",       before.Early.StaWeight,      after.Early.StaWeight),
            ("序盤 駒数差",       before.Early.DiffWeight,     after.Early.DiffWeight),
            ("序盤 パリティ",     before.Early.ParityWeight,   after.Early.ParityWeight),
            ("序盤 フロンティア", before.Early.FrontierWeight, after.Early.FrontierWeight),
            ("序盤 潜在機動力",   before.Early.PotMobWeight,   after.Early.PotMobWeight),
            ("中盤 位置",         before.Mid.PosWeight,        after.Mid.PosWeight),
            ("中盤 機動力",       before.Mid.MobWeight,        after.Mid.MobWeight),
            ("中盤 安定石",       before.Mid.StaWeight,        after.Mid.StaWeight),
            ("中盤 駒数差",       before.Mid.DiffWeight,       after.Mid.DiffWeight),
            ("中盤 パリティ",     before.Mid.ParityWeight,     after.Mid.ParityWeight),
            ("中盤 フロンティア", before.Mid.FrontierWeight,   after.Mid.FrontierWeight),
            ("中盤 潜在機動力",   before.Mid.PotMobWeight,     after.Mid.PotMobWeight),
        };
        foreach (var (label, bVal, aVal) in phaseChecks)
            if (bVal != aVal) return $"{label} {bVal}→{aVal}";

        for (int i = 0; i < 64; i++)
        {
            if (before.PositionTable[i] != after.PositionTable[i])
            {
                int r = i / 8, c = i % 8;
                return $"位置[{r},{c}] {before.PositionTable[i]}→{after.PositionTable[i]}";
            }
        }
        return "-";
    }

    /// <summary>最善重みの内容を見やすく表示する</summary>
    private static void PrintWeightSummary(WeightSet ws)
    {
        Console.WriteLine("\n--- 最善重み（焼きなまし法） ---");
        Console.WriteLine($"序盤: 位置={ws.Early.PosWeight,3}  機動力={ws.Early.MobWeight,3}  " +
                          $"安定石={ws.Early.StaWeight,3}  駒数差={ws.Early.DiffWeight,3}  " +
                          $"パリティ={ws.Early.ParityWeight,3}  フロンティア={ws.Early.FrontierWeight,3}  " +
                          $"潜在機動力={ws.Early.PotMobWeight,3}");
        Console.WriteLine($"中盤: 位置={ws.Mid.PosWeight,3}  機動力={ws.Mid.MobWeight,3}  " +
                          $"安定石={ws.Mid.StaWeight,3}  駒数差={ws.Mid.DiffWeight,3}  " +
                          $"パリティ={ws.Mid.ParityWeight,3}  フロンティア={ws.Mid.FrontierWeight,3}  " +
                          $"潜在機動力={ws.Mid.PotMobWeight,3}");
        Console.WriteLine("位置評価テーブル:");
        for (int r = 0; r < 8; r++)
        {
            Console.Write("  ");
            for (int c = 0; c < 8; c++)
                Console.Write($"{ws.PositionTable[r * 8 + c],5}");
            Console.WriteLine();
        }
    }
}
