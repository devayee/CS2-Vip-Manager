using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private static void LogNoActiveService(CCSPlayerController player)
    {
        Console.WriteLine($"[Mesharsky - VIP] No active service for player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ]");
    }

    private void ScheduleNightVipWelcomeMessage(CCSPlayerController player)
    {
        if (!player.IsValid)
            return;

        AddTimer(15.0f, () =>
        {
            if (!player.IsValid)
                return;

            var inheritGroup = Config!.NightVip.InheritGroup;
            var service = ServiceManager.GetService(inheritGroup);
            if (service == null)
                return;

            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.welcome.title", player.PlayerName);
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.welcome.info", service.Name);
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.welcome.expiry", Config.NightVip.EndHour);
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        });
    }

    private static void LogDatabaseNotLoaded(CCSPlayerController player)
    {
        Console.WriteLine($"[Mesharsky - VIP] Database not Loaded [ Name: {player.PlayerName} - SteamID: {player.SteamID} ] won't be processed");
    }

    private void WelcomeMessageEveryone(CCSPlayerController player, Service primaryService, int groupCount)
    {
        if (!player.IsValid)
            return;

        var otherGroupsText = "";
        if (groupCount > 1)
        {
            var translationKey = (groupCount - 1) == 1 
                ? "vip.player.join.multiplegroups.one" 
                : "vip.player.join.multiplegroups.many";
            
            otherGroupsText = _localizer![translationKey, (groupCount - 1)];
        }

        ChatHelper.PrintLocalizedChatToAll(_localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChatToAll(_localizer!, true, "vip.player.join", player.PlayerName, primaryService.Name, otherGroupsText);
        ChatHelper.PrintLocalizedChatToAll(_localizer!, false, "global.divider");
    }

    private void GoodbyeMessageEveryone(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        
        var cachedPlayer = PlayerCache[steamId];

        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        if (activeGroups.Count == 0) return;
        
        var primaryService = ServiceManager.GetService(activeGroups.First().GroupName);
        if (primaryService == null) return;
        
        var otherGroupsText = "";
        if (activeGroups.Count > 1)
        {
            var translationKey = (activeGroups.Count - 1) == 1 
                ? "vip.player.join.multiplegroups.one" 
                : "vip.player.join.multiplegroups.many";
            
            otherGroupsText = _localizer![translationKey, (activeGroups.Count - 1)];
        }
                
        ChatHelper.PrintLocalizedChatToAll(_localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChatToAll(_localizer!, true, "vip.player.leave", player.PlayerName, primaryService.Name, otherGroupsText);
        ChatHelper.PrintLocalizedChatToAll(_localizer!, false, "global.divider");
    }
}