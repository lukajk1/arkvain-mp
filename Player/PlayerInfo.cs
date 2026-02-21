using PurrNet;

public struct PlayerInfo
{
    public PlayerID playerID;
    public string name;

    public PlayerInfo(PlayerID id, string playerName = "")
    {
        playerID = id;
        name = playerName;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(name) ? playerID.ToString() : name;
    }
}
