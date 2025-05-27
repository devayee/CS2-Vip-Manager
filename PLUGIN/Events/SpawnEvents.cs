using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if (IsWarmup())
                return HookResult.Continue;

            var player = @event.Userid;

            if (player == null)
                return HookResult.Continue;

            if (!IsValidPlayer(player))
                return HookResult.Continue;
            
            AddTimer(0.6f, () => ProcessPlayerSpawn(player));

            return HookResult.Continue;
        }

        private void ProcessPlayerSpawn(CCSPlayerController player)
        {
            if (!player.IsValid)
                return;
            
            var steamId = player.SteamID;

            if (!PlayerCache.TryGetValue(steamId, out var cachedPlayer))
            {
                cachedPlayer = GetOrCreatePlayer(steamId, player.PlayerName);
            }

            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
            
            if (activeGroups.Count != 0)
            {
                var activeServices = activeGroups
                    .Select(g => ServiceManager.GetService(g.GroupName))
                    .Where(s => s != null)
                    .ToList();
        
                if (activeServices.Count != 0)
                {
                    PlayerSpawn_CombineBonuses(player, activeServices);
                }
            }
            else
            {
                var externalServices = CheckExternalPermissions(player);
                if (externalServices.Count > 0)
                {
                    PlayerSpawn_CombineBonuses(player, externalServices!);
                }
            }
        }
    }
}