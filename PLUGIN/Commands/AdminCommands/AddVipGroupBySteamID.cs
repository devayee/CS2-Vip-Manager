using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(minArgs: 3, usage: "<steamid64> <group> [duration_days=0 (permanent)]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    private void cmd_AddVipGroupBySteamID(CCSPlayerController? player, CommandInfo command)
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
        
        var durationDays = 0;
        if (command.ArgCount >= 4)
        {
            if (!int.TryParse(command.GetArg(3), out durationDays))
            {
                ReplyToCommand(player, "admin.group.invalid.duration");
                return;
            }
        }
        
        var expiryTime = 0;
        if (durationDays > 0)
        {
            expiryTime = (int)DateTimeOffset.UtcNow.AddDays(durationDays).ToUnixTimeSeconds();
        }
        
        var service = ServiceManager.GetService(groupName);
        if (service == null)
        {
            ReplyToCommand(player, "admin.group.notfound", groupName);
            return;
        }
        
        var cachedPlayer = GetOrCreatePlayer(steamId, "Offline Player");
        var existingGroup = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == groupName);
        
        if (existingGroup != null && existingGroup.Active)
        {
            var currentExpiryMsg = existingGroup.ExpiryTime == 0 
                ? _localizer!.ForPlayer(player, "admin.group.expiry.permanent") 
                : _localizer!.ForPlayer(player, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(existingGroup.ExpiryTime).ToLocalTime());
                
            ReplyToCommand(player, "admin.group.alreadyexists", steamId, groupName, currentExpiryMsg);
            
            if (player != null && player.IsValid)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "admin.group.use.ingame");
            }
            
            return;
        }
        
        var onlinePlayer = Utilities.GetPlayers().FirstOrDefault(p => p is { IsValid: true, IsBot: false } && p.SteamID == steamId);
        
        AddGroupToPlayer(steamId, groupName, expiryTime);
        
        if (onlinePlayer != null)
        {
            AdminManager.AddPlayerPermissions(onlinePlayer, service.Flag);
            
            string expiryMessageForChat;
            if (expiryTime == 0) {
                expiryMessageForChat = _localizer!.ForPlayer(onlinePlayer, "commands.vip.details.neverexpires");
            } else {
                var formattedDate = DateTimeOffset.FromUnixTimeSeconds(expiryTime).ToLocalTime().ToString("dd MMMM, 'o godzinie' HH:mm");
                expiryMessageForChat = formattedDate;
            }
            
            ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.added", groupName);
            
            if (expiryTime == 0) {
                ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.expiry.never");
            } else {
                ChatHelper.PrintLocalizedChat(onlinePlayer, _localizer!, true, "admin.notify.service.expiry", expiryMessageForChat);
            }
        }
        
        var expiryMsg = expiryTime == 0 
            ? _localizer!.ForPlayer(player, "admin.group.expiry.permanent") 
            : _localizer!.ForPlayer(player, "admin.group.expiry.until", DateTimeOffset.FromUnixTimeSeconds(expiryTime).ToLocalTime());

        var playerName = onlinePlayer?.PlayerName ?? "Offline Player";
        ReplyToCommand(player, "admin.group.success.added.offline", groupName, steamId, expiryMsg);
    }
}