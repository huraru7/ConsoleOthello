using System;
using System.Collections.Generic;

public class OthelloAI
{
    private int _depth = 0; // 探索の深さ
    private int _nodeCount = 0;
    private int _player;
    private int _AI;
    private const int BOARD_SIZE = 8;

    public (int x, int y) AI(List<(int x, int y)> _validMoves, int[,] _board, int player, AIStrength _AIStrength)
    {
        _nodeCount = 0;
        _player = player;
        _AI = (player == 1) ? 2 : 1;

        switch (_AIStrength)
        {
            case AIStrength.whet: _depth = 0; break;
            case AIStrength.nuub: _depth = 2; break;
            case AIStrength.normal: _depth = 4; break;
            case AIStrength.professional: _depth = 6; break;
        }

        int bestScore = int.MinValue;
        (int x, int y) bestMove = (0, 0);

        foreach (var move in _validMoves)
        {
            _nodeCount++;
            int[,] newBoard = CopyBoard(_board);
            AIPlacePiece(move.x, move.y, newBoard, _AI);

            int score = -Negamax(newBoard, _depth - 1, _player, int.MinValue + 1, int.MaxValue - 1);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        Console.Write($"探索ノード数: {_nodeCount}");
        return bestMove;
    }

    private int Negamax(int[,] board, int depth, int player, int alpha, int beta, int level = 0)
    {
        _nodeCount++;
        if (depth == 0)
        {
            int eval = EvaluatePosition(board);
            return eval;
        }

        var validMoves = GetValidMoves(board, player);
        if (validMoves.Count == 0)
        {
            int eval = EvaluatePosition(board);
            return eval;
        }

        int opponent = (player == 1) ? 2 : 1;
        int maxEval = int.MinValue;

        foreach (var move in validMoves)
        {
            int[,] newBoard = AIPlacePiece(move.x, move.y, board, player);

            int eval = -Negamax(newBoard, depth - 1, opponent, -beta, -alpha, level + 1);


            if (eval > maxEval)
            {
                maxEval = eval;
            }

            alpha = Math.Max(alpha, eval);
            if (alpha >= beta)
            {
                break;
            }
        }

        return maxEval;
    }


    private List<(int x, int y)> GetValidMoves(int[,] board, int player)
    {
        List<(int x, int y)> moves = new();
        int opponent = (player == 1) ? 2 : 1;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                if (board[y, x] != 0) continue;

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = x + dx[dir];
                    int ny = y + dy[dir];
                    bool hasOpponent = false;

                    while (nx >= 0 && nx < BOARD_SIZE && ny >= 0 && ny < BOARD_SIZE)
                    {
                        if (board[ny, nx] == opponent)
                        {
                            hasOpponent = true;
                        }
                        else if (board[ny, nx] == player && hasOpponent)
                        {
                            moves.Add((x + 1, y + 1)); // 1-indexで返す
                            goto NextCell;
                        }
                        else break;

                        nx += dx[dir];
                        ny += dy[dir];
                    }
                }
            NextCell:;
            }
        }
        return moves;
    }

    private int[,] AIPlacePiece(int x, int y, int[,] board, int turn)
    {
        int[,] copy = (int[,])board.Clone(); // ← 新しい盤面を複製
        int current = turn;
        int opponent = (turn == 1) ? 2 : 1;

        // 盤面上は 0-index なので調整
        x -= 1;
        y -= 1;
        copy[y, x] = current;

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int dir = 0; dir < dx.Length; dir++)
        {
            List<(int x, int y)> candidates = new List<(int x, int y)>();
            int nx = x + dx[dir];
            int ny = y + dy[dir];

            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                if (copy[ny, nx] == opponent)
                {
                    candidates.Add((nx, ny));
                }
                else if (copy[ny, nx] == current)
                {
                    if (candidates.Count > 0)
                    {
                        foreach (var c in candidates)
                            copy[c.y, c.x] = current;
                    }
                    break;
                }
                else break;

                nx += dx[dir];
                ny += dy[dir];
            }
        }

        return copy;
    }


    private int EvaluatePosition(int[,] board)
    {
        int[,] scoreSheet = new int[,]
        {
            { 30, -12, 0, -1, -1, 0, -12, 30 },
            { -12, -15, -3, -3, -3, -3, -15, -12 },
            { 0, -3, 0, -1, -1, 0, -3, 0 },
            { -1, -3, -1, -1, -1, -1, -3, -1 },
            { -1, -3, -1, -1, -1, -1, -3, -1 },
            { 0, -3, 0, -1, -1, 0, -3, 0 },
            { -12, -15, -3, -3, -3, -3, -15, -12 },
            { 30, -12, 0, -1, -1, 0, -12, 30 },
        };

        int score = 0;
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                if (board[y, x] == _AI)
                    score += scoreSheet[y, x];
                else if (board[y, x] == _player)
                    score -= scoreSheet[y, x];
            }
        }
        return score;
    }

    private int[,] CopyBoard(int[,] board)
    {
        int[,] copy = new int[BOARD_SIZE, BOARD_SIZE];
        Array.Copy(board, copy, board.Length);
        return copy;
    }
}

public enum AIStrength
{
    whet,
    nuub,
    normal,
    professional
}
