using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;

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
            .Where(p => p is { IsValid: true, IsBot: false } && 
                (PlayerCache.TryGetValue(p.SteamID, out var cached) && cached.Groups.Any(g => g.Active) ||
                 CheckExternalPermissions(p).Count > 0))
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
        var manager = GetMenuManager();
        if (manager == null)
            return;
            
        var menu = manager.CreateMenu(_localizer!.ForPlayer(player, "commands.online.title", vipPlayers.Count), isSubMenu: false);
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.online.header", vipPlayers.Count), (_, _) => { });
        
        foreach (var vipPlayer in vipPlayers)
        {
            var groups = new List<string>();
            
            if (PlayerCache.TryGetValue(vipPlayer.SteamID, out var cached))
            {
                groups.AddRange(cached.Groups.Where(g => g.Active)
                    .Select(g => ServiceManager.GetService(g.GroupName)?.Name ?? g.GroupName));
            }
            
            if (groups.Count == 0)
            {
                var externalServices = CheckExternalPermissions(vipPlayer);
                groups.AddRange(externalServices.Select(s => s.Name));
            }
            
            var groupsText = groups.Count > 0 ? string.Join(", ", groups) : "External VIP";
            
            menu.AddOption($"{vipPlayer.PlayerName} - {groupsText}", (p, _) => {
                ShowVipPlayerDetails(p, vipPlayer, menu);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.close"), (p, _) => {
            manager.CloseMenu(player);
        });
        
        manager.OpenMainMenu(player, menu);
    }

    private void ShowVipPlayerDetails(CCSPlayerController viewer, CCSPlayerController vipPlayer, IT3Menu parentMenu)
    {
        var manager = GetMenuManager();
        if (manager == null)
            return;
        
        var detailsMenu = manager.CreateMenu(_localizer!.ForPlayer(viewer, "commands.online.player.details", vipPlayer.PlayerName), isSubMenu: true);
        
        detailsMenu.AddOption(_localizer!.ForPlayer(viewer, "commands.online.player.name", vipPlayer.PlayerName), (_, _) => { });
        
        var hasDetails = false;
        
        if (PlayerCache.TryGetValue(vipPlayer.SteamID, out var cached))
        {
            var groupsWithExpiry = cached.Groups.Where(g => g.Active).Select(group => (group.GroupName, group.ExpiryTime)).ToList();
            
            foreach (var (name, expiryTime) in groupsWithExpiry)
            {
                var expiryText = expiryTime == 0 
                    ? _localizer!.ForPlayer(viewer, "commands.vip.details.neverexpires") 
                    : _localizer!.ForPlayer(viewer, "commands.vip.details.expires", 
                        FormatExpiryDate(viewer, expiryTime));
                        
                detailsMenu.AddOption($"{name} - {expiryText}", (_, _) => { });
                hasDetails = true;
            }
        }
        
        if (!hasDetails)
        {
            var externalServices = CheckExternalPermissions(vipPlayer);
            foreach (var service in externalServices)
            {
                var expiryText = _localizer!.ForPlayer(viewer, "commands.vip.details.external");
                detailsMenu.AddOption($"{service.Name} - {expiryText}", (_, _) => { });
            }
        }
        
        manager.OpenSubMenu(viewer, detailsMenu);
    }
}
