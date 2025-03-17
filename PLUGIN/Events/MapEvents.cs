using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        private void RegisterMapEvents()
        {
            RegisterListener<Listeners.OnMapStart>(OnMapStartEvent);
        }

        private void OnMapStartEvent(string mapName)
        {
            _onMapStart?.Kill();
            _onMapStart = AddTimer(60, OnMapTimerTick, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void OnMapTimerTick()
        {
            foreach (var (key, cachedPlayer) in PlayerCache)
            {
                var player = Utilities.GetPlayerFromSteamId(key);
                if (player == null)
                    continue;

                if (IsNightVipTime())
                {
                    ProcessNightVipAssignment(player, cachedPlayer, key);
                }
                else
                {
                    ProcessNightVipRemoval(player, cachedPlayer);
                }
            }
        }

        private void ProcessNightVipAssignment(CCSPlayerController player, Player cachedPlayer, ulong steamId)
        {
            var hasActiveGroups = cachedPlayer.Groups.Any(g => g.Active);
            
            var hasExternalPermissions = false;
            if (ExternalPermissionsCache.TryGetValue(steamId, out var cachedPermissions))
            {
                hasExternalPermissions = cachedPermissions.Services.Count > 0;
            }
            
            if (!hasActiveGroups && !hasExternalPermissions)
            {
                Console.WriteLine($"[Mesharsky - VIP] Assigning Night VIP to player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ].");
                AssignNightVip(player);
                ScheduleNightVipWelcomeMessage(player);
            }
            else
            {
                Console.WriteLine($"[Mesharsky - VIP] Player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ] already has an active service or Night VIP.");
            }
        }

        private void ProcessNightVipRemoval(CCSPlayerController player, Player cachedPlayer)
        {
            var hasNightVip = cachedPlayer.Groups.Any(g => 
                g.GroupName == Config!.NightVip.InheritGroup && 
                g is { ExpiryTime: 0, Active: true });

            if (!hasNightVip) return;
            
            Console.WriteLine($"[Mesharsky - VIP] Night VIP time is over - checking if player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ] should keep VIP.");
            RemoveNightVip(player);
        }
    }
}