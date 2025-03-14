using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ShowOnlineVips(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
            
        if (!Config!.PluginSettings.OnlineList)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.online.disabled");
            return;
        }
        
        var vipPlayers = Utilities.GetPlayers()
            .Where(p => p is { IsValid: true, IsBot: false } && PlayerCache.TryGetValue(p.SteamID, out var cached) && cached.Groups.Any(g => g.Active))
            .ToList();
            
        if (vipPlayers.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.online.none");
            return;
        }
        
        CreateOnlineVipsMenu(player, vipPlayers);
    }

    private void CreateOnlineVipsMenu(CCSPlayerController player, List<CCSPlayerController> vipPlayers)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.online.title", vipPlayers.Count), this)
        {
            PostSelectAction = CS2ScreenMenuAPI.Enums.PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.online.header", vipPlayers.Count), (_, _) => { }, disabled: true);
        
        foreach (var vipPlayer in vipPlayers)
        {
            if (!PlayerCache.TryGetValue(vipPlayer.SteamID, out var cached)) 
                continue;
                
            var groups = cached.Groups.Where(g => g.Active)
                .Select(g => ServiceManager.GetService(g.GroupName)?.Name ?? g.GroupName)
                .ToList();
                
            var groupsText = string.Join(", ", groups);
            
            menu.AddOption($"{vipPlayer.PlayerName} - {groupsText}", (p, _) => {
                ShowVipPlayerDetails(p, vipPlayer, menu);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.close"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, menu);
    }

    private void ShowVipPlayerDetails(CCSPlayerController viewer, CCSPlayerController vipPlayer, ScreenMenu parentMenu)
    {
        if (!PlayerCache.TryGetValue(vipPlayer.SteamID, out var cached))
            return;
        
        var detailsMenu = new ScreenMenu(_localizer!.ForPlayer(viewer, "commands.online.player.details", vipPlayer.PlayerName), this)
        {
            IsSubMenu = true,
            PostSelectAction = CS2ScreenMenuAPI.Enums.PostSelectAction.Nothing,
            TextColor = Color.LightGreen,
            FontName = "Verdana Bold",
            ParentMenu = parentMenu
        };
        
        detailsMenu.AddOption(_localizer!.ForPlayer(viewer, "commands.online.player.name", vipPlayer.PlayerName), (_, _) => { }, disabled: true);
        
        var groupsWithExpiry = cached.Groups.Where(g => g.Active).Select(group => (group.GroupName, group.ExpiryTime)).ToList();
        
        foreach (var (name, expiryTime) in groupsWithExpiry)
        {
            var expiryText = expiryTime == 0 
                ? _localizer!.ForPlayer(viewer, "commands.vip.details.neverexpires") 
                : _localizer!.ForPlayer(viewer, "commands.vip.details.expires", 
                    FormatExpiryDate(viewer, expiryTime));
                    
            detailsMenu.AddOption($"{name} - {expiryText}", (_, _) => { }, disabled: true);
        }
        
        detailsMenu.AddOption(_localizer!.ForPlayer(viewer, "commands.online.back"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
            MenuAPI.OpenMenu(this, p, parentMenu);
        });
        
        MenuAPI.OpenSubMenu(this, viewer, detailsMenu);
    }
}