using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [RequiresPermissions("@css/root")]
    private void cmd_ReloadVips(CCSPlayerController? commandSender, CommandInfo command)
    {

        Console.WriteLine("[Mesharsky - VIP] Reloading vips for all players");

        var players = Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false }).ToList();

        if (players.Count == 0)
        {
            Console.WriteLine("[Mesharsky - VIP] No players online during reload");
            return;
        }

        Console.WriteLine($"[Mesharsky - VIP] Found {players.Count} players to reload vips");

        foreach (var player in players)
        {
            if (!player.IsValid)
                continue;

            var steamId = player.SteamID;
            var playerName = player.PlayerName;

            if (!_databaseLoaded)
            {
                LogDatabaseNotLoaded(player);
                continue;
            }

            ResetPlayerCache(steamId);
            var cachedPlayer = GetOrCreatePlayer(steamId, playerName);

            ReassignPlayerPermissions(player, cachedPlayer);
        }


    }

}