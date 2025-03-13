using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;
using PostSelectAction = CounterStrikeSharp.API.Modules.Menu.PostSelectAction;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public void RegisterPlayerCommands()
    {
        var commandManager = GetCommandManager();
        
        commandManager.RegisterCommand("vip_command", "Show your VIP groups", cmd_ShowVipGroups);
        commandManager.RegisterCommand("benefits_command", "Show your VIP benefits", cmd_ShowVipBenefits);
        commandManager.RegisterCommand("online_command", "Show online VIP players", cmd_ShowOnlineVips);
        
        Console.WriteLine("[Mesharsky - VIP] Player commands registered");
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ShowVipGroups(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
        
        var cachedPlayer = GetOrCreatePlayer(player.SteamID, player.PlayerName);
        
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        
        if (activeGroups.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.noactive");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.purchase");
            return;
        }
        
        var menu = new ChatMenu(_localizer!.ForPlayer(player, "commands.vip.menu.title"));
        
        foreach (var group in activeGroups)
        {
            var service = ServiceManager.GetService(group.GroupName);
            if (service == null) continue;
            
            var expiryText = group.ExpiryTime == 0 
                ? _localizer!.ForPlayer(player, "commands.vip.details.neverexpires") 
                : _localizer!.ForPlayer(player, "commands.vip.details.expires", 
                    DateTimeOffset.FromUnixTimeSeconds(group.ExpiryTime).ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                
            menu.AddMenuOption($"{group.GroupName} - {expiryText}", (player, _) => {
                ShowGroupDetails(player, group);
            });
        }
        
        menu.AddMenuOption(_localizer!.ForPlayer(player, "commands.vip.menu.extend"), (player, _) => {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.extend");
        });
        
        MenuManager.OpenChatMenu(player, menu);
    }
    
    private void ShowGroupDetails(CCSPlayerController player, PlayerGroup group)
    {
        ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.details.title", group.GroupName);
        
        var expiryText = group.ExpiryTime == 0 
            ? _localizer!.ForPlayer(player, "commands.vip.details.neverexpires") 
            : _localizer!.ForPlayer(player, "commands.vip.details.expires", 
                FormatExpiryDate(player, group.ExpiryTime, "date.format.long"));
            
        ChatHelper.PrintLocalizedChat(player, _localizer!, false, expiryText);
        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.details.usebenefits");
        ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ShowVipBenefits(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
        
        var cachedPlayer = GetOrCreatePlayer(player.SteamID, player.PlayerName);
        
        var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        
        if (activeGroups.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.noactive");
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.vip.purchase");
            return;
        }
        
        var activeServices = activeGroups
            .Select(g => ServiceManager.GetService(g.GroupName))
            .Where(s => s != null)
            .ToList();
            
        if (activeServices.Count == 0)
            return;
        
        var bestHp = activeServices.Max(s => s!.PlayerHp);
        var hasArmor = activeServices.Any(s => s!.PlayerVest);
        var hasHelmet = activeServices.Any(s => s!.PlayerHelmet);
        var hasDefuser = activeServices.Any(s => s!.PlayerDefuser);
        var heAmount = activeServices.Max(s => s!.HeAmount);
        var flashAmount = activeServices.Max(s => s!.FlashAmount);
        var smokeAmount = activeServices.Max(s => s!.SmokeAmount);
        var decoyAmount = activeServices.Max(s => s!.DecoyAmount);
        var molotovAmount = activeServices.Max(s => s!.MolotovAmount);
        var healthshotAmount = activeServices.Max(s => s!.HealthshotAmount);
        var extraJumps = activeServices.Max(s => s!.PlayerExtraJumps);
        var jumpHeight = activeServices.Max(s => s!.PlayerExtraJumpHeight);
        var hasBunnyhop = activeServices.Any(s => s!.PlayerBunnyhop);
        var hasWeaponMenu = activeServices.Any(s => s!.PlayerWeaponmenu);
        
        var mainMenu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.benefits.menu.title"), this)
        {
            PostSelectAction = (CS2ScreenMenuAPI.Enums.PostSelectAction)PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        var groupNames = string.Join(", ", activeGroups.Select(g => g.GroupName));
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.activeservices", groupNames), (_, _) => { }, disabled: true);
        
        if (bestHp > 100)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.health", bestHp), (_, _) => { });
        }
        
        if (hasArmor)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.armor"), (_, _) => { });
        }
        
        if (hasHelmet)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.helmet"), (_, _) => { });
        }
        
        if (hasDefuser)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.defuser"), (_, _) => { });
        }
        
        if (heAmount > 0 || flashAmount > 0 || smokeAmount > 0 || decoyAmount > 0 || molotovAmount > 0 || healthshotAmount > 0)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.grenades"), (p, _) => {
                var grenadesMenu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.benefits.grenades.title"), this)
                {
                    IsSubMenu = true,
                    PostSelectAction = (CS2ScreenMenuAPI.Enums.PostSelectAction)PostSelectAction.Nothing,
                    TextColor = Color.Orange,
                    ParentMenu = mainMenu,
                    FontName = "Verdana Bold"
                };
                
                if (heAmount > 0)
                    grenadesMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.grenades.he", heAmount), (_, _) => {});
                    
                if (flashAmount > 0)
                    grenadesMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.grenades.flash", flashAmount), (_, _) => {});
                    
                if (smokeAmount > 0)
                    grenadesMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.grenades.smoke", smokeAmount), (_, _) => {});
                    
                if (decoyAmount > 0)
                    grenadesMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.grenades.decoy", decoyAmount), (_, _) => {});
                    
                if (molotovAmount > 0)
                    grenadesMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.grenades.molotov", molotovAmount), (_, _) => {});
                    
                if (healthshotAmount > 0)
                    grenadesMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.grenades.healthshot", healthshotAmount), (_, _) => {});
                
                MenuAPI.OpenSubMenu(this, p, grenadesMenu);
            });
        }
        
        if (extraJumps > 0 || hasBunnyhop || hasWeaponMenu)
        {
            mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.abilities"), (p, _) => {
                var specialMenu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.benefits.abilities.title"), this)
                {
                    IsSubMenu = true,
                    PostSelectAction = (CS2ScreenMenuAPI.Enums.PostSelectAction)PostSelectAction.Nothing,
                    TextColor = Color.GreenYellow,
                    ParentMenu = mainMenu,
                    FontName = "Verdana Bold"
                };
                
                if (extraJumps > 0)
                {
                    var jumpType = extraJumps == 1 
                        ? _localizer!.ForPlayer(player, "commands.benefits.abilities.jump.double")
                        : _localizer!.ForPlayer(player, "commands.benefits.abilities.jump.triple");
                        
                    specialMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.abilities.jump", extraJumps + 1, jumpType), (_, _) => {});
                    specialMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.abilities.jumpheight", jumpHeight), (_, _) => {});
                }
                
                if (hasBunnyhop)
                    specialMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.abilities.bhop"), (_, _) => {});
                    
                if (hasWeaponMenu)
                    specialMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.abilities.weapons"), (_, _) => {});
                
                MenuAPI.OpenSubMenu(this, p, specialMenu);
            });
        }
        
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.sourceinfo"), (p, _) => {
            var sourceMenu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.benefits.source.title"), this)
            {
                IsSubMenu = true,
                PostSelectAction = (CS2ScreenMenuAPI.Enums.PostSelectAction)PostSelectAction.Nothing,
                TextColor = Color.LightBlue,
                ParentMenu = mainMenu,
                FontName = "Verdana Bold"
            };
            
            foreach (var group in activeGroups)
            {
                var service = ServiceManager.GetService(group.GroupName);
                if (service == null) continue;
                
                var expiryText = group.ExpiryTime == 0 
                    ? _localizer!.ForPlayer(player, "commands.vip.details.neverexpires") 
                    : _localizer!.ForPlayer(player, "commands.vip.details.expires", 
                        FormatExpiryDate(player, group.ExpiryTime));
                
                sourceMenu.AddOption($"{group.GroupName} - {expiryText}", (_, _) => {});
            }
            
            MenuAPI.OpenSubMenu(this, p, sourceMenu);
        });
        
        mainMenu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.close"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, mainMenu);
    }
    
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void cmd_ShowOnlineVips(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;
            
        if (!Config!.PluginSettings.OnlineList)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.online.disabled");
            return;
        }
        
        var vipPlayers = Utilities.GetPlayers()
            .Where(p => p is { IsValid: true, IsBot: false } && PlayerCache.TryGetValue(p.SteamID, out var cached) && cached.Groups.Any(g => g.Active))
            .ToList();
            
        if (vipPlayers.Count == 0)
        {
            ChatHelper.PrintLocalizedChat(player, _localizer!, true, "commands.online.none");
            return;
        }
        
        CreateOnlineVipsMenu(player, vipPlayers);
    }

    private void CreateOnlineVipsMenu(CCSPlayerController player, List<CCSPlayerController> vipPlayers)
    {
        var menu = new ScreenMenu(_localizer!.ForPlayer(player, "commands.online.title", vipPlayers.Count), this)
        {
            PostSelectAction = CS2ScreenMenuAPI.Enums.PostSelectAction.Nothing,
            IsSubMenu = false,
            TextColor = Color.Gold,
            FontName = "Verdana Bold",
            MenuType = MenuType.Both
        };
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.online.header", vipPlayers.Count), (_, _) => { }, disabled: true);
        
        foreach (var vipPlayer in vipPlayers)
        {
            if (!PlayerCache.TryGetValue(vipPlayer.SteamID, out var cached)) 
                continue;
                
            var groups = cached.Groups.Where(g => g.Active)
                .Select(g => ServiceManager.GetService(g.GroupName)?.Name ?? g.GroupName)
                .ToList();
                
            var groupsText = string.Join(", ", groups);
            
            menu.AddOption($"{vipPlayer.PlayerName} - {groupsText}", (p, _) => {
                ShowVipPlayerDetails(p, vipPlayer, menu);
            });
        }
        
        menu.AddOption(_localizer!.ForPlayer(player, "commands.benefits.menu.close"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
        });
        
        MenuAPI.OpenMenu(this, player, menu);
    }

    private void ShowVipPlayerDetails(CCSPlayerController viewer, CCSPlayerController vipPlayer, ScreenMenu parentMenu)
    {
        if (!PlayerCache.TryGetValue(vipPlayer.SteamID, out var cached))
            return;
        
        var detailsMenu = new ScreenMenu(_localizer!.ForPlayer(viewer, "commands.online.player.details", vipPlayer.PlayerName), this)
        {
            IsSubMenu = true,
            PostSelectAction = CS2ScreenMenuAPI.Enums.PostSelectAction.Nothing,
            TextColor = Color.LightGreen,
            FontName = "Verdana Bold",
            ParentMenu = parentMenu
        };
        
        detailsMenu.AddOption(_localizer!.ForPlayer(viewer, "commands.online.player.name", vipPlayer.PlayerName), (_, _) => { }, disabled: true);
        
        var groupsWithExpiry = cached.Groups.Where(g => g.Active).Select(group => (group.GroupName, group.ExpiryTime)).ToList();
        
        foreach (var (name, expiryTime) in groupsWithExpiry)
        {
            var expiryText = expiryTime == 0 
                ? _localizer!.ForPlayer(viewer, "commands.vip.details.neverexpires") 
                : _localizer!.ForPlayer(viewer, "commands.vip.details.expires", 
                    FormatExpiryDate(viewer, expiryTime));
                    
            detailsMenu.AddOption($"{name} - {expiryText}", (_, _) => { }, disabled: true);
        }
        
        detailsMenu.AddOption(_localizer!.ForPlayer(viewer, "commands.online.back"), (p, _) => {
            MenuAPI.CloseActiveMenu(p);
            MenuAPI.OpenMenu(this, p, parentMenu);
        });
        
        MenuAPI.OpenSubMenu(this, viewer, detailsMenu);
    }
}