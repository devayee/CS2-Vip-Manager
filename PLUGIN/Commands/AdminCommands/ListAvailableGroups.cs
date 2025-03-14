using System.Drawing;
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
        
        if (group.SmokeColor.Enabled)
        {
            menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.menu.smokecolor"), (_, _) => { }, disabled: true);
        
            if (group.SmokeColor.Random)
            {
                menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.smokecolor.random", group.Name), (_, _) => { }, disabled: true);
            }
            else
            {
                menu.AddOption(_localizer!.ForPlayer(admin, "commands.benefits.smokecolor.custom", 
                    group.Name, group.SmokeColor.Red, group.SmokeColor.Green, group.SmokeColor.Blue), (_, _) => { }, disabled: true);
            }
        }
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
            MenuAPI.OpenMenu(this, p, parentMenu);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
}