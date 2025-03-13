using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public void RegisterAdminCommands()
    {
        var commandManager = GetCommandManager();
        
        commandManager.RegisterCommand("addvip_command", "Add a VIP group to a player", cmd_AddVipGroup);
        commandManager.RegisterCommand("removevip_command", "Remove a VIP group from a player", cmd_RemoveVipGroup);
        commandManager.RegisterCommand("listvip_command", "List all VIP groups for a player", cmd_ListVipGroups);
        commandManager.RegisterCommand("listavailable_command", "List all available VIP groups", cmd_ListAvailableGroups);
        
        commandManager.RegisterCommand("addvipsteam_command", "Add a VIP group to a player by SteamID64", cmd_AddVipGroupBySteamID);
        commandManager.RegisterCommand("removevipsteam_command", "Remove a VIP group from a player by SteamID64", cmd_RemoveVipGroupBySteamID);
        
        Console.WriteLine("[Mesharsky - VIP] Admin commands registered");
    }
    
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
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.selectgroup", targetPlayer.PlayerName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.player.info", targetPlayer.PlayerName, targetPlayer.SteamID), (_, _) => { }, disabled: true);
        
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
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private void ShowGroupExistsMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName, int currentExpiryTime)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.groupexists", targetPlayer.PlayerName, groupName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.OrangeRed,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        var expiryInfo = currentExpiryTime == 0 
            ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
            : _localizer!.ForPlayer(admin, "admin.group.expiry.until", FormatExpiryDate(admin, currentExpiryTime));
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.groupexists.info", groupName, expiryInfo), (_, _) => { }, disabled: true);
        
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
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private void ShowExtendDurationMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName, int currentExpiryTime)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.extendduration", targetPlayer.PlayerName, groupName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.extend.currentexpiry", 
            FormatExpiryDate(admin, currentExpiryTime)), (_, _) => { }, disabled: true);
        
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
        
        MenuAPI.OpenMenu(this, admin, menu);
    }

    private void ShowDurationSelectionMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.selectduration", targetPlayer.PlayerName, groupName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
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
        
        MenuAPI.OpenMenu(this, admin, menu);
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
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.added", groupName);
        
        if (expiryTime == 0) {
            ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.expiry.never");
        } else {
            ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.expiry", expiryMessageForChat);
        }
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, false, "global.divider");
        
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
        
        var oldExpiryFormatted = FormatExpiryDate(admin, currentExpiryTime, "date.format.medium");
        var newExpiryFormatted = FormatExpiryDate(admin, newExpiryTime, "date.format.medium");

        ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.extended", 
            groupName, targetPlayer.PlayerName, 
            oldExpiryFormatted, 
            newExpiryFormatted, 
            daysToAdd);
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.extended", 
            groupName, FormatExpiryDate(targetPlayer, newExpiryTime, "date.format.with_time"), daysToAdd);
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, false, "global.divider");
        
        ShowAddVipPlayerSelectionMenu(admin);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    private void cmd_RemoveVipGroup(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || !CheckCommandAccess(player))
            return;
        
        ShowRemoveVipPlayerSelectionMenu(player);
    }

    private void ShowRemoveVipPlayerSelectionMenu(CCSPlayerController admin)
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
            var cachedPlayer = GetOrCreatePlayer(selectedPlayer.SteamID, selectedPlayer.PlayerName);
            
            if (cachedPlayer.Groups.Count == 0 || !cachedPlayer.Groups.Any(g => g.Active))
            {
                ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.player.nogroups", selectedPlayer.PlayerName);
                ShowRemoveVipPlayerSelectionMenu(admin);
                return;
            }
            
            ShowRemoveGroupMenu(admin, selectedPlayer);
        });
    }

    private void ShowRemoveGroupMenu(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.removegroup", targetPlayer.PlayerName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.player.info", targetPlayer.PlayerName, targetPlayer.SteamID), (_, _) => { }, disabled: true);
        
        foreach (var group in cachedPlayer.Groups.Where(g => g.Active))
        {
            var expiryMsg = group.ExpiryTime == 0 
                ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
                : _localizer!.ForPlayer(admin, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(group.ExpiryTime).ToLocalTime());
                
            menu.AddOption($"{group.GroupName} - {expiryMsg}", (p, _) => {
                ShowRemoveConfirmationMenu(p, targetPlayer, group.GroupName);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            ShowRemoveVipPlayerSelectionMenu(p);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private void ShowRemoveConfirmationMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.confirmremove", groupName, targetPlayer.PlayerName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.OrangeRed,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.confirm.areyousure"), (_, _) => { }, disabled: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.confirm.yes"), (p, _) => {
            RemoveVipGroupFromPlayer(p, targetPlayer, groupName);
        });
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.confirm.no"), (p, _) => {
            ShowRemoveGroupMenu(p, targetPlayer);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }

    private void RemoveVipGroupFromPlayer(CCSPlayerController admin, CCSPlayerController targetPlayer, string groupName)
    {
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.notfound", groupName);
            return;
        }
        
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        
        if (!cachedPlayer.HasGroup(groupName))
        {
            ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.playerdoesnothave", targetPlayer.PlayerName, groupName);
            return;
        }
        
        RemoveGroupFromPlayer(targetPlayer.SteamID, groupName);
        
        AdminManager.RemovePlayerPermissions(targetPlayer, service.Flag);
        
        ChatHelper.PrintLocalizedChat(admin, _localizer!, true, "admin.group.removed", groupName, targetPlayer.PlayerName);
        
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, true, "admin.notify.service.removed", groupName);
        ChatHelper.PrintLocalizedChat(targetPlayer, _localizer!, false, "global.divider");
        
        ShowRemoveVipPlayerSelectionMenu(admin);
    }
    
    [CommandHelper(minArgs: 1, usage: "<target>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    private void cmd_ListVipGroups(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
        {
            cmd_ListVipGroupsConsole(player, command);
            return;
        }
        
        if (!CheckCommandAccess(player))
            return;
            
        var target = command.GetArg(1);
        
        var targetPlayers = Utilities.GetPlayers().Where(p => 
            p is { IsValid: true, IsBot: false } && 
            (p.PlayerName.Contains(target, StringComparison.OrdinalIgnoreCase) || 
             p.SteamID.ToString() == target)).ToList();
        
        switch (targetPlayers.Count)
        {
            case 0:
                ReplyToCommand(player, "admin.group.playernotfound", target);
                return;
            case > 1:
                ShowPlayerSelectionMenu(player, targetPlayers, _localizer!.ForPlayer(player, "admin.menu.title.playerselection"), (selectedPlayer) => {
                    ShowPlayerVipGroupsMenu(player, selectedPlayer);
                });
                return;
        }

        var targetPlayer = targetPlayers[0];
        ShowPlayerVipGroupsMenu(player, targetPlayer);
    }
    
    private void cmd_ListVipGroupsConsole(CCSPlayerController? player, CommandInfo command)
    {
        if (!CheckCommandAccess(player))
            return;
            
        var target = command.GetArg(1);
        
        var targetPlayers = Utilities.GetPlayers().Where(p => 
            p is { IsValid: true, IsBot: false } && 
            (p.PlayerName.Contains(target, StringComparison.OrdinalIgnoreCase) || 
             p.SteamID.ToString() == target)).ToList();
        
        switch (targetPlayers.Count)
        {
            case 0:
                ReplyToCommand(player, "admin.group.playernotfound", target);
                return;
            case > 1:
                ReplyToCommand(player, "admin.group.multipleplayers", target);
                return;
        }

        var targetPlayer = targetPlayers[0];
        
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        
        if (player != null && player.IsValid)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "admin.menu.title.listgroups", targetPlayer.PlayerName);
        }
        else
        {
            Console.WriteLine($"[Mesharsky - VIP] VIP groups for player {targetPlayer.PlayerName}:");
        }
        
        if (cachedPlayer.Groups.Count == 0)
        {
            if (player != null && player.IsValid)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "admin.menu.player.nogroups");
            }
            else
            {
                Console.WriteLine("  None");
            }
            return;
        }
        
        foreach (var group in cachedPlayer.Groups)
        {
            var expiryMsg = group.ExpiryTime == 0 
                ? _localizer!.ForPlayer(player, "admin.group.expiry.permanent") 
                : _localizer!.ForPlayer(player, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(group.ExpiryTime).ToLocalTime());
                
            var statusText = group.Active 
                ? _localizer!.ForPlayer(player, "admin.menu.group.status.active") 
                : _localizer!.ForPlayer(player, "admin.menu.group.status.inactive");
                
            if (player != null && player.IsValid)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, false, "  {0} - {1} - {2}", group.GroupName, expiryMsg, statusText);
            }
            else
            {
                Console.WriteLine($"  {group.GroupName} - {expiryMsg} - {(group.Active ? "Active" : "Inactive")}");
            }
        }
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    private void cmd_ListAvailableGroups(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            cmd_ListAvailableGroupsConsole(player);
            return;
        }
        
        if (!CheckCommandAccess(player))
            return;
        
        if (!player.IsValid)
            return;
        
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, "admin.menu.title.listavailable"), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        foreach (var group in Config!.GroupSettings)
        {
            menu.AddOption($"{group.Name} - Flag: {group.Flag}", (p, _) => {
                ShowGroupDetailsMenu(p, group, menu);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(player, "admin.menu.button.close"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, menu);
    }
    
    private void cmd_ListAvailableGroupsConsole(CCSPlayerController? player)
    {
        if (!CheckCommandAccess(player))
            return;
        
        if (player != null && player.IsValid)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "admin.menu.title.listavailable");
        }
        else
        {
            Console.WriteLine("[Mesharsky - VIP] Available VIP groups:");
        }
        
        foreach (var group in Config!.GroupSettings)
        {
            if (player != null && player.IsValid)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, false, "  {0} - Flag: {1}", group.Name, group.Flag);
            }
            else
            {
                Console.WriteLine($"  {group.Name} - Flag: {group.Flag}");
            }
        }
    }
    
    [CommandHelper(minArgs: 3, usage: "<steamid64> <group> [duration_days=0 (permanent)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    private void cmd_AddVipGroupBySteamID(CCSPlayerController? player, CommandInfo command)
    {
        if (!CheckCommandAccess(player))
            return;
            
        var steamIdInput = command.GetArg(1);
        var groupName = command.GetArg(2);
        
        if (!ulong.TryParse(steamIdInput, out var steamId) || steamId < 76561197960265728UL)
        {
            ReplyToCommand(player, "admin.group.invalid.steamid");
            return;
        }
        
        var durationDays = 0;
        if (command.ArgCount >= 4)
        {
            if (!int.TryParse(command.GetArg(3), out durationDays))
            {
                ReplyToCommand(player, "admin.group.invalid.duration");
                return;
            }
        }
        
        var expiryTime = 0;
        if (durationDays > 0)
        {
            expiryTime = (int)DateTimeOffset.UtcNow.AddDays(durationDays).ToUnixTimeSeconds();
        }
        
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ReplyToCommand(player, "admin.group.notfound", groupName);
            return;
        }
        
        var cachedPlayer = GetOrCreatePlayer(steamId, "Offline Player");
        var existingGroup = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == groupName);
        
        if (existingGroup != null && existingGroup.Active)
        {
            var currentExpiryMsg = existingGroup.ExpiryTime == 0 
                ? _localizer!.ForPlayer(player, "admin.group.expiry.permanent") 
                : _localizer!.ForPlayer(player, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(existingGroup.ExpiryTime).ToLocalTime());
                
            ReplyToCommand(player, "admin.group.alreadyexists", steamId, groupName, currentExpiryMsg);
            
            if (player != null && player.IsValid)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "admin.group.use.ingame");
            }
            
            return;
        }
        
        var onlinePlayer = Utilities.GetPlayers().FirstOrDefault(p => p is { IsValid: true, IsBot: false } && p.SteamID == steamId);
        
        AddGroupToPlayer(steamId, groupName, expiryTime);
        
        if (onlinePlayer != null)
        {
            AdminManager.AddPlayerPermissions(onlinePlayer, service.Flag);
            
            string expiryMessageForChat;
            if (expiryTime == 0) {
                expiryMessageForChat = _localizer!.ForPlayer(onlinePlayer, "commands.vip.details.neverexpires");
            } else {
                var formattedDate = DateTimeOffset.FromUnixTimeSeconds(expiryTime).ToLocalTime().ToString("dd MMMM, 'o godzinie' HH:mm");
                expiryMessageForChat = formattedDate;
            }
            
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.added", groupName);
            
            if (expiryTime == 0) {
                ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.expiry.never");
            } else {
                ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.expiry", expiryMessageForChat);
            }
            
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, false, "global.divider");
        }
        
        var expiryMsg = expiryTime == 0 
            ? _localizer!.ForPlayer(player, "admin.group.expiry.permanent") 
            : _localizer!.ForPlayer(player, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(expiryTime).ToLocalTime());

        var playerName = onlinePlayer?.PlayerName ?? "Offline Player";
        ReplyToCommand(player, "admin.group.success.added.offline", groupName, steamId, expiryMsg);
    }
    
    [CommandHelper(minArgs: 2, usage: "<steamid64> <group>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    private void cmd_RemoveVipGroupBySteamID(CCSPlayerController? player, CommandInfo command)
    {
        if (!CheckCommandAccess(player))
            return;
            
        var steamIdInput = command.GetArg(1);
        var groupName = command.GetArg(2);
        
        if (!ulong.TryParse(steamIdInput, out var steamId) || steamId < 76561197960265728UL)
        {
            ReplyToCommand(player, "admin.group.invalid.steamid");
            return;
        }
        
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ReplyToCommand(player, "admin.group.notfound", groupName);
            return;
        }
        
        var cachedPlayer = GetOrCreatePlayer(steamId, "");
        
        if (!cachedPlayer.HasGroup(groupName))
        {
            ReplyToCommand(player, "admin.group.playerdoesnothave", $"SteamID {steamId}", groupName);
            return;
        }
        
        var onlinePlayer = Utilities.GetPlayers().FirstOrDefault(p => p is { IsValid: true, IsBot: false } && p.SteamID == steamId);
        
        RemoveGroupFromPlayer(steamId, groupName);
        
        if (onlinePlayer != null)
        {
            AdminManager.RemovePlayerPermissions(onlinePlayer, service.Flag);
            
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.removed", groupName);
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, false, "global.divider");
        }

        ReplyToCommand(player, "admin.group.success.removed.offline", groupName, steamId);
    }
    
    private void ShowPlayerSelectionMenu(CCSPlayerController admin, List<CCSPlayerController> players, string title, Action<CCSPlayerController> onSelect)
    {
        var menu = new ScreenMenu(title, this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        foreach (var p in players)
        {
            menu.AddOption($"{p.PlayerName} ({p.SteamID})", (_, _) => {
                onSelect(p);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.cancel"), (sender, _) => {
            MenuAPI.CloseActiveMenu(sender);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private void ShowPlayerVipGroupsMenu(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var cachedPlayer = GetOrCreatePlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.listgroups", targetPlayer.PlayerName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.player.info", targetPlayer.PlayerName, targetPlayer.SteamID), (_, _) => { }, disabled: true);
        
        if (cachedPlayer.Groups.Count == 0)
        {
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.player.nogroups"), (_, _) => { }, disabled: true);
        }
        else
        {
            foreach (var group in cachedPlayer.Groups)
            {
                var expiryMsg = group.ExpiryTime == 0 
                    ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
                    : _localizer!.ForPlayer(admin, "admin.group.expiry.until", FormatExpiryDate(admin, group.ExpiryTime));
                
                var statusText = group.Active 
                    ? _localizer!.ForPlayer(admin, "admin.menu.group.status.active") 
                    : _localizer!.ForPlayer(admin, "admin.menu.group.status.inactive");
                
                menu.AddOption($"{group.GroupName} - {expiryMsg} - {statusText}", (p, _) => {
                    ShowGroupManagementMenu(p, targetPlayer, group);
                });
            }
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.close"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private void ShowGroupManagementMenu(CCSPlayerController admin, CCSPlayerController targetPlayer, PlayerGroup group)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.managegroup", group.GroupName, targetPlayer.PlayerName), this)
        {
            IsSubMenu = true,
            PostSelectAction = PostSelectAction.Nothing,
            TextColor = Color.Orange,
            FontName = "Verdana Bold"
        };
        
        var expiryMsg = group.ExpiryTime == 0 
            ? _localizer!.ForPlayer(admin, "admin.group.expiry.permanent") 
            : _localizer!.ForPlayer(admin, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(group.ExpiryTime).ToLocalTime());
            
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.group.info", group.GroupName, expiryMsg), (_, _) => { }, disabled: true);
        
        if (group.Active)
        {
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.remove", group.GroupName), (p, _) => {
                ShowRemoveConfirmationMenu(p, targetPlayer, group.GroupName);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
            ShowPlayerVipGroupsMenu(admin, targetPlayer);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private void ShowGroupDetailsMenu(CCSPlayerController admin, GroupSettingsConfig group, ScreenMenu parentMenu)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(admin, "admin.menu.title.groupdetails", group.Name), this)
        {
            IsSubMenu = true,
            PostSelectAction = PostSelectAction.Nothing,
            TextColor = Color.LightBlue,
            FontName = "Verdana Bold",
            ParentMenu = parentMenu
        };
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.name", group.Name), (_, _) => { }, disabled: true);
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.flag", group.Flag), (_, _) => { }, disabled: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.bonuses"), (_, _) => { }, disabled: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.health", group.PlayerHp), (_, _) => { }, disabled: true);
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.maxhealth", group.PlayerMaxHp), (_, _) => { }, disabled: true);
        
        if (group.PlayerVest)
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.armor", group.PlayerVestRound), (_, _) => { }, disabled: true);
            
        if (group.PlayerHelmet)
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.helmet", group.PlayerHelmetRound), (_, _) => { }, disabled: true);
            
        if (group.PlayerDefuser)
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.defuser"), (_, _) => { }, disabled: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.grenades"), (_, _) => { }, disabled: true);
        
        if (group.HeAmount > 0)
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.grenades.he", group.HeAmount), (_, _) => { }, disabled: true);
            
        if (group.FlashAmount > 0)
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.grenades.flash", group.FlashAmount), (_, _) => { }, disabled: true);
            
        if (group.SmokeAmount > 0)
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.grenades.smoke", group.SmokeAmount), (_, _) => { }, disabled: true);
            
        if (group.DecoyAmount > 0)
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.grenades.decoy", group.DecoyAmount), (_, _) => { }, disabled: true);
            
        if (group.MolotovAmount > 0)
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.grenades.molotov", group.MolotovAmount), (_, _) => { }, disabled: true);
            
        if (group.HealthshotAmount > 0)
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.grenades.healthshot", group.HealthshotAmount), (_, _) => { }, disabled: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.abilities"), (_, _) => { }, disabled: true);
        
        if (group.PlayerExtraJumps > 0)
        {
            var jumpType = group.PlayerExtraJumps == 1 
                ? _localizer!.ForPlayer(admin, "admin.menu.details.jump.double", group.PlayerExtraJumps + 1)
                : _localizer!.ForPlayer(admin, "admin.menu.details.jump.triple", group.PlayerExtraJumps + 1);
                
            menu.AddOption(jumpType, (_, _) => { }, disabled: true);
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.jumpheight", group.PlayerExtraJumpHeight), (_, _) => { }, disabled: true);
        }
        
        if (group.PlayerBunnyhop)
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.bhop"), (_, _) => { }, disabled: true);
            
        if (group.PlayerWeaponmenu)
            menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.details.weaponmenu"), (_, _) => { }, disabled: true);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
            MenuAPI.OpenMenu(this, p, parentMenu);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
    
    private static bool CheckCommandAccess(CCSPlayerController? player)
    {
        if (player == null)
            return true;
            
        if (!player.IsValid)
            return false;

        if (AdminManager.PlayerHasPermissions(player, "@css/root")) return true;
        
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "admin.group.nopermission");
        return false;
    }
    
    private static void ReplyToCommand(CCSPlayerController? player, string key, params object[] args)
    {
        if (player == null)
        {
            Console.WriteLine($"[Mesharsky - VIP] {key} {string.Join(" ", args)}");
        }
        else if (player.IsValid)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, key, args);
        }
    }
}