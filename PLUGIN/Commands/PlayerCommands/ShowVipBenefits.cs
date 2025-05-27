using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ShowVipBenefits(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
        
        var cachedPlayer = GetOrCreatePlayer(player.SteamID, player.PlayerName);
        
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        var activeServices = new List<Service>();
        var isExternalVip = false;
        
        // First check cached groups
        if (activeGroups.Count > 0)
        {
            activeServices.AddRange(activeGroups
                .Select(g => ServiceManager.GetService(g.GroupName))
                .Where(s => s != null)
                .Cast<Service>());
        }
        
        if (activeServices.Count == 0)
        {
            var externalServices = CheckExternalPermissions(player);
            if (externalServices.Count > 0)
            {
                activeServices.AddRange(externalServices);
                isExternalVip = true;
            }
        }
        
        if (activeServices.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.noactive");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.purchase");
            return;
        }
            
        var manager = GetMenuManager();
        if (manager == null)
            return;
        
        var bestHp = activeServices.Max(s => s.PlayerHp);
        var hasArmor = activeServices.Any(s => s.PlayerVest);
        var hasHelmet = activeServices.Any(s => s.PlayerHelmet);
        var hasDefuser = activeServices.Any(s => s.PlayerDefuser);
        
        var mainMenu = manager.CreateMenu(_localizer!.ForPlayer(player, "commands.benefits.menu.title"), isSubMenu: false);
        
        var groupNames = isExternalVip 
            ? string.Join(", ", activeServices.Select(s => s.Name))
            : string.Join(", ", activeGroups.Select(g => g.GroupName));
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.activeservices", groupNames), (_, _) => { });
        
        if (bestHp > 100)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "benefits.health", bestHp), (_, _) => { });
        }
        
        if (hasArmor)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.armor"), (_, _) => { });
        }
        
        if (hasHelmet)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.helmet"), (_, _) => { });
        }
        
        if (hasDefuser)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.defuser"), (_, _) => { });
        }
        
        BenefitsRenderer.CreateBenefitsSubmenus(this, mainMenu, player, activeServices);
        
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.sourceinfo"), (p, _) => {
            var sourceMenu = manager.CreateMenu(_localizer!.ForPlayer(player, "benefits.source.title"), isSubMenu: true);
            
            if (isExternalVip)
            {
                foreach (var service in activeServices)
                {
                    var expiryText = _localizer!.ForPlayer(player, "commands.vip.details.external");
                    sourceMenu.AddOption($"{service.Name} - {expiryText}", (_, _) => {});
                }
            }
            else
            {
                foreach (var group in activeGroups)
                {
                    var service = ServiceManager.GetService(group.GroupName);
                    if (service == null) continue;
                    
                    var expiryText = group.ExpiryTime == 0 
                        ? _localizer!.ForPlayer(player, "commands.vip.details.neverexpires") 
                        : _localizer!.ForPlayer(player, "commands.vip.details.expires", 
                            FormatExpiryDate(player, group.ExpiryTime));
                    
                    sourceMenu.AddOption($"{group.GroupName} - {expiryText}", (_, _) => {});
                }
            }
            
            manager.OpenSubMenu(p, sourceMenu);
        });
        
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.close"), (p, _) => {
            manager.CloseMenu(player);
        });
        
        manager.OpenMainMenu(player, mainMenu);
    }
}
