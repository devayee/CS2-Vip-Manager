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
}