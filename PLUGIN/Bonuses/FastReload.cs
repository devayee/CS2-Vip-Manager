using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private void RegisterFastReloadEvents()
    {
        RegisterEventHandler<EventWeaponReload>(OnWeaponReloadFastReload);
    }
    
    private HookResult OnWeaponReloadFastReload(EventWeaponReload @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        ApplyFastReload(player);
        return HookResult.Continue;
    }
    
    private static void ApplyFastReload(CCSPlayerController player)
    {
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer)) return;
        if (!cachedPlayer.Active) return;
        
        var hasFastReload = PlayerHasFeature(player, service => service.FastReload);
        if (!hasFastReload) return;

        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null) return;

        var weaponBase = activeWeapon.As<CCSWeaponBase>();

        var weaponData = weaponBase.VData;
        if (weaponData == null) return;

        if (activeWeapon.Clip1 >= weaponData.MaxClip1) return;
    
        activeWeapon.Clip1 = weaponData.MaxClip1;
        Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
    }
}