using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
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
            
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.removed", groupName);
        }

        ReplyToCommand(player, "admin.group.success.removed.offline", groupName, steamId);
    }
}