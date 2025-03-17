using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;
using PostSelectAction = CounterStrikeSharp.API.Modules.Menu.PostSelectAction;

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
        
        if (activeGroups.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.noactive");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.purchase");
            return;
        }
        
        var activeServices = activeGroups
            .Select(g => ServiceManager.GetService(g.GroupName))
            .Where(s => s != null)
            .Cast<Service>()
            .ToList();
            
        if (activeServices.Count == 0)
            return;
        
        var bestHp = activeServices.Max(s => s.PlayerHp);
        var hasArmor = activeServices.Any(s => s.PlayerVest);
        var hasHelmet = activeServices.Any(s => s.PlayerHelmet);
        var hasDefuser = activeServices.Any(s => s.PlayerDefuser);
        
        var mainMenu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.benefits.menu.title"), this)
        {
            PostSelectAction = (CS2ScreenMenuAPI.Enums.PostSelectAction)PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        var groupNames = string.Join(", ", activeGroups.Select(g => g.GroupName));
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.activeservices", groupNames), (_, _) => { }, disabled: true);
        
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
            var sourceMenu = new ScreenMenu(_localizer!.ForPlayer(player, "benefits.source.title"), this)
            {
                IsSubMenu = true,
                PostSelectAction = (CS2ScreenMenuAPI.Enums.PostSelectAction)PostSelectAction.Nothing,
                TextColor = Color.LightBlue,
                ParentMenu = mainMenu,
                FontName = "Verdana Bold"
            };
            
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
            
            MenuAPI.OpenSubMenu(this, p, sourceMenu);
        });
        
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.close"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, mainMenu);
    }
}