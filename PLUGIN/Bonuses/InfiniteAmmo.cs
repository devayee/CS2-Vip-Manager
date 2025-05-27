using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private void RegisterInfiniteAmmoEvents()
    {
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        RegisterEventHandler<EventWeaponReload>(OnWeaponReloadInfiniteAmmo);
    }
    
    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        ApplyInfiniteAmmo(player);
        return HookResult.Continue;
    }

    private HookResult OnWeaponReloadInfiniteAmmo(EventWeaponReload @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        ApplyInfiniteAmmo(player);
        return HookResult.Continue;
    }
    
    private static void ApplyInfiniteAmmo(CCSPlayerController player)
    {
        var hasInfiniteAmmo = PlayerHasFeature(player, service => service.InfiniteAmmo);
        if (!hasInfiniteAmmo) return;
        
        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon?.Value;
        if (activeWeapon == null) return;
        
        ApplyInfiniteClip(player);
        ApplyInfiniteReserve(player);
    }

    private static void ApplyInfiniteClip(CCSPlayerController player)
    {
        var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
        if (activeWeaponHandle?.Value == null) return;
        activeWeaponHandle.Value.Clip1 = 100;
        Utilities.SetStateChanged(activeWeaponHandle.Value, "CBasePlayerWeapon", "m_iClip1");
    }

    private static void ApplyInfiniteReserve(CCSPlayerController player)
    {
        var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
        if (activeWeaponHandle?.Value == null) return;
        activeWeaponHandle.Value.ReserveAmmo[0] = 100;
        Utilities.SetStateChanged(activeWeaponHandle.Value, "CBasePlayerWeapon", "m_iReserveAmmo");
    }
}
