using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private static readonly Dictionary<ulong, (List<Service> Services, DateTimeOffset LastCheck)> ExternalPermissionsCache = new();
    private const int EXTERNAL_PERMISSIONS_REFRESH_INTERVAL = 60;

    private void Initialize_OnTick()
    {
        RegisterListener<Listeners.OnTick>(PlayerOnTick);
    }

    private void PlayerOnTick()
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
            
            if (!PlayerCache.TryGetValue(steamId, out var cachedPlayer))
            {
                cachedPlayer = GetOrCreatePlayer(steamId, player.PlayerName);
            }
            
            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
            List<Service> activeServices = [];
            
            if (activeGroups.Count > 0)
            {
                activeServices.AddRange(activeGroups
                    .Select(g => ServiceManager.GetService(g.GroupName))
                    .Where(s => s != null)
                    .Cast<Service>());
            }
            
            if (activeServices.Count == 0)
            {
                var externalServices = GetCachedExternalPermissions(player);
                if (externalServices.Count > 0)
                {
                    activeServices.AddRange(externalServices);
                }
            }
            
            if (activeServices.Count == 0)
                continue;
            
            var bestJumpService = activeServices
                .OrderByDescending(s => s.PlayerExtraJumps)
                .ThenByDescending(s => s.PlayerExtraJumpHeight)
                .FirstOrDefault();
             
            if (bestJumpService != null && bestJumpService.PlayerExtraJumps > 0)
            {
                PlayerOnTick_MultiJump(player, cachedPlayer, bestJumpService);
            }
            
            PlayerOnTick_AutoBunnyHop(player, activeServices);
        }
    }
    
    private static List<Service> GetCachedExternalPermissions(CCSPlayerController player)
    {
        var steamId = player.SteamID;
        var now = DateTimeOffset.UtcNow;
        
        if (ExternalPermissionsCache.TryGetValue(steamId, out var cachedResult))
        {
            var (services, lastCheck) = cachedResult;
            
            if ((now - lastCheck).TotalSeconds < EXTERNAL_PERMISSIONS_REFRESH_INTERVAL)
            {
                return services;
            }
        }
        
        var externalServices = CheckExternalPermissions(player);
        
        ExternalPermissionsCache[steamId] = (externalServices, now);
        
        return externalServices;
    }

    private static void PlayerOnTick_MultiJump(CCSPlayerController player, Player cachedPlayer, Service service)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return;

        var flags = (PlayerFlags)playerPawn.Flags;
        var buttons = player.Buttons;

        var lastFlags = cachedPlayer.BonusSettings.LastFlags;
        var lastButtons = cachedPlayer.BonusSettings.LastButtons;
        var maxJumps = service.PlayerExtraJumps;
        var jumpHeight = service.PlayerExtraJumpHeight;
        
        if ((lastFlags & PlayerFlags.FL_ONGROUND) != 0 && (flags & PlayerFlags.FL_ONGROUND) == 0 && 
            (lastButtons & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0)
        {
            cachedPlayer.BonusSettings.JumpsUsed = 1;
            cachedPlayer.BonusSettings.UsingExtraJump = false;
        }
        else if ((flags & PlayerFlags.FL_ONGROUND) != 0)
        {
            cachedPlayer.BonusSettings.JumpsUsed = 0;
            cachedPlayer.BonusSettings.UsingExtraJump = false;
        }
        else if ((lastButtons & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0 && 
                 cachedPlayer.BonusSettings.JumpsUsed > 0 && cachedPlayer.BonusSettings.JumpsUsed < maxJumps + 1)
        {
            cachedPlayer.BonusSettings.JumpsUsed++;
            
            playerPawn.AbsVelocity.Z = (float)jumpHeight;
            cachedPlayer.BonusSettings.UsingExtraJump = true;
        }

        cachedPlayer.BonusSettings.LastFlags = flags;
        cachedPlayer.BonusSettings.LastButtons = buttons;
    }

    private static void PlayerOnTick_AutoBunnyHop(CCSPlayerController player, List<Service> activeServices)
    {
        var hasBhopEnabled = activeServices.Any(s => s.PlayerBunnyhop);
            
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