using System;

public class OthelloAI
{
    public (int x, int y) AI(List<(int x, int y)> values, int[,] tiles)
    {
        //そこのマスに駒を設置したときに、将来的に取られる可能性のあるマスの個数を計算して
        //評価する。取られるマスが一番少ないマスはどこかを探して返す。
        return (0, 0); // 仮の戻り値
    }

    public enum AIStrength
    {
        nuub,
        normal,
        professional
    }
}