using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        private void ProcessPlayerJoin(CCSPlayerController player, Player cachedPlayer)
        {
            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
            
            // First process the normal VIP groups from database
            if (activeGroups.Count != 0)
            {
                ProcessPlayerWithActiveGroups(player, cachedPlayer, activeGroups);
            }
            else 
            {
                ProcessPlayerWithoutActiveGroups(player, cachedPlayer);
            }
        }

        private void ProcessPlayerWithActiveGroups(CCSPlayerController player, Player cachedPlayer, List<PlayerGroup> activeGroups)
        {
            foreach (var group in activeGroups)
            {
                var service = ServiceManager.GetService(group.GroupName);
                if (service != null)
                {
                    AssignPlayerPermissions(player, cachedPlayer, service, group);
                }
            }
        
            var primaryGroup = activeGroups.First();
            var primaryService = ServiceManager.GetService(primaryGroup.GroupName);

            if (primaryService == null) return;
        
            WelcomeMessageEveryone(player, primaryService, activeGroups.Count);
            ScheduleWelcomeMessage(player, activeGroups);
        }

        private void ProcessPlayerWithoutActiveGroups(CCSPlayerController player, Player cachedPlayer)
        {
            var externalVipGroups = CheckExternalPermissions(player);
            
            if (externalVipGroups.Count > 0)
            {
                ProcessPlayerWithExternalPermissions(player, cachedPlayer, externalVipGroups);
            }
            // Check for night VIP
            else if (Config!.NightVip.Enabled && IsNightVipTime() && !cachedPlayer.Active)
            {
                AssignNightVip(player);
                ScheduleNightVipWelcomeMessage(player);
            }
            else
            {
                ProcessPlayerWithNoVip(player, cachedPlayer);
            }
        }

        private void ProcessPlayerWithExternalPermissions(CCSPlayerController player, Player cachedPlayer, List<Service> externalVipGroups)
        {
            // Apply VIP status without adding to database
            foreach (var service in externalVipGroups.OfType<Service>())
            {
                ApplyExternalVipPermissions(player, cachedPlayer, service);
            }
            
            var primaryService = externalVipGroups.First();
            
            // Show welcome messages
            WelcomeMessageEveryone(player, primaryService, externalVipGroups.Count);
            ScheduleExternalWelcomeMessage(player, externalVipGroups);
        }

        private void ProcessPlayerWithNoVip(CCSPlayerController player, Player cachedPlayer)
        {
            InvalidateCachedPlayer(cachedPlayer);
            LogNoActiveService(player);
    
            AddTimer(15.0f, () =>
            {
                if (!player.IsValid)
                    return;
        
                ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.title", player.PlayerName);
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.info");
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.suggestion");
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.website");
                ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
            });
        }

        private static void InvalidateCachedPlayer(Player cachedPlayer)
        {
            cachedPlayer.Active = false;
            cachedPlayer.LoadedGroup = null;
            
            foreach (var group in cachedPlayer.Groups)
            {
                group.Active = false;
            }
        }
        
        /// <summary>
        /// Checks if a player has a specific feature across any of their active VIP groups
        /// </summary>
        private static bool PlayerHasFeature(CCSPlayerController player, Func<Service, bool> featureCheck)
        {
            if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer)) 
                return false;
    
            if (!cachedPlayer.Active) 
                return false;
    
            // Get all active groups for the player
            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
    
            // Check each group for the feature
            foreach (var group in activeGroups)
            {
                var service = ServiceManager.GetService(group.GroupName);
                if (service != null && featureCheck(service))
                    return true;
            }
    
            return false;
        }
    }
}