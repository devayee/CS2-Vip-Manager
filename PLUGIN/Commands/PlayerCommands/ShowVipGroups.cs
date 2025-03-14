using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ShowVipGroups(CCSPlayerController? player, CommandInfo command)
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
        
        var menu = new ChatMenu(_localizer!.ForPlayer(player, "commands.vip.menu.title"));
        
        foreach (var group in activeGroups)
        {
            var service = ServiceManager.GetService(group.GroupName);
            if (service == null) continue;
            
            var expiryText = group.ExpiryTime == 0 
                ? _localizer!.ForPlayer(player, "commands.vip.details.neverexpires") 
                : _localizer!.ForPlayer(player, "commands.vip.details.expires", 
                    DateTimeOffset.FromUnixTimeSeconds(group.ExpiryTime).ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                
            menu.AddMenuOption($"{group.GroupName} - {expiryText}", (player, _) => {
                ShowGroupDetails(player, group);
            });
        }
        
        menu.AddMenuOption(_localizer!.ForPlayer(player, "commands.vip.menu.extend"), (player, _) => {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.extend");
        });
        
        MenuManager.OpenChatMenu(player, menu);
    }
    
    private void ShowGroupDetails(CCSPlayerController player, PlayerGroup group)
    {
        ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.details.title", group.GroupName);
        
        var expiryText = group.ExpiryTime == 0 
            ? _localizer!.ForPlayer(player, "commands.vip.details.neverexpires") 
            : _localizer!.ForPlayer(player, "commands.vip.details.expires", 
                FormatExpiryDate(player, group.ExpiryTime, "date.format.long"));
            
        ChatHelper.PrintLocalizedChat(player, _localizer!, false, expiryText);
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.details.usebenefits");
        ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
    }
}