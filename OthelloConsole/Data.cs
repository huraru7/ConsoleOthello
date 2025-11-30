using System;

public struct MainGameData
{
    public GameTurn _gameTurn;

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