using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class BenefitsRenderer
    {
        /// <summary>
        /// Renders all benefits for a single service to a menu
        /// </summary>
        public static void RenderServiceBenefits(IT3Menu menu, CCSPlayerController viewer, Service service)
        {
            if (service.PlayerHp > 100)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.health", service.PlayerHp), (_, _) => { });

            if (service.PlayerMaxHp > 0)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.maxhealth", service.PlayerMaxHp), (_, _) => { });
            
            if (service.PlayerVest)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.armor", service.PlayerVestRound), (_, _) => { });

            if (service.PlayerHelmet)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.helmet", service.PlayerHelmetRound), (_, _) => { });

            if (service.PlayerDefuser)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.defuser"), (_, _) => { });

            RenderGrenades(menu, viewer, service);
            RenderAbilities(menu, viewer, service);
            RenderSmokeColor(menu, viewer, service);
            RenderSpecialFeatures(menu, viewer, service);
            RenderHealthBonuses(menu, viewer, service);
        }
        
        /// <summary>
        /// Creates submenu options for a list of services (used for player's active benefits)
        /// </summary>
        public static void CreateBenefitsSubmenus(
            MesharskyVip plugin,
            IT3Menu parentMenu, 
            CCSPlayerController viewer, 
            List<Service> services)
        {
            var manager = plugin.GetMenuManager();
            if (manager == null)
                return;
                
            var heAmount = services.Max(s => s.HeAmount);
            var flashAmount = services.Max(s => s.FlashAmount);
            var smokeAmount = services.Max(s => s.SmokeAmount);
            var decoyAmount = services.Max(s => s.DecoyAmount);
            var molotovAmount = services.Max(s => s.MolotovAmount);
            var healthshotAmount = services.Max(s => s.HealthshotAmount);
            var extraJumps = services.Max(s => s.PlayerExtraJumps);
            var jumpHeight = services.Max(s => s.PlayerExtraJumpHeight);
            var hasBunnyhop = services.Any(s => s.PlayerBunnyhop);
            var hasWeaponMenu = services.Any(s => s.WeaponMenu.Enabled);
            var hasSmokeColor = services.Any(s => s.SmokeColorEnabled);
            
            var hasGrenades = heAmount > 0 || flashAmount > 0 || smokeAmount > 0 || 
                            decoyAmount > 0 || molotovAmount > 0 || healthshotAmount > 0;
                        
            if (hasGrenades)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.grenades"), (p, _) => {
                    var grenadesMenu = manager.CreateMenu(_localizer!.ForPlayer(viewer, "benefits.grenades.title"), isSubMenu: true);
                    
                    if (heAmount > 0)
                        grenadesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.he", heAmount), (_, _) => {});
                        
                    if (flashAmount > 0)
                        grenadesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.flash", flashAmount), (_, _) => {});
                        
                    if (smokeAmount > 0)
                        grenadesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.smoke", smokeAmount), (_, _) => {});
                        
                    if (decoyAmount > 0)
                        grenadesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.decoy", decoyAmount), (_, _) => {});
                        
                    if (molotovAmount > 0)
                        grenadesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.molotov", molotovAmount), (_, _) => {});
                        
                    if (healthshotAmount > 0)
                        grenadesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.healthshot", healthshotAmount), (_, _) => {});
                    
                    manager.OpenSubMenu(p, grenadesMenu);
                });
            }
            
            var hasAbilities = extraJumps > 0 || hasBunnyhop || hasWeaponMenu;
            
            if (hasAbilities)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.abilities"), (p, _) => {
                    var abilitiesMenu = manager.CreateMenu(_localizer!.ForPlayer(viewer, "benefits.abilities.title"), isSubMenu: true);
                    
                    if (extraJumps > 0)
                    {
                        var jumpType = extraJumps == 1 
                            ? _localizer!.ForPlayer(viewer, "benefits.abilities.jump.double")
                            : _localizer!.ForPlayer(viewer, "benefits.abilities.jump.triple");
                            
                        abilitiesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.jump", extraJumps + 1, jumpType), (_, _) => {});
                        abilitiesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.jumpheight", jumpHeight), (_, _) => {});
                    }
                    
                    if (hasBunnyhop)
                        abilitiesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.bhop"), (_, _) => {});
                        
                    if (hasWeaponMenu)
                        abilitiesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.weaponmenu"), (_, _) => {});
                    
                    manager.OpenSubMenu(p, abilitiesMenu);
                });
            }
            
            // Smoke Color submenu
            if (hasSmokeColor)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.smokecolor"), (p, _) => {
                    var smokeColorMenu = manager.CreateMenu(_localizer!.ForPlayer(viewer, "benefits.smokecolor.title"), isSubMenu: true);
            
                    foreach (var service in services.Where(s => s.SmokeColorEnabled))
                    {
                        if (service.SmokeColorRandom)
                        {
                            smokeColorMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.random", service.Name), (_, _) => {});
                        }
                        else
                        {
                            smokeColorMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.custom", 
                                service.Name, service.SmokeColorR, service.SmokeColorG, service.SmokeColorB), (_, _) => {});
                        }
                    }
            
                    manager.OpenSubMenu(p, smokeColorMenu);
                });
            }
            
            CreateSpecialFeaturesSubmenu(plugin, parentMenu, viewer, services);
            CreateHealthBonusesSubmenu(plugin, parentMenu, viewer, services);
        }
        
        /// <summary>
        /// Renders grenade benefits to a menu
        /// </summary>
        private static void RenderGrenades(IT3Menu menu, CCSPlayerController viewer, Service service)
        {
            var hasGrenades = service.HeAmount > 0 || service.FlashAmount > 0 || 
                            service.SmokeAmount > 0 || service.DecoyAmount > 0 || 
                            service.MolotovAmount > 0 || service.HealthshotAmount > 0;
                            
            if (!hasGrenades) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.header"), (_, _) => { });
            
            if (service.HeAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.he", service.HeAmount), (_, _) => { });
                
            if (service.FlashAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.flash", service.FlashAmount), (_, _) => { });
                
            if (service.SmokeAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.smoke", service.SmokeAmount), (_, _) => { });
                
            if (service.DecoyAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.decoy", service.DecoyAmount), (_, _) => { });
                
            if (service.MolotovAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.molotov", service.MolotovAmount), (_, _) => { });
                
            if (service.HealthshotAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.healthshot", service.HealthshotAmount), (_, _) => { });
        }
        
        /// <summary>
        /// Renders special abilities to a menu
        /// </summary>
        private static void RenderAbilities(IT3Menu menu, CCSPlayerController viewer, Service service)
        {
            var hasAbilities = service.PlayerExtraJumps > 0 || service.PlayerBunnyhop || service.WeaponMenu.Enabled;
            
            if (!hasAbilities) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.header"), (_, _) => { });
            
            if (service.PlayerExtraJumps > 0)
            {
                var jumpType = service.PlayerExtraJumps == 1 
                    ? _localizer!.ForPlayer(viewer, "benefits.abilities.jump.double", service.PlayerExtraJumps + 1)
                    : _localizer!.ForPlayer(viewer, "benefits.abilities.jump.triple", service.PlayerExtraJumps + 1);
                    
                menu.AddOption(jumpType, (_, _) => { });
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.jumpheight", service.PlayerExtraJumpHeight), (_, _) => { });
            }
            
            if (service.PlayerBunnyhop)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.bhop"), (_, _) => { });
                
            if (service.WeaponMenu.Enabled)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.weaponmenu"), (_, _) => { });
        }
        
        /// <summary>
        /// Renders smoke color options to a menu
        /// </summary>
        private static void RenderSmokeColor(IT3Menu menu, CCSPlayerController viewer, Service service)
        {
            if (!service.SmokeColorEnabled) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.header"), (_, _) => { });
            
            if (service.SmokeColorRandom)
            {
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.random", service.Name), (_, _) => { });
            }
            else
            {
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.custom", 
                    service.Name, service.SmokeColorR, service.SmokeColorG, service.SmokeColorB), (_, _) => { });
            }
        }
        
        /// <summary>
        /// Renders new features to a menu
        /// </summary>
        private static void RenderSpecialFeatures(IT3Menu menu, CCSPlayerController viewer, Service service)
        {
            var hasNewFeatures = service.InfiniteAmmo || service.FastReload || service.KillScreen;
            
            if (!hasNewFeatures) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.header"), (_, _) => { });
            
            if (service.InfiniteAmmo)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.infiniteammo"), (_, _) => { });
            
            if (service.FastReload)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.fastreload"), (_, _) => { });
                
            if (service.KillScreen)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.killscreen"), (_, _) => { });
        }

        /// <summary>
        /// Creates a submenu for special features
        /// </summary>
        private static void CreateSpecialFeaturesSubmenu(
            MesharskyVip plugin,
            IT3Menu parentMenu, 
            CCSPlayerController viewer, 
            List<Service> services)
        {
            var manager = plugin.GetMenuManager();
            if (manager == null)
                return;
                
            var hasInfiniteAmmo = services.Any(s => s.InfiniteAmmo);
            var hasFastReload = services.Any(s => s.FastReload);
            var hasKillScreen = services.Any(s => s.KillScreen);
            
            var hasSpecialFeatures = hasInfiniteAmmo || hasFastReload || hasKillScreen;
            
            if (hasSpecialFeatures)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.special"), (p, _) => {
                    var specialFeaturesMenu = manager.CreateMenu(_localizer!.ForPlayer(viewer, "benefits.special.title"), isSubMenu: true);
                    
                    if (hasInfiniteAmmo)
                        specialFeaturesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.infiniteammo"), (_, _) => {});
                    
                    if (hasFastReload)
                        specialFeaturesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.fastreload"), (_, _) => {});
                        
                    if (hasKillScreen)
                        specialFeaturesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.killscreen"), (_, _) => {});
                    
                    manager.OpenSubMenu(p, specialFeaturesMenu);
                });
            }
        }
        
        /// <summary>
        /// Renders health bonus features to a menu
        /// </summary>
        private static void RenderHealthBonuses(IT3Menu menu, CCSPlayerController viewer, Service service)
        {
            var hasHealthBonuses = service.HealthPerKill > 0 || service.HealthPerHeadshot > 0 || 
                                   service.HealthPerKnifeKill > 0 || service.HealthPerNoScope > 0;
            
            if (!hasHealthBonuses) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.header"), (_, _) => { });
            
            if (service.HealthPerKill > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.kill", service.HealthPerKill), (_, _) => { });
            
            if (service.HealthPerHeadshot > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.headshot", service.HealthPerHeadshot), (_, _) => { });
                
            if (service.HealthPerKnifeKill > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.knife", service.HealthPerKnifeKill), (_, _) => { });
                
            if (service.HealthPerNoScope > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.noscope", service.HealthPerNoScope), (_, _) => { });
        }
        
        /// <summary>
        /// Creates a submenu for health bonuses
        /// </summary>
        private static void CreateHealthBonusesSubmenu(
            MesharskyVip plugin,
            IT3Menu parentMenu, 
            CCSPlayerController viewer, 
            List<Service> services)
        {
            var manager = plugin.GetMenuManager();
            if (manager == null)
                return;
                
            var maxKillHealth = services.Max(s => s.HealthPerKill);
            var maxHeadshotHealth = services.Max(s => s.HealthPerHeadshot);
            var maxKnifeHealth = services.Max(s => s.HealthPerKnifeKill);
            var maxNoScopeHealth = services.Max(s => s.HealthPerNoScope);
            
            var hasHealthBonuses = maxKillHealth > 0 || maxHeadshotHealth > 0 || 
                                   maxKnifeHealth > 0 || maxNoScopeHealth > 0;
            
            if (hasHealthBonuses)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.healthbonus"), (p, _) => {
                    var healthBonusMenu = manager.CreateMenu(_localizer!.ForPlayer(viewer, "benefits.healthbonus.title"), isSubMenu: true);
                    
                    if (maxKillHealth > 0)
                        healthBonusMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.kill", maxKillHealth), (_, _) => {});
                    
                    if (maxHeadshotHealth > 0)
                        healthBonusMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.headshot", maxHeadshotHealth), (_, _) => {});
                        
                    if (maxKnifeHealth > 0)
                        healthBonusMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.knife", maxKnifeHealth), (_, _) => {});
                        
                    if (maxNoScopeHealth > 0)
                        healthBonusMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.healthbonus.noscope", maxNoScopeHealth), (_, _) => {});
                    
                    manager.OpenSubMenu(p, healthBonusMenu);
                });
            }
        }
    }
}
