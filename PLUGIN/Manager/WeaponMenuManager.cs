using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class PlayerWeaponSelection
    {
        public string? PrimaryWeapon { get; set; }
        public string? SecondaryWeapon { get; set; }
        public int TeamNum { get; set; }
        public long SavedTime { get; set; }
    }
    
    private void InitializeWeaponMenu()
    {
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawnWeaponMenu);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFullWeaponMenu);
        RegisterWeaponMenuCommands();

        CreateWeaponPreferencesTable();
        
        Console.WriteLine("[Mesharsky - VIP] Weapon menu feature initialized");
    }

    private HookResult OnPlayerConnectFullWeaponMenu(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;
        
        Server.NextFrame(() => {
            Task.Run(() => LoadPlayerWeaponPreferences(player));
        });
        
        return HookResult.Continue;
    }
    
    private void RegisterWeaponMenuCommands()
    {
        var commandManager = GetCommandManager();
        
        commandManager.RegisterCommand("weapons_menu_command", "Open weapon selection menu", cmd_WeaponsMenu);
        commandManager.RegisterCommand("weapons_menu_reset_command", "Reset your weapon selection", cmd_ResetWeaponsMenu);
        
        Console.WriteLine("[Mesharsky - VIP] Weapon menu commands registered");
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_WeaponsMenu(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
            
        if (!CanUseWeaponMenu(player))
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.weaponsmenu.noaccess");
            return;
        }
        
        var config = GetPlayerWeaponMenuConfig(player);
        if (config == null)
            return;
        
        CreateTeamSelectionMenu(player);
    }

    private void CreateTeamSelectionMenu(CCSPlayerController player)
    {
        var config = GetPlayerWeaponMenuConfig(player);
        if (config == null)
            return;
            
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.weaponsmenu.selectteam"), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Orange,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        var hasCtSelection = TryGetTeamWeaponSelection(player, 3, out var ctSelection);
        var ctPrimaryDisplay = hasCtSelection && ctSelection.PrimaryWeapon != null 
            ? GetWeaponDisplayName(ctSelection.PrimaryWeapon) 
            : "None";
        var ctSecondaryDisplay = hasCtSelection && ctSelection.SecondaryWeapon != null 
            ? GetWeaponDisplayName(ctSelection.SecondaryWeapon) 
            : "None";
            
        menu.AddOption($"{_localizer!.ForPlayer(player, "commands.weaponsmenu.configct")} " +
            $"({ctPrimaryDisplay}/{ctSecondaryDisplay})", (p, _) => {
            CreateMainWeaponsMenu(p, 3);
        });
        
        // Option for T Team
        var hasTSelection = TryGetTeamWeaponSelection(player, 2, out var tSelection);
        var tPrimaryDisplay = hasTSelection && tSelection.PrimaryWeapon != null 
            ? GetWeaponDisplayName(tSelection.PrimaryWeapon) 
            : "None";
        var tSecondaryDisplay = hasTSelection && tSelection.SecondaryWeapon != null 
            ? GetWeaponDisplayName(tSelection.SecondaryWeapon) 
            : "None";
            
        menu.AddOption($"{_localizer!.ForPlayer(player, "commands.weaponsmenu.configt")} " +
            $"({tPrimaryDisplay}/{tSecondaryDisplay})", (p, _) => {
            CreateMainWeaponsMenu(p, 2);
        });
        
        if (player.TeamNum == 2 || player.TeamNum == 3)
        {
            menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.configcurrent"), (p, _) => {
                CreateMainWeaponsMenu(p, p.TeamNum);
            });
        }
        
        MenuAPI.OpenMenu(this, player, menu);
    }
    
    private void CreateTeamSelectionResetMenu(CCSPlayerController player)
    {
        var config = GetPlayerWeaponMenuConfig(player);
        if (config == null)
            return;
            
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.weaponsmenu.selectteamreset"), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Orange,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.resetct"), (p, _) => {
            RemoveTeamWeaponSelection(p, 3);
            ChatHelper.PrintLocalizedChat(p, _localizer!, true, "commands.weaponsmenu.resetteam", "CT");
            MenuAPI.CloseActiveMenu(p);
        });
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.resett"), (p, _) => {
            RemoveTeamWeaponSelection(p, 2);
            ChatHelper.PrintLocalizedChat(p, _localizer!, true, "commands.weaponsmenu.resetteam", "T");
            MenuAPI.CloseActiveMenu(p);
        });
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.resetboth"), (p, _) => {
            RemoveTeamWeaponSelection(p, 2);
            RemoveTeamWeaponSelection(p, 3);
            ChatHelper.PrintLocalizedChat(p, _localizer!, true, "commands.weaponsmenu.resetallteams");
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, menu);
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ResetWeaponsMenu(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
            
        if (!CanUseWeaponMenu(player))
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.weaponsmenu.noaccess");
            return;
        }
        
        CreateTeamSelectionResetMenu(player);
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.weaponsmenu.reset");
    }
    
    private HookResult OnPlayerSpawnWeaponMenu(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (IsWarmup())
            return HookResult.Continue;
            
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot || player.TeamNum < 2)
            return HookResult.Continue;
            
        if (!CanUseWeaponMenu(player))
            return HookResult.Continue;
        
        Server.NextFrame(() => 
        {
            AddTimer(0.5f, () => 
            {
                if (!player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null)
                    return;
                
                if (TryGetPlayerWeaponSelection(player, out var selection))
                {
                    ApplyWeaponSelection(player, selection);
                }
                else
                {
                    var config = GetPlayerWeaponMenuConfig(player);
                    if (config != null && GetEffectiveRoundNumber() >= config.MinRound)
                    {
                        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.weaponsmenu.notconfigured");
                    }
                }
            });
        });
        
        return HookResult.Continue;
    }
    
    private static bool CanUseWeaponMenu(CCSPlayerController player)
    {
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer) || !cachedPlayer.Active)
            return false;
            
        return PlayerHasFeature(player, service => service.WeaponMenu.Enabled);
    }
    
    private static WeaponMenuConfig? GetPlayerWeaponMenuConfig(CCSPlayerController player)
    {
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer) || !cachedPlayer.Active)
            return null;
            
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();

        return (from service in activeGroups.Select(@group => ServiceManager.GetService(@group.GroupName)).OfType<Service>() where service.WeaponMenu.Enabled select service.WeaponMenu).FirstOrDefault();
    }
    
    private void CreateMainWeaponsMenu(CCSPlayerController player, int teamNum = 0)
    {
        var config = GetPlayerWeaponMenuConfig(player);
        if (config == null)
            return;
        
        if (teamNum == 0)
        {
            teamNum = player.TeamNum;
            
            if (teamNum != 2 && teamNum != 3)
            {
                CreateTeamSelectionMenu(player);
                return;
            }
        }
            
        var hasSelection = TryGetTeamWeaponSelection(player, teamNum, out var selection);
        
        string teamName = teamNum == 2 ? "T" : "CT";
        
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.weaponsmenu.titleteam", teamName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Orange,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.chooseprimary", 
            hasSelection && selection.PrimaryWeapon != null ? GetWeaponDisplayName(selection.PrimaryWeapon) : "None"), 
            (p, _) => {
                CreateWeaponSelectionMenu(p, true, teamNum);
            });
            
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.choosesecondary", 
            hasSelection && selection.SecondaryWeapon != null ? GetWeaponDisplayName(selection.SecondaryWeapon) : "None"), 
            (p, _) => {
                CreateWeaponSelectionMenu(p, false, teamNum);
            });
        
        if (hasSelection)
        {
            menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.resetloadout"), (p, _) => {
                RemoveTeamWeaponSelection(p, teamNum);
                ChatHelper.PrintLocalizedChat(p, _localizer!, true, "commands.weaponsmenu.resetteam", teamName);
                MenuAPI.CloseActiveMenu(p);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.backtoteams"), (p, _) => {
            CreateTeamSelectionMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, menu);
    }
    
    private void CreateWeaponSelectionMenu(CCSPlayerController player, bool isPrimary, int teamNum = 0)
    {
        var config = GetPlayerWeaponMenuConfig(player);
        if (config == null)
            return;
            
        if (teamNum == 0)
        {
            teamNum = player.TeamNum;
        }
            
        List<string> weaponsList;
        if (teamNum == 2)
        {
            weaponsList = isPrimary ? config.TPrimaryWeapons : config.TSecondaryWeapons;
        }
        else
        {
            weaponsList = isPrimary ? config.CTPrimaryWeapons : config.CTSecondaryWeapons;
        }
        
        if (weaponsList.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.weaponsmenu.noweapons");
            return;
        }
        
        var teamName = teamNum == 2 ? "T" : "CT";
        
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, 
            isPrimary ? "commands.weaponsmenu.selectprimaryteam" : "commands.weaponsmenu.selectsecondaryteam", teamName), this)
        {
            PostSelectAction = PostSelectAction.Nothing,
            IsSubMenu = true,
            TextColor = Color.Gold,
            FontName = "Verdana Bold"
        };
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.none"), (p, _) => {
            UpdateTeamWeaponSelection(p, isPrimary, null, teamNum);
            ChatHelper.PrintLocalizedChat(p, _localizer!, true, "commands.weaponsmenu.selectionclearedteam", 
                isPrimary ? "primary" : "secondary", teamName);
            CreateMainWeaponsMenu(p, teamNum);
        });
        
        foreach (var weapon in weaponsList)
        {
            var displayName = GetWeaponDisplayName(weapon);
            menu.AddOption(displayName, (p, _) => {
                UpdateTeamWeaponSelection(p, isPrimary, weapon, teamNum);
                ChatHelper.PrintLocalizedChat(p, _localizer!, true, "commands.weaponsmenu.weaponselectedteam", 
                    isPrimary ? "primary" : "secondary", teamName, displayName);
                CreateMainWeaponsMenu(p, teamNum);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.weaponsmenu.back"), (p, _) => {
            CreateMainWeaponsMenu(p, teamNum);
        });
        
        MenuAPI.OpenMenu(this, player, menu);
    }
    
    private void UpdateTeamWeaponSelection(CCSPlayerController player, bool isPrimary, string? weaponName, int teamNum)
    {
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer))
            return;
            
        if (!cachedPlayer.WeaponSelections.TryGetValue(teamNum, out var selection))
        {
            selection = new PlayerWeaponSelection
            {
                TeamNum = teamNum,
                SavedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            cachedPlayer.WeaponSelections[teamNum] = selection;
        }
        
        if (isPrimary)
        {
            selection.PrimaryWeapon = weaponName;
        }
        else
        {
            selection.SecondaryWeapon = weaponName;
        }
        
        selection.SavedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        if (string.IsNullOrEmpty(selection.PrimaryWeapon) && string.IsNullOrEmpty(selection.SecondaryWeapon))
        {
            RemoveTeamWeaponSelection(player, teamNum);
            return;
        }
    }
    
    private static void ApplyWeaponSelection(CCSPlayerController player, PlayerWeaponSelection selection)
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
        if (gameRules == null)
            return;
        
        if (gameRules.BuyTimeEnded || 
            player.PlayerPawn.Value == null || 
            !player.PlayerPawn.Value.InBuyZone)
            return;
        
        var config = GetPlayerWeaponMenuConfig(player);
        if (config == null || GetEffectiveRoundNumber() < config.MinRound)
            return;
        
        RemoveWeapons(player);
        
        if (!string.IsNullOrEmpty(selection.PrimaryWeapon))
        {
            player.GiveNamedItem(selection.PrimaryWeapon);
        }
        
        if (!string.IsNullOrEmpty(selection.SecondaryWeapon))
        {
            player.GiveNamedItem(selection.SecondaryWeapon);
        }
    }

    private static bool TryGetTeamWeaponSelection(CCSPlayerController player, int teamNum, out PlayerWeaponSelection selection)
    {
        selection = null!;
        
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer))
            return false;
            
        return cachedPlayer.WeaponSelections.TryGetValue(teamNum, out selection!);
    }
    
    private static bool TryGetPlayerWeaponSelection(CCSPlayerController player, out PlayerWeaponSelection selection)
    {
        selection = null!;
        
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer))
            return false;
            
        return cachedPlayer.WeaponSelections.TryGetValue(player.TeamNum, out selection!);
    }

    private static void RemoveTeamWeaponSelection(CCSPlayerController player, int teamNum)
    {
        if (!PlayerCache.TryGetValue(player.SteamID, out var cachedPlayer)) 
            return;
            
        if (cachedPlayer.WeaponSelections.TryGetValue(teamNum, out _))
        {
            Server.NextFrame(() => {
                Task.Run(() => DeleteWeaponPreferencesFromDb(player, teamNum));
            });
            
            cachedPlayer.WeaponSelections.Remove(teamNum);
        }
    }
    
    // Snippet used from daffy module for VIP CORE specially: AddEntityIOEvent call.
    private static void RemoveWeapons(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value?.WeaponServices == null || player.PlayerPawn.Value?.ItemServices == null)
            return;

        var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons.ToList();

        if (weapons.Count == 0)
            return;

        foreach (var weapon in weapons)
        {
            if (!weapon.IsValid || weapon.Value == null ||
                !weapon.Value.IsValid)
                continue;

            if (weapon.Value.Entity == null) continue;
            if (!weapon.Value.VisibleinPVS) continue;

            var weaponData = weapon.Value.As<CCSWeaponBase>().VData;

            if (weaponData?.GearSlot is gear_slot_t.GEAR_SLOT_RIFLE or gear_slot_t.GEAR_SLOT_PISTOL)
            {
                weapon.Value?.AddEntityIOEvent("Kill", weapon.Value, null, "", 0.12f);
            }
        }
    }
    
    private static string GetWeaponDisplayName(string weaponName)
    {
        var name = weaponName.Replace("weapon_", "");
        
        return char.ToUpper(name[0]) + name.Substring(1);
    }
}