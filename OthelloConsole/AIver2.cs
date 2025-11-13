// class AIver2
// {
//     /// <summary>
//     /// NegaMaxアルゴリズムによるストーンの探索
//     /// </summary>
//     /// <param name="stoneStates">ストーン状態配列</param>
//     /// <param name="putStoneState">置くストーン状態</param>
//     /// <param name="depth">探索する深さ</param>
//     /// <returns>探索したストーン</returns>
//     public static StoneIndex SearchNegaMaxStone(StoneState[,] stoneStates, StoneState putStoneState, int depth)
//     {
//         // 探索したストーン
//         StoneIndex resultStoneIndex = null;

//         // 置くことが可能なストーンを全て調べる
//         var maxScore = int.MinValue;
//         var canPutStonesIndex = StoneCalculator.GetAllCanPutStonesIndex(stoneStates, putStoneState);
//         foreach (var putStoneIndex in canPutStonesIndex)
//         {
//             // 次の階層の状態を調べる
//             var putStoneStates = StoneCalculator.GetPutStoneState(stoneStates, putStoneState, putStoneIndex.X, putStoneIndex.Z);
//             var score = -1 * GetNegaMaxScore(putStoneStates, GetReverseStoneState(putStoneState), depth - 1);

//             // 最大スコアの場合、スコアと該当インデックスを保持
//             if (maxScore < score)
//             {
//                 maxScore = score;
//                 resultStoneIndex = putStoneIndex;
//             }
//         }
//         return resultStoneIndex;
//     }

//     /// <summary>
//     /// NegaMaxアルゴリズムによるスコアの計算
//     /// </summary>
//     /// <param name="stoneStates">ストーン状態の配列</param>
//     /// <param name="putStoneState">置くストーン状態</param>
//     /// <param name="depth">探索する深さ</param>
//     /// <param name="isPrevPassed">上の階層でパスしたか？</param>
//     /// <returns>指定階層の最大スコア</returns>
//     private static int GetNegaMaxScore(StoneState[,] stoneStates, StoneState putStoneState, int depth, bool isPrevPassed = false)
//     {
//         // 葉ノードで評価関数を実行
//         if (depth == 0) return EvaluateStoneStates(stoneStates, putStoneState);

//         // 置くことが可能なストーンを全て調べる
//         var maxScore = int.MinValue;
//         var canPutStonesIndex = StoneCalculator.GetAllCanPutStonesIndex(stoneStates, putStoneState);
//         foreach (var putStoneIndex in canPutStonesIndex)
//         {
//             // 次の階層の状態を調べる
//             var putStoneStates = StoneCalculator.GetPutStoneState(stoneStates, putStoneState, putStoneIndex.X, putStoneIndex.Z);
//             maxScore = Math.Max(maxScore, -1 * GetNegaMaxScore(putStoneStates, GetReverseStoneState(putStoneState), depth - 1));
//         }

//         // 見つからなかった場合
//         if (maxScore == int.MinValue)
//         {
//             // ２回連続パスの場合、評価関数を実行
//             if (isPrevPassed) return EvaluateStoneStates(stoneStates, putStoneState);
//             // ストーン状態はそのままで、次の階層の状態を調べる
//             return -1 * GetNegaMaxScore(stoneStates, GetReverseStoneState(putStoneState), depth - 1, true);
//         }
//         return maxScore;
//     }
// }