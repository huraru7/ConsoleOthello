public struct MainGameData
{
    public int[,] _tiles;
    public GameTurn _gameTurn;
    public int _turnConter;
    public int _turnNum;
    public GameMode _gameMode;
    public AIStrength _AIStrength;
    public int _tilesSize;

    public MainGameData Clone()
    {
        MainGameData clone = new MainGameData();
        clone._tiles = (int[,])this._tiles.Clone();
        clone._gameTurn = this._gameTurn;
        clone._turnConter = this._turnConter;
        clone._turnNum = this._turnNum;
        clone._gameMode = this._gameMode;
        clone._AIStrength = this._AIStrength;
        clone._tilesSize = this._tilesSize;
        return clone;
    }
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