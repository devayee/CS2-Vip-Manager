using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_VipTest(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
        
        if (!Config!.VipTest.Enabled)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.viptest.disabled");
            return;
        }
        
        var steamId = player.SteamID;
        var playerName = player.PlayerName;
        
        var cachedPlayer = GetOrCreatePlayer(steamId, playerName);
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        
        var nonTestVipGroups = activeGroups
            .Where(g => g.GroupName != Config.VipTest.TestGroup)
            .ToList();
        
        if (nonTestVipGroups.Count > 0)
        {
            var groupNames = string.Join(", ", nonTestVipGroups.Select(g => g.GroupName));
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.viptest.info.purchased", groupNames, Config.VipTest.TestGroup, Config.VipTest.TestDuration);
            
            return;
        }
        
        var testGroup = activeGroups.FirstOrDefault(g => g.GroupName == Config.VipTest.TestGroup);
        if (testGroup is { ExpiryTime: > 0 })
        {
            var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeRemaining = testGroup.ExpiryTime - currentTime;
            
            if (timeRemaining > 0)
            {
                var days = (int)Math.Ceiling(timeRemaining / 86400.0);
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.viptest.already.active", days);
                return;
            }
        }
        
        if (HasUsedVipTest(steamId))
        {
            if (Config.VipTest.TestCooldown == 0)
            {
                ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.viptest.already.used");
            }
            else
            {
                var remainingDays = GetRemainingCooldownDays(steamId);
                if (remainingDays > 0)
                {
                    ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.viptest.cooldown", remainingDays);
                }
                else
                {
                    GiveVipTest(player);
                }
            }
            return;
        }
        
        GiveVipTest(player);
    }
    
    private void GiveVipTest(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        var playerName = player.PlayerName;
        var testGroup = Config!.VipTest.TestGroup;
        var testDuration = Config.VipTest.TestDuration;

        // Calculate expiry time
        var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiryTime = currentTime + (testDuration * 86400);

        // Add the group to the player
        AddGroupToPlayer(steamId, testGroup, expiryTime);

        // Record the usage
        RecordVipTestUsage(steamId, playerName);
        
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.viptest.success", testGroup, testDuration);
        
        var service = ServiceManager.GetService(testGroup);
        if (service == null) return;
    
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.title", playerName);
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.services", testGroup);
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.welcome.expiry.date", 
            FormatExpiryDate(player, expiryTime),
            FormatRemainingDays(player, expiryTime));
    }
}