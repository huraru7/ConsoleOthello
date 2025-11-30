using System;

public struct MainGameData
{
    public int[,] _tiles;
    public GameTurn _gameTurn;
    public int _turnConter;
}

public enum GameTurn
{
    prayer1,
    prayer2,
    AI
}
public enum GameMode
{
    AIvsPlayer,
    PlayervsPlayer
}