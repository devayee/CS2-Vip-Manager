using System.Globalization;
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

    private static void WelcomeMessageEveryone(CCSPlayerController player, Service primaryService, int groupCount)
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

    private void ScheduleWelcomeMessage(CCSPlayerController player, List<PlayerGroup> activeGroups)
    {
        AddTimer(15.0f, () =>
        {
            if (!player.IsValid)
                return;
            
            var earliestExpiry = activeGroups
                .Where(g => g.ExpiryTime > 0)
                .OrderBy(g => g.ExpiryTime)
                .FirstOrDefault();
            
            var servicesList = string.Join(", ", activeGroups.Select(g => {
                var svc = ServiceManager.GetService(g.GroupName);
                return svc != null ? $"{svc.Name}" : g.GroupName;
            }));

            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.title", player.PlayerName);
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.services", servicesList);
            
            if (earliestExpiry == null || earliestExpiry.ExpiryTime == 0)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.expiry.never");
            }
            else
            {
                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(earliestExpiry.ExpiryTime).ToLocalTime();
                var expiryDateString = expiryDate.ToString("dd MMMM, 'o godzinie' HH:mm", new CultureInfo("pl-PL"));

                var now = DateTimeOffset.Now;
                var timeRemaining = expiryDate - now;
                var timeRemainingMessage = "";

                if (timeRemaining.TotalDays is <= 3 and > 0)
                {
                    timeRemainingMessage = _localizer!["vip.welcome.expiry.warning", (int)timeRemaining.TotalDays];
                }
                else
                {
                    var years = timeRemaining.Days / 365;
                    var months = timeRemaining.Days % 365 / 30;
                    var days = timeRemaining.Days % 365 % 30;

                    if (years > 0)
                    {
                        var translationKey = years == 1 
                            ? "vip.welcome.expiry.years.one" 
                            : "vip.welcome.expiry.years.many";
                        
                        var monthTranslationKey = months == 1 
                            ? "vip.welcome.expiry.months.one" 
                            : "vip.welcome.expiry.months.many";
                        
                        var dayTranslationKey = days == 1 
                            ? "vip.welcome.expiry.days.one" 
                            : "vip.welcome.expiry.days.many";
                        
                        timeRemainingMessage = _localizer!["vip.welcome.expiry.years", 
                            years, 
                            _localizer![translationKey],
                            months,
                            _localizer![monthTranslationKey],
                            days,
                            _localizer![dayTranslationKey]];
                    }
                    else if (months > 0)
                    {
                        var monthTranslationKey = months == 1 
                            ? "vip.welcome.expiry.months.one" 
                            : "vip.welcome.expiry.months.many";
                        
                        var dayTranslationKey = days == 1 
                            ? "vip.welcome.expiry.days.one" 
                            : "vip.welcome.expiry.days.many";
                        
                        timeRemainingMessage = _localizer!["vip.welcome.expiry.months", 
                            months,
                            _localizer![monthTranslationKey],
                            days,
                            _localizer![dayTranslationKey]];
                    }
                    else if (days > 0)
                    {
                        var dayTranslationKey = days == 1 
                            ? "vip.welcome.expiry.days.one" 
                            : "vip.welcome.expiry.days.many";
                        
                        timeRemainingMessage = _localizer!["vip.welcome.expiry.days", 
                            days,
                            _localizer![dayTranslationKey]];
                    }
                }

                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.expiry.date", expiryDateString, timeRemainingMessage);
            }
            
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        });
    }

    private void ScheduleExternalWelcomeMessage(CCSPlayerController player, List<Service?> services)
    {
        AddTimer(15.0f, () =>
        {
            if (!player.IsValid)
                return;
    
            var servicesList = string.Join(", ", services.Select(s => s.Name));

            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.title", player.PlayerName);
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.services", servicesList);
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.external.permission");
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        });
    }
}