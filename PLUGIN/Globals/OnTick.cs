using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public void Initialize_OnTick()
    {
        RegisterListener<Listeners.OnTick>(PlayerOnTick);
    }

    private static void PlayerOnTick()
    {
        foreach (var player in Utilities.GetPlayers()
            .Where(player => player is { IsValid: true, IsBot: false, PawnIsAlive: true }))
        {
            if (IsWarmup())
                return;

            if (!IsValidPlayer(player))
                return;

            if (!player.PawnIsAlive)
                return;

            var steamId = player.SteamID;

            if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer))
                return;

            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
            
            if (activeGroups.Count == 0)
                continue;
                
            var activeServices = activeGroups
                .Select(g => ServiceManager.GetService(g.GroupName))
                .Where(s => s != null)
                .ToList();
                    
            if (activeServices.Count == 0)
                continue;
                    
            var bestJumpService = activeServices
                .OrderByDescending(s => s!.PlayerExtraJumps)
                .ThenByDescending(s => s!.PlayerExtraJumpHeight)
                .First();
                        
            PlayerOnTick_MultiJump(player, cachedPlayer, bestJumpService!);
            PlayerOnTick_AutoBunnyHop(player, cachedPlayer);
        }
    }

    private static void PlayerOnTick_MultiJump(CCSPlayerController player, Player cachedPlayer, Service service)
    {
        if (service.PlayerExtraJumps == 0)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return;

        var flags = (PlayerFlags)playerPawn.Flags;
        var buttons = player.Buttons;

        var lastFlags = cachedPlayer.BonusSettings.LastFlags;
        var lastButtons = cachedPlayer.BonusSettings.LastButtons;
        var maxJumps = service.PlayerExtraJumps;
        var jumpHeight = service.PlayerExtraJumpHeight;

        if ((lastFlags & PlayerFlags.FL_ONGROUND) != 0 && (flags & PlayerFlags.FL_ONGROUND) == 0 && (lastButtons & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0)
        {
            cachedPlayer.BonusSettings.JumpsUsed++;
            cachedPlayer.BonusSettings.UsingExtraJump = false;
        }
        else if ((flags & PlayerFlags.FL_ONGROUND) != 0)
        {
            cachedPlayer.BonusSettings.JumpsUsed = 0;
            cachedPlayer.BonusSettings.UsingExtraJump = false;
        }
        else if ((lastButtons & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0 && cachedPlayer.BonusSettings.JumpsUsed <= maxJumps)
        {
            cachedPlayer.BonusSettings.JumpsUsed++;

            playerPawn.AbsVelocity.Z = (float)jumpHeight;
            cachedPlayer.BonusSettings.UsingExtraJump = true;
        }

        cachedPlayer.BonusSettings.LastFlags = flags;
        cachedPlayer.BonusSettings.LastButtons = buttons;
    }

    private static void PlayerOnTick_AutoBunnyHop(CCSPlayerController player, Player cachedPlayer)
    {
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        
        if (activeGroups.Count == 0)
            return;
            
        var hasBhopEnabled = activeGroups
            .Select(g => ServiceManager.GetService(g.GroupName))
            .Where(s => s != null)
            .Any(s => s!.PlayerBunnyhop);
            
        if (!hasBhopEnabled)
            return;
            
        var playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
            return;

        var flags = (PlayerFlags)playerPawn.Flags;
        var buttons = player.Buttons;

        if ((buttons & PlayerButtons.Jump) != 0 && (flags & PlayerFlags.FL_ONGROUND) != 0 && (playerPawn.MoveType & MoveType_t.MOVETYPE_LADDER) == 0)
        {
            playerPawn.AbsVelocity.Z = 300;
        }
    }
}