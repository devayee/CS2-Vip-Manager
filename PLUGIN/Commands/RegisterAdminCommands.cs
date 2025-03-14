using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
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