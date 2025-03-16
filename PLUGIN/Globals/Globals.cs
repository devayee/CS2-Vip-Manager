using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public static bool IsPistolRound()
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
        if (gameRules == null)
        {
            return false;
        }

        var halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
        var maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();
        var roundsPlayed = gameRules.TotalRoundsPlayed;

        return roundsPlayed == 0 || (halftime && maxrounds / 2 == roundsPlayed) || gameRules.GameRestart;
    }
    
    public static int GetEffectiveRoundNumber()
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        var halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
        var maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();
        var totalRounds = gameRules.TotalRoundsPlayed;
        
        if (halftime && totalRounds > maxrounds / 2)
        {
            return totalRounds - (maxrounds / 2);
        }
        
        return totalRounds + 1;
    }

    private static bool IsWarmup()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!
            .WarmupPeriod;
    }

    private static int GetRoundNumber()
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        
        var rounds = gameRules.TotalRoundsPlayed;
        
        return rounds;
    }

    private static bool HasWeapon(CCSPlayerController player, string weaponName)
    {
        if (!player.IsValid || !player.PawnIsAlive)
            return false;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || pawn.WeaponServices == null)
            return false;

        return pawn.WeaponServices.MyWeapons.Any(weapon => weapon?.Value?.IsValid == true && weapon.Value.DesignerName?.Contains(weaponName) == true);
    }

    private static bool HasAnyPermission(CCSPlayerController player, string permissions)
    {
        return AdminManager.PlayerHasPermissions(player, permissions);
    }

    private static bool IsNightVipTime()
    {
        if (!Config!.NightVip.Enabled)
            return false;
    
        var currentTime = DateTimeOffset.Now.Hour;
        var startTime = Config.NightVip.StartHour;
        var endTime = Config.NightVip.EndHour;

        if (startTime < endTime)
        {
            return currentTime >= startTime && currentTime < endTime;
        }

        return currentTime >= startTime || currentTime < endTime;
    }
    
    private static void AssignNightVip(CCSPlayerController player)
    {
        var cachedPlayer = GetOrCreatePlayer(player.SteamID, player.PlayerName);
        
        // Check if player already has any active group
        if (cachedPlayer.Groups.Any(g => g.Active))
        {
            return;
        }
        
        // Find the service to inherit from
        var inheritGroup = Config!.NightVip.InheritGroup;
        var service = ServiceManager.GetService(inheritGroup);
        if (service == null)
        {
            Console.WriteLine($"[Mesharsky - VIP] ERROR: Night VIP inheritance group '{inheritGroup}' not found");
            return;
        }
        
        // Create a temporary night VIP group
        var nightVipGroup = new PlayerGroup
        {
            GroupName = inheritGroup,
            ExpiryTime = 0,
            Active = true,
            PlayerName = player.PlayerName,
        };
        
        cachedPlayer.Groups.Add(nightVipGroup);
        
        cachedPlayer.LoadedGroup = inheritGroup;
        cachedPlayer.Active = true;
        
        PlayerCache[player.SteamID] = cachedPlayer;

        AdminManager.AddPlayerPermissions(player, Config.NightVip.Flag);
        
        Console.WriteLine($"[Mesharsky - VIP] Assigned Night VIP (inheriting from {inheritGroup}) to player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ]");
    }

    private static void RemoveNightVip(CCSPlayerController player)
    {
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer)) 
            return;
        
        var inheritGroup = Config!.NightVip.InheritGroup;
        
        var nightVipGroup = cachedPlayer.Groups.FirstOrDefault(g => g.GroupName == inheritGroup && g.ExpiryTime == 0);
        
        if (nightVipGroup == null) 
            return;
        
        var hasOtherActiveGroups = cachedPlayer.Groups
            .Any(g => g != nightVipGroup && g.Active);
        
        bool hasExternalPermissions;
        if (ExternalPermissionsCache.TryGetValue(player.SteamID, out var cachedPermissions))
        {
            hasExternalPermissions = cachedPermissions.Services.Count > 0;
        }
        else
        {
            var externalServices = CheckExternalPermissions(player);
            hasExternalPermissions = externalServices.Count > 0;
        }
        
        if (!hasOtherActiveGroups && !hasExternalPermissions)
        {
            cachedPlayer.Groups.Remove(nightVipGroup);
            
            var remainingActiveGroup = cachedPlayer.GetPrimaryGroup();
            if (remainingActiveGroup != null)
            {
                cachedPlayer.LoadedGroup = remainingActiveGroup.GroupName;
                cachedPlayer.GroupExpiryTime = remainingActiveGroup.ExpiryTime;
                cachedPlayer.Active = true;
            }
            else
            {
                cachedPlayer.LoadedGroup = null;
                cachedPlayer.GroupExpiryTime = 0;
                cachedPlayer.Active = false;
            }
            
            AdminManager.RemovePlayerPermissions(player, Config.NightVip.Flag);
            
            Console.WriteLine($"[Mesharsky - VIP] Removed Night VIP from player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ]");
            
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.expired.title");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.nightvip.expired.info");
            ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        }
        else
        {
            Console.WriteLine($"[Mesharsky - VIP] Player {player.PlayerName} has Night VIP but also has other active VIP services - not removing Night VIP");
        }
    }
    
    private static string FormatExpiryDate(CCSPlayerController player, int expiryTime, string formatKey = "date.format.medium")
    {
        if (expiryTime == 0)
            return _localizer!.ForPlayer(player, "commands.vip.details.neverexpires");
    
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(expiryTime).ToLocalTime();
        var format = _localizer!.ForPlayer(player, formatKey);
    
        return dateTime.ToString(format);
    }
    
    private string FormatRemainingDays(CCSPlayerController player, int expiryTime)
    {
        var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secondsRemaining = expiryTime - currentTime;
        
        if (secondsRemaining <= 0)
            return _localizer!.ForPlayer(player, "vip.viptest.expired");
        
        var daysRemaining = (int)Math.Ceiling(secondsRemaining / 86400.0);
        
        return string.Format(_localizer!.ForPlayer(player, 
                daysRemaining == 1 ? "vip.viptest.expires.one" : "vip.viptest.expires.many"), 
            daysRemaining);
    }
    
    private static List<Service> CheckExternalPermissions(CCSPlayerController player)
    {
        var matchingServices = new List<Service>();
        var logDetails = false;
    
        foreach (var groupSetting in Config!.GroupSettings)
        {
            var service = ServiceManager.GetService(groupSetting.Name);
            if (service == null || !HasAnyPermission(player, service.Flag)) continue;
            if (matchingServices.Count == 0 || logDetails)
            {
                Console.WriteLine($"[Mesharsky - VIP] Player {player.PlayerName} has external permission matching VIP group {service.Name}");
            }
            matchingServices.Add(service);
        }
    
        return matchingServices;
    }
    
    private static void ApplyExternalVipPermissions(CCSPlayerController player, Player cachedPlayer, Service service)
    {
        var virtualGroup = new PlayerGroup
        {
            GroupName = service.Name,
            PlayerName = player.PlayerName,
            ExpiryTime = 0,
            Active = true
        };
        
        cachedPlayer.Groups.Add(virtualGroup);
        
        if (cachedPlayer.LoadedGroup == null)
        {
            cachedPlayer.LoadedGroup = service.Name;
            cachedPlayer.GroupExpiryTime = 0;
            cachedPlayer.Active = true;
        }
        
        PlayerCache[player.SteamID] = cachedPlayer;
    
        Console.WriteLine($"[Mesharsky - VIP] Applied external VIP service {service.Name} to player {player.PlayerName} with flag {service.Flag}");
    }
    
    private static void AssignPlayerPermissions(CCSPlayerController player, Player cachedPlayer, Service service, PlayerGroup group)
    {
        group.Active = true;
            
        if (cachedPlayer.LoadedGroup == null)
        {
            cachedPlayer.LoadedGroup = service.Name;
            cachedPlayer.GroupExpiryTime = group.ExpiryTime;
        }
            
        cachedPlayer.Active = true;
        PlayerCache[player.SteamID] = cachedPlayer;

        AdminManager.AddPlayerPermissions(player, service.Flag);
        Console.WriteLine($"[Mesharsky - VIP] Loaded service for player [ Service: {service.Name} - Name: {player.PlayerName} - SteamID: {player.SteamID} ]");

        Console.WriteLine(HasAnyPermission(player, service.Flag)
            ? $"[Mesharsky - VIP] Loaded assigned permission: {service.Flag}"
            : $"[Mesharsky - VIP] ERROR Player has no permissions assigned, should have: {service.Flag}");
    }

    private static bool IsValidPlayer(CCSPlayerController player)
    {
        if (!player.IsValid) return false;
        return !player.IsBot;
    }
}