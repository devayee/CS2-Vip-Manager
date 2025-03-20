using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        private void RegisterPlayerEvents()
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot)
                return HookResult.Continue;

            var steamId = player.SteamID;
            var playerName = player.PlayerName;

            if (!_databaseLoaded)
            {
                LogDatabaseNotLoaded(player);
                return HookResult.Continue;
            }

            var cachedPlayer = GetOrCreatePlayer(steamId, playerName);
            ProcessPlayerJoin(player, cachedPlayer);
            
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || player.IsBot)
                return HookResult.Continue;

            var steamId = player.SteamID;

            if (PlayerCache.TryGetValue(steamId, out var cachedPlayer))
            {
                SavePlayerWeaponPreferences(player);
                
                GoodbyeMessageEveryone(player);
                PlayerCache.TryRemove(steamId, out _);
                Console.WriteLine($"[Mesharsky - VIP] Removed player from cache [ SteamID: {steamId} ]");
            }

            if (ExternalPermissionsCache.ContainsKey(steamId))
            {
                ExternalPermissionsCache.Remove(steamId);
                Console.WriteLine($"[Mesharsky - VIP] Cleared external permissions cache for player [ SteamID: {steamId} ]");
            }

            return HookResult.Continue;
        }
    }
}