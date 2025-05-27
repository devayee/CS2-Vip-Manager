using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private void RegisterPlayerDeathEvent()
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }
    
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        
        if (attacker == null || !attacker.IsValid || attacker.IsBot || attacker == victim)
            return HookResult.Continue;

        ApplyKillScreen(attacker);
        ApplyHealthBonus(attacker, @event);
        return HookResult.Continue;
    }
    
    private static void ApplyKillScreen(CCSPlayerController attacker)
    {
        var hasKillScreen = PlayerHasFeature(attacker, service => service.KillScreen);
        if (!hasKillScreen) return;
    
        var attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null) return;
        
        attackerPawn.HealthShotBoostExpirationTime = Server.CurrentTime + 1.0f;
        Utilities.SetStateChanged(attackerPawn, "CCSPlayerPawn", "m_flHealthShotBoostExpirationTime");
    }
    
    private static void ApplyHealthBonus(CCSPlayerController attacker, EventPlayerDeath deathEvent)
    {
        var attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null || !attacker.PawnIsAlive)
            return;

        // Get all services with health bonuses
        var services = new List<Service>();
        
        // First check cached player data
        if (PlayerCache.TryGetValue(attacker.SteamID, out var cachedPlayer) && cachedPlayer.Active)
        {
            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
            services.AddRange(activeGroups.Select(group => ServiceManager.GetService(group.GroupName)).OfType<Service>());
        }
        
        // Fallback: Check external permissions
        if (services.Count == 0)
        {
            var externalServices = CheckExternalPermissions(attacker);
            services.AddRange(externalServices);
        }
        
        if (services.Count == 0)
            return;

        int healthToGive = 0;
        string bonusType = "";
        
        if (IsKnifeWeapon(deathEvent.Weapon))
        {
            var knifeHealth = services.Max(s => s.HealthPerKnifeKill);
            if (knifeHealth > 0)
            {
                healthToGive = knifeHealth;
                bonusType = "knife";
            }
        }
        else if (deathEvent.Headshot)
        {
            var headshotHealth = services.Max(s => s.HealthPerHeadshot);
            if (headshotHealth > 0)
            {
                healthToGive = headshotHealth;
                bonusType = "headshot";
            }
        }
        else if (deathEvent.Noscope)
        {
            var noscopeHealth = services.Max(s => s.HealthPerNoScope);
            if (noscopeHealth > 0)
            {
                healthToGive = noscopeHealth;
                bonusType = "noscope";
            }
        }
        
        if (healthToGive == 0)
        {
            var killHealth = services.Max(s => s.HealthPerKill);
            if (killHealth > 0)
            {
                healthToGive = killHealth;
                bonusType = "kill";
            }
        }
        
        if (healthToGive > 0)
        {
            GiveHealthBonus(attacker, attackerPawn, healthToGive, bonusType);
        }
    }
    
    private static void GiveHealthBonus(CCSPlayerController player, CCSPlayerPawn playerPawn, int healthAmount, string bonusType)
    {
        var currentHealth = playerPawn.Health;
        var maxHealth = playerPawn.MaxHealth;
        var newHealth = Math.Min(currentHealth + healthAmount, maxHealth);
        
        if (newHealth > currentHealth)
        {
            playerPawn.Health = newHealth;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
            
            var actualHealthGained = newHealth - currentHealth;
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, $"vip.healthbonus.{bonusType}", actualHealthGained);
        }
    }
    
    private static bool IsKnifeWeapon(string weapon)
    {
        return weapon.Contains("knife") || weapon.Contains("bayonet");
    }
}
