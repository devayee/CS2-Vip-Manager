using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private void RegisterKillScreenEvents()
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }
    
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid) return HookResult.Continue;
        
        if (@event.Userid != null && attacker.SteamID == @event.Userid.SteamID) 
            return HookResult.Continue;
        
        ApplyKillScreen(attacker);
        return HookResult.Continue;
    }
    
    private void ApplyKillScreen(CCSPlayerController attacker)
    {
        if (!PlayerCache.TryGetValue(attacker.SteamID, out var cachedPlayer)) return;
        if (!cachedPlayer.Active) return;
        
        var hasKillScreen = PlayerHasFeature(attacker, service => service.KillScreen);
        if (!hasKillScreen) return;
    
        var attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null) return;
        
        attackerPawn.HealthShotBoostExpirationTime = Server.CurrentTime + 1.0f;
        Utilities.SetStateChanged(attackerPawn, "CCSPlayerPawn", "m_flHealthShotBoostExpirationTime");
    }
}