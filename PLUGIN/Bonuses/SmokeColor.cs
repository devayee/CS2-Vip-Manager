using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private void InitializeSmokeColor()
    {
        RegisterListener<Listeners.OnEntitySpawned>((entity) =>
        {
            if (entity.DesignerName != "smokegrenade_projectile") return;

            var smokeGrenade = new CSmokeGrenadeProjectile(entity.Handle);
            if (smokeGrenade.Handle == IntPtr.Zero) return;
            
            Server.NextFrame(() =>
            {
                var thrower = smokeGrenade.Thrower.Value;
                if (thrower == null) return;
                
                var throwerController = thrower.Controller.Value;
                if (throwerController == null) return;
                
                var controller = new CCSPlayerController(throwerController.Handle);
                if (!controller.IsValid || controller.IsBot) return;
                
                if (!PlayerCache.TryGetValue(controller.SteamID, out var cachedPlayer) || !cachedPlayer.Active)
                    return;
                
                var activeServices = cachedPlayer.Groups
                    .Where(g => g.Active)
                    .Select(g => ServiceManager.GetService(g.GroupName))
                    .Where(s => s is { SmokeColorEnabled: true })
                    .ToList();

                if (activeServices.Count == 0)
                    return;
                
                var service = activeServices.First();
                
                if (service!.SmokeColorRandom)
                {
                    smokeGrenade.SmokeColor.X = Random.Shared.NextSingle() * 255.0f;
                    smokeGrenade.SmokeColor.Y = Random.Shared.NextSingle() * 255.0f;
                    smokeGrenade.SmokeColor.Z = Random.Shared.NextSingle() * 255.0f;
                }
                else
                {
                    smokeGrenade.SmokeColor.X = service.SmokeColorR;
                    smokeGrenade.SmokeColor.Y = service.SmokeColorG;
                    smokeGrenade.SmokeColor.Z = service.SmokeColorB;
                }
            });
        });
    }
}