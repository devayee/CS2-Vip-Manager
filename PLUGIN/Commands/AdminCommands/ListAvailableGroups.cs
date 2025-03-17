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
    
    private static void cmd_ListAvailableGroupsConsole(CCSPlayerController? player)
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
        
        menu.AddOption(_localizer!.ForPlayer(admin, "benefits.header"), (_, _) => { }, disabled: true);
        
        var service = new Service
        {
            Name = group.Name,
            Flag = group.Flag,
            PlayerHp = group.PlayerHp,
            PlayerMaxHp = group.PlayerMaxHp,
            PlayerVest = group.PlayerVest,
            PlayerVestRound = group.PlayerVestRound,
            PlayerHelmet = group.PlayerHelmet,
            PlayerHelmetRound = group.PlayerHelmetRound,
            PlayerDefuser = group.PlayerDefuser,
            HeAmount = group.HeAmount,
            FlashAmount = group.FlashAmount,
            SmokeAmount = group.SmokeAmount,
            DecoyAmount = group.DecoyAmount,
            MolotovAmount = group.MolotovAmount,
            HealthshotAmount = group.HealthshotAmount,
            PlayerExtraJumps = group.PlayerExtraJumps,
            PlayerExtraJumpHeight = group.PlayerExtraJumpHeight,
            PlayerBunnyhop = group.PlayerBunnyhop,
            SmokeColorEnabled = group.SmokeColor.Enabled,
            SmokeColorRandom = group.SmokeColor.Random,
            SmokeColorR = group.SmokeColor.Red,
            SmokeColorG = group.SmokeColor.Green,
            SmokeColorB = group.SmokeColor.Blue,
            InfiniteAmmo = group.InfiniteAmmo,
            FastReload = group.FastReload,
            KillScreen = group.KillScreen,
            WeaponMenu = group.WeaponMenu
        };
        
        BenefitsRenderer.RenderServiceBenefits(menu, admin, service);
        
        menu.AddOption(_localizer!.ForPlayer(admin, "admin.menu.button.back"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
            MenuAPI.OpenMenu(this, p, parentMenu);
        });
        
        MenuAPI.OpenMenu(this, admin, menu);
    }
}