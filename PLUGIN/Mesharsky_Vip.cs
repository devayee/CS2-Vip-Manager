using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Microsoft.Extensions.Localization;

namespace Mesharsky_Vip;

public partial class MesharskyVip : BasePlugin
{
    public override string ModuleName => "VIP Manager";
    public override string ModuleAuthor => "Mesharsky";
    public override string ModuleDescription => "Advanced vip manager plugin.";
    public override string ModuleVersion => "1.2.0";

    private static IStringLocalizer? _localizer { get; set; }

    public override void Load(bool hotReload)
    {
        LoadConfiguration();
        LoadDatabase();
        LoadEvents();
        Initialize_OnTick();
        RegisterAdminCommands();
        RegisterPlayerCommands();

        if (hotReload)
        {
            AddTimer(1.0f, HandleHotReload);
        }
    }

    public override void Unload(bool hotReload)
    {

    }

    public override void OnAllPluginsLoaded(bool isReload)
    {
        _localizer = Localizer;
    }

    private void HandleHotReload()
    {
        Console.WriteLine("[Mesharsky - VIP] Plugin hot reloaded - reassigning permissions to all players");
        
        var players = Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false }).ToList();
        
        if (players.Count == 0)
        {
            Console.WriteLine("[Mesharsky - VIP] No players online during hot reload");
            return;
        }
        
        Console.WriteLine($"[Mesharsky - VIP] Found {players.Count} players to reassign permissions");
        
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

    private static void ReassignPlayerPermissions(CCSPlayerController player, Player cachedPlayer)
    {
        var allGroups = Config!.GroupSettings.Select(g => g.Flag).ToList();
        if (Config.NightVip.Enabled)
        {
            allGroups.Add(Config.NightVip.Flag);
        }
        
        foreach (var flag in allGroups)
        {
            AdminManager.RemovePlayerPermissions(player, flag);
        }
        
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        
        if (activeGroups.Count != 0)
        {
            Console.WriteLine($"[Mesharsky - VIP] Reassigning {activeGroups.Count} active groups to player {player.PlayerName}");

            foreach (var service in activeGroups.Select(group => ServiceManager.GetService(group.GroupName)).OfType<Service>())
            {
                Console.WriteLine($"[Mesharsky - VIP] Reassigning permission {service.Flag} from group {service.Name}");
                AdminManager.AddPlayerPermissions(player, service.Flag);
            }
        }
        else if (Config.NightVip.Enabled && IsNightVipTime() && !cachedPlayer.Active)
        {
            Console.WriteLine($"[Mesharsky - VIP] Reassigning Night VIP to player {player.PlayerName}");
            AssignNightVip(player);
        }
        else
        {
            Console.WriteLine($"[Mesharsky - VIP] Player {player.PlayerName} has no VIP permissions to reassign");
        }
    }

    private static void ResetPlayerCache(ulong steamId)
    {
        if (PlayerCache.ContainsKey(steamId))
        {
            PlayerCache.TryRemove(steamId, out _);
        }
    }
}