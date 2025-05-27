using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    private void cmd_AddVipGroup(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || !CheckCommandAccess(player))
            return;
        
        ShowAddVipPlayerSelectionMenu(player);
    }

    private void ShowAddVipPlayerSelectionMenu(CCSPlayerController admin)
    {
        var onlinePlayers = Utilities.GetPlayers()
            .Where(p => p is { IsValid: true, IsBot: false })
            .ToList();
        
        if (onlinePlayers.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.player.none");
            return;
        }
        
        ShowPlayerSelectionMenu(admin, onlinePlayers, _localizer!.ForPlayer(admin, "admin.menu.title.selectplayer"), (selectedPlayer) => {
            ShowGroupSelectionMenu(admin, selectedPlayer);
        });
    }

    private void ShowGroupSelectionMenu(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(admin, "admin.menu.title.selectgroup", targetPlayer.PlayerName), isSubMenu: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.player.info", targetPlayer.PlayerName, targetPlayer.SteamID), (_, _) => { });
        
        foreach (var group in Config!.GroupSettings)
        {
            menu.AddOption(group.Name, (p, _) => {
                var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
                var existingGroup = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == group.Name);
                
                if (existingGroup != null && existingGroup.Active)
                {
                    ShowGroupExistsMenu(p, targetPlayer, group.Name, existingGroup.ExpiryTime);
                }
                else
                {
                    ShowDurationSelectionMenu(p, targetPlayer, group.Name);
                }
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            ShowAddVipPlayerSelectionMenu(p);
        });
        
        manager.OpenSubMenu(admin, menu);
    }
    
    private void ShowGroupExistsMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName, int currentExpiryTime)
    {
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(admin, "admin.menu.title.groupexists", targetPlayer.PlayerName, groupName), isSubMenu: true);
        
        var expiryInfo = currentExpiryTime == 0 
            ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
            : _localizer!.ForPlayer(admin, "admin.group.expiry.until", FormatExpiryDate(admin, currentExpiryTime));
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.groupexists.info", groupName, expiryInfo), (_, _) => { });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.groupexists.replace"), (p, _) => {
            ShowDurationSelectionMenu(p, targetPlayer, groupName);
        });
        
        if (currentExpiryTime > 0)
        {
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.groupexists.extend"), (p, _) => {
                ShowExtendDurationMenu(p, targetPlayer, groupName, currentExpiryTime);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.cancel"), (p, _) => {
            ShowGroupSelectionMenu(p, targetPlayer);
        });
        
        manager.OpenSubMenu(admin, menu);
    }
    
    private void ShowExtendDurationMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName, int currentExpiryTime)
    {
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(admin, "admin.menu.title.extendduration", targetPlayer.PlayerName, groupName), isSubMenu: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.extend.currentexpiry", 
            FormatExpiryDate(admin, currentExpiryTime)), (_, _) => { });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.oneday"), (p, _) => {
            ExtendVipGroupForPlayer(p, targetPlayer, groupName, 1, currentExpiryTime);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.oneweek"), (p, _) => {
            ExtendVipGroupForPlayer(p, targetPlayer, groupName, 7, currentExpiryTime);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.onemonth"), (p, _) => {
            ExtendVipGroupForPlayer(p, targetPlayer, groupName, 30, currentExpiryTime);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.threemonths"), (p, _) => {
            ExtendVipGroupForPlayer(p, targetPlayer, groupName, 90, currentExpiryTime);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.sixmonths"), (p, _) => {
            ExtendVipGroupForPlayer(p, targetPlayer, groupName, 180, currentExpiryTime);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.oneyear"), (p, _) => {
            ExtendVipGroupForPlayer(p, targetPlayer, groupName, 365, currentExpiryTime);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            ShowGroupExistsMenu(p, targetPlayer, groupName, currentExpiryTime);
        });
        
        manager.OpenSubMenu(admin, menu);
    }

    private void ShowDurationSelectionMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName)
    {
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(admin, "admin.menu.title.selectduration", targetPlayer.PlayerName, groupName), isSubMenu: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.permanent"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 0);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.oneday"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 1);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.oneweek"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 7);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.onemonth"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 30);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.threemonths"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 90);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.sixmonths"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 180);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.duration.oneyear"), (p, _) => {
            AddVipGroupToPlayer(p, targetPlayer, groupName, 365);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            ShowGroupSelectionMenu(p, targetPlayer);
        });
        
        manager.OpenSubMenu(admin, menu);
    }

    private void AddVipGroupToPlayer(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName, int days)
    {
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.notfound", groupName);
            return;
        }
        
        var expiryTime = 0;
        if (days > 0)
        {
            expiryTime = (int)DateTimeOffset.UtcNow.AddDays(days).ToUnixTimeSeconds();
        }
        
        AddGroupToPlayer(targetPlayer.SteamID, groupName, expiryTime);
        
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        var group = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == groupName);
        
        if (group != null)
        {
            AdminManager.AddPlayerPermissions(targetPlayer, service.Flag);
        }
        
        var expiryMsg = expiryTime == 0 
            ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
            : _localizer!.ForPlayer(admin, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(expiryTime).ToLocalTime());
            
        ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.added", groupName, targetPlayer.PlayerName, expiryMsg);

        var expiryMessageForChat = expiryTime == 0 ? _localizer!.ForPlayer(targetPlayer, "commands.vip.details.neverexpires") : FormatExpiryDate(targetPlayer, expiryTime, "date.format.with_time");
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.added", groupName);
        
        if (expiryTime == 0) {
            ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.expiry.never");
        } else {
            ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.expiry", expiryMessageForChat);
        }
        
        ShowAddVipPlayerSelectionMenu(admin);
    }
    
    private void ExtendVipGroupForPlayer(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName, int daysToAdd, int currentExpiryTime)
    {
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.notfound", groupName);
            return;
        }
        
        var currentDate = DateTimeOffset.FromUnixTimeSeconds(currentExpiryTime);
        var newExpiryTime = (int)currentDate.AddDays(daysToAdd).ToUnixTimeSeconds();
        
        AddGroupToPlayer(targetPlayer.SteamID, groupName, newExpiryTime);
        
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        var group = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == groupName);
        
        if (group != null)
        {
            AdminManager.AddPlayerPermissions(targetPlayer, service.Flag);
        }
        
        var oldExpiryFormatted = FormatExpiryDate(admin, currentExpiryTime);
        var newExpiryFormatted = FormatExpiryDate(admin, newExpiryTime);

        ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.extended", 
            groupName, targetPlayer.PlayerName, 
            oldExpiryFormatted, 
            newExpiryFormatted, 
            daysToAdd);
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.extended", 
            groupName, FormatExpiryDate(targetPlayer, newExpiryTime, "date.format.with_time"), daysToAdd);
        
        ShowAddVipPlayerSelectionMenu(admin);
    }
}