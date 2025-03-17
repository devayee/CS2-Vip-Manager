using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        private static bool IsNightVipTime()
        {
            if (Config is { NightVip.Enabled: false })
                return false;
        
            var currentTime = DateTimeOffset.Now.Hour;
            if (Config == null) return false;
            var startTime = Config.NightVip.StartHour;
            var endTime = Config.NightVip.EndHour;

            if (startTime < endTime)
            {
                return currentTime >= startTime && currentTime < endTime;
            }

            return currentTime >= startTime || currentTime < endTime;
        }
        
        private static void AssignNightVip(CCSPlayerController player)
        {
            var cachedPlayer = GetOrCreatePlayer(player.SteamID, player.PlayerName);
            
            // Check if player already has any active group
            if (cachedPlayer.Groups.Any(g => g.Active))
            {
                return;
            }
            
            // Find the service to inherit from
            var inheritGroup = Config!.NightVip.InheritGroup;
            var service = ServiceManager.GetService(inheritGroup);
            if (service == null)
            {
                Console.WriteLine($"[Mesharsky - VIP] ERROR: Night VIP inheritance group '{inheritGroup}' not found");
                return;
            }
            
            // Create a temporary night VIP group
            var nightVipGroup = new PlayerGroup
            {
                GroupName = inheritGroup,
                ExpiryTime = 0,
                Active = true,
                PlayerName = player.PlayerName,
            };
            
            cachedPlayer.Groups.Add(nightVipGroup);
            
            cachedPlayer.LoadedGroup = inheritGroup;
            cachedPlayer.Active = true;
            
            PlayerCache[player.SteamID] = cachedPlayer;

            AdminManager.AddPlayerPermissions(player, Config.NightVip.Flag);
            
            Console.WriteLine($"[Mesharsky - VIP] Assigned Night VIP (inheriting from {inheritGroup}) to player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ]");
        }

        private static void RemoveNightVip(CCSPlayerController player)
        {
            if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer)) 
                return;
            
            var inheritGroup = Config!.NightVip.InheritGroup;
            
            var nightVipGroup = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == inheritGroup && g.ExpiryTime == 0);
            
            if (nightVipGroup == null) 
                return;
            
            var hasOtherActiveGroups = cachedPlayer.Groups
                .Any(g => g != nightVipGroup && g.Active);
            
            bool hasExternalPermissions;
            if (ExternalPermissionsCache.TryGetValue(player.SteamID, out var cachedPermissions))
            {
                hasExternalPermissions = cachedPermissions.Services.Count > 0;
            }
            else
            {
                var externalServices = CheckExternalPermissions(player);
                hasExternalPermissions = externalServices.Count > 0;
            }
            
            if (!hasOtherActiveGroups && !hasExternalPermissions)
            {
                RemoveNightVipGroup(player, cachedPlayer, nightVipGroup);
            }
            else
            {
                Console.WriteLine($"[Mesharsky - VIP] Player {player.PlayerName} has Night VIP but also has other active VIP services - not removing Night VIP");
            }
        }
        
        private static void RemoveNightVipGroup(CCSPlayerController player, Player cachedPlayer, PlayerGroup nightVipGroup)
        {
            cachedPlayer.Groups.Remove(nightVipGroup);
            
            var remainingActiveGroup = cachedPlayer.GetPrimaryGroup();
            if (remainingActiveGroup != null)
            {
                cachedPlayer.LoadedGroup = remainingActiveGroup.GroupName;
                cachedPlayer.GroupExpiryTime = remainingActiveGroup.ExpiryTime;
                cachedPlayer.Active = true;
            }
            else
            {
                cachedPlayer.LoadedGroup = null;
                cachedPlayer.GroupExpiryTime = 0;
                cachedPlayer.Active = false;
            }
            
            AdminManager.RemovePlayerPermissions(player, Config!.NightVip.Flag);
            
            Console.WriteLine($"[Mesharsky - VIP] Removed Night VIP from player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ]");
            
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.expired.title");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.expired.info");
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        }
    }
}