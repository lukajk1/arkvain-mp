using PurrNet;
using System.Collections.Generic;

public static class PlayerInfoManager
{
    private static Dictionary<PlayerID, PlayerInfo> _playerInfos = new();
    private static int _playerCounter = 0;

    public static PlayerInfo Register(PlayerID id)
    {
        _playerCounter++;
        var info = new PlayerInfo { name = $"Player #{_playerCounter}" };
        _playerInfos[id] = info;
        return info;
    }

    public static PlayerInfo Get(PlayerID id) => _playerInfos[id];

    public static bool TryGet(PlayerID id, out PlayerInfo info) => _playerInfos.TryGetValue(id, out info);

    public static void Remove(PlayerID id) => _playerInfos.Remove(id);

    public static void Clear()
    {
        _playerInfos.Clear();
        _playerCounter = 0;
    }

    public static IReadOnlyDictionary<PlayerID, PlayerInfo> GetAll() => _playerInfos;
}
