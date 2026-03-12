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
        switch (_rng.Next(5))
        {
            case 0: phase.PosWeight    = Math.Max(0, phase.PosWeight    + delta); break;
            case 1: phase.MobWeight    = Math.Max(0, phase.MobWeight    + delta); break;
            case 2: phase.StaWeight    = Math.Max(0, phase.StaWeight    + delta); break;
            case 3: phase.DiffWeight   = Math.Max(0, phase.DiffWeight   + delta); break;
            case 4: phase.ParityWeight = Math.Max(0, phase.ParityWeight + delta); break;
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
            ("序盤 位置",     before.Early.PosWeight,    after.Early.PosWeight),
            ("序盤 機動力",   before.Early.MobWeight,    after.Early.MobWeight),
            ("序盤 安定石",   before.Early.StaWeight,    after.Early.StaWeight),
            ("序盤 駒数差",   before.Early.DiffWeight,   after.Early.DiffWeight),
            ("序盤 パリティ", before.Early.ParityWeight, after.Early.ParityWeight),
            ("中盤 位置",     before.Mid.PosWeight,      after.Mid.PosWeight),
            ("中盤 機動力",   before.Mid.MobWeight,      after.Mid.MobWeight),
            ("中盤 安定石",   before.Mid.StaWeight,      after.Mid.StaWeight),
            ("中盤 駒数差",   before.Mid.DiffWeight,     after.Mid.DiffWeight),
            ("中盤 パリティ", before.Mid.ParityWeight,   after.Mid.ParityWeight),
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
                          $"安定石={ws.Early.StaWeight,3}  駒数差={ws.Early.DiffWeight,3}  パリティ={ws.Early.ParityWeight,3}");
        Console.WriteLine($"中盤: 位置={ws.Mid.PosWeight,3}  機動力={ws.Mid.MobWeight,3}  " +
                          $"安定石={ws.Mid.StaWeight,3}  駒数差={ws.Mid.DiffWeight,3}  パリティ={ws.Mid.ParityWeight,3}");
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
