using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Enums;
using CS2ScreenMenuAPI.Internal;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class BenefitsRenderer
    {
        /// <summary>
        /// Renders all benefits for a single service to a menu
        /// </summary>
        public static void RenderServiceBenefits(ScreenMenu menu, CCSPlayerController viewer, Service service, bool isDisabled = true)
        {
            if (service.PlayerHp > 100)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.health", service.PlayerHp), (_, _) => { },
                        disabled: isDisabled);

            if (service.PlayerMaxHp > 0)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.maxhealth", service.PlayerMaxHp),
                        (_, _) => { }, disabled: isDisabled);
            
            if (service.PlayerVest)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.armor", service.PlayerVestRound),
                        (_, _) => { }, disabled: isDisabled);

            if (service.PlayerHelmet)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.helmet", service.PlayerHelmetRound),
                        (_, _) => { }, disabled: isDisabled);

            if (service.PlayerDefuser)
                if (_localizer != null)
                    menu.AddOption(_localizer.ForPlayer(viewer, "benefits.defuser"), (_, _) => { },
                        disabled: isDisabled);

            RenderGrenades(menu, viewer, service, isDisabled);
            RenderAbilities(menu, viewer, service, isDisabled);
            RenderSmokeColor(menu, viewer, service, isDisabled);
            RenderSpecialFeatures(menu, viewer, service, isDisabled);
        }
        
        /// <summary>
        /// Creates submenu options for a list of services (used for player's active benefits)
        /// </summary>
        public static void CreateBenefitsSubmenus(
            MesharskyVip plugin,
            ScreenMenu parentMenu, 
            CCSPlayerController viewer, 
            List<Service> services)
        {
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
                    var grenadesMenu = new ScreenMenu(_localizer!.ForPlayer(viewer, "benefits.grenades.title"), plugin)
                    {
                        IsSubMenu = true,
                        PostSelectAction = PostSelectAction.Nothing,
                        TextColor = Color.Orange,
                        ParentMenu = parentMenu,
                        FontName = "Verdana Bold"
                    };
                    
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
                    
                    MenuAPI.OpenSubMenu(plugin, p, grenadesMenu);
                });
            }
            
            var hasAbilities = extraJumps > 0 || hasBunnyhop || hasWeaponMenu;
            
            if (hasAbilities)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.abilities"), (p, _) => {
                    var abilitiesMenu = new ScreenMenu(_localizer!.ForPlayer(viewer, "benefits.abilities.title"), plugin)
                    {
                        IsSubMenu = true,
                        PostSelectAction = PostSelectAction.Nothing,
                        TextColor = Color.GreenYellow,
                        ParentMenu = parentMenu,
                        FontName = "Verdana Bold"
                    };
                    
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
                    
                    MenuAPI.OpenSubMenu(plugin, p, abilitiesMenu);
                });
            }
            
            // Smoke Color submenu
            if (hasSmokeColor)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.smokecolor"), (p, _) => {
                    var smokeColorMenu = new ScreenMenu(_localizer!.ForPlayer(viewer, "benefits.smokecolor.title"), plugin)
                    {
                        IsSubMenu = true,
                        PostSelectAction = PostSelectAction.Nothing,
                        TextColor = Color.DeepSkyBlue,
                        ParentMenu = parentMenu,
                        FontName = "Verdana Bold"
                    };
            
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
            
                    MenuAPI.OpenSubMenu(plugin, p, smokeColorMenu);
                });
            }
            
            CreateSpecialFeaturesSubmenu(plugin, parentMenu, viewer, services);
        }
        
        /// <summary>
        /// Renders grenade benefits to a menu
        /// </summary>
        private static void RenderGrenades(ScreenMenu menu, CCSPlayerController viewer, Service service, bool isDisabled)
        {
            var hasGrenades = service.HeAmount > 0 || service.FlashAmount > 0 || 
                             service.SmokeAmount > 0 || service.DecoyAmount > 0 || 
                             service.MolotovAmount > 0 || service.HealthshotAmount > 0;
                               
            if (!hasGrenades) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.header"), (_, _) => { }, disabled: true);
            
            if (service.HeAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.he", service.HeAmount), (_, _) => { }, disabled: isDisabled);
                
            if (service.FlashAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.flash", service.FlashAmount), (_, _) => { }, disabled: isDisabled);
                
            if (service.SmokeAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.smoke", service.SmokeAmount), (_, _) => { }, disabled: isDisabled);
                
            if (service.DecoyAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.decoy", service.DecoyAmount), (_, _) => { }, disabled: isDisabled);
                
            if (service.MolotovAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.molotov", service.MolotovAmount), (_, _) => { }, disabled: isDisabled);
                
            if (service.HealthshotAmount > 0)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.grenades.healthshot", service.HealthshotAmount), (_, _) => { }, disabled: isDisabled);
        }
        
        /// <summary>
        /// Renders special abilities to a menu
        /// </summary>
        private static void RenderAbilities(ScreenMenu menu, CCSPlayerController viewer, Service service, bool isDisabled)
        {
            var hasAbilities = service.PlayerExtraJumps > 0 || service.PlayerBunnyhop || service.WeaponMenu.Enabled;
            
            if (!hasAbilities) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.header"), (_, _) => { }, disabled: true);
            
            if (service.PlayerExtraJumps > 0)
            {
                var jumpType = service.PlayerExtraJumps == 1 
                    ? _localizer!.ForPlayer(viewer, "benefits.abilities.jump.double", service.PlayerExtraJumps + 1)
                    : _localizer!.ForPlayer(viewer, "benefits.abilities.jump.triple", service.PlayerExtraJumps + 1);
                    
                menu.AddOption(jumpType, (_, _) => { }, disabled: isDisabled);
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.jumpheight", service.PlayerExtraJumpHeight), (_, _) => { }, disabled: isDisabled);
            }
            
            if (service.PlayerBunnyhop)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.bhop"), (_, _) => { }, disabled: isDisabled);
                
            if (service.WeaponMenu.Enabled)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.abilities.weaponmenu"), (_, _) => { }, disabled: isDisabled);
        }
        
        /// <summary>
        /// Renders smoke color options to a menu
        /// </summary>
        private static void RenderSmokeColor(ScreenMenu menu, CCSPlayerController viewer, Service service, bool isDisabled)
        {
            if (!service.SmokeColorEnabled) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.header"), (_, _) => { }, disabled: true);
            
            if (service.SmokeColorRandom)
            {
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.random", service.Name), (_, _) => { }, disabled: isDisabled);
            }
            else
            {
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.smokecolor.custom", 
                    service.Name, service.SmokeColorR, service.SmokeColorG, service.SmokeColorB), (_, _) => { }, disabled: isDisabled);
            }
        }
        
        /// <summary>
        /// Renders new features to a menu
        /// </summary>
        private static void RenderSpecialFeatures(ScreenMenu menu, CCSPlayerController viewer, Service service, bool isDisabled)
        {
            var hasNewFeatures = service.InfiniteAmmo || service.FastReload || service.KillScreen;
            
            if (!hasNewFeatures) return;
            
            menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.header"), (_, _) => { }, disabled: true);
            
            if (service.InfiniteAmmo)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.infiniteammo"), (_, _) => { }, disabled: isDisabled);
            
            if (service.FastReload)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.fastreload"), (_, _) => { }, disabled: isDisabled);
                
            if (service.KillScreen)
                menu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.killscreen"), (_, _) => { }, disabled: isDisabled);
        }

        /// <summary>
        /// Creates a submenu for special features
        /// </summary>
        private static void CreateSpecialFeaturesSubmenu(
            MesharskyVip plugin,
            ScreenMenu parentMenu, 
            CCSPlayerController viewer, 
            List<Service> services)
        {
            var hasInfiniteAmmo = services.Any(s => s.InfiniteAmmo);
            var hasFastReload = services.Any(s => s.FastReload);
            var hasKillScreen = services.Any(s => s.KillScreen);
            
            var hasSpecialFeatures = hasInfiniteAmmo || hasFastReload || hasKillScreen;
            
            if (hasSpecialFeatures)
            {
                parentMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.menu.special"), (p, _) => {
                    var specialFeaturesMenu = new ScreenMenu(_localizer!.ForPlayer(viewer, "benefits.special.title"), plugin)
                    {
                        IsSubMenu = true,
                        PostSelectAction = PostSelectAction.Nothing,
                        TextColor = Color.HotPink,
                        ParentMenu = parentMenu,
                        FontName = "Verdana Bold"
                    };
                    
                    if (hasInfiniteAmmo)
                        specialFeaturesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.infiniteammo"), (_, _) => {});
                    
                    if (hasFastReload)
                        specialFeaturesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.fastreload"), (_, _) => {});
                        
                    if (hasKillScreen)
                        specialFeaturesMenu.AddOption(_localizer!.ForPlayer(viewer, "benefits.special.killscreen"), (_, _) => {});
                    
                    MenuAPI.OpenSubMenu(plugin, p, specialFeaturesMenu);
                });
            }
        }
    }
}