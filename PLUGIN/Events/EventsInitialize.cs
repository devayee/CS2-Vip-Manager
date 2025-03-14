using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        private bool _roundStateStarted;

        private Timer? _onMapStart;

        private void LoadEvents()
        {
            Event_PlayerJoin();
            Event_PlayerLeave();
            Listener_OnMapStart();
            InitializeSmokeColor();
        }

        private void Listener_OnMapStart()
        {
            RegisterListener<Listeners.OnMapStart>((_) =>
            {
                _onMapStart?.Kill();
                _onMapStart = AddTimer(60, () =>
                {
                    foreach (var (key, cachedPlayer) in PlayerCache)
                    {
                        var player = Utilities.GetPlayerFromSteamId(key);
                        if (player == null)
                            continue;

                        if (IsNightVipTime())
                        {
                            if (cachedPlayer.LoadedGroup == null && !cachedPlayer.Active)
                            {
                                Console.WriteLine($"[Mesharsky - VIP] Assigning Night VIP to player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ].");
                                AssignNightVip(player);
                                ScheduleNightVipWelcomeMessage(player);
                            }
                            else
                                Console.WriteLine($"[Mesharsky - VIP] Player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ] already has an active service or Night VIP.");
                        }
                        else
                        {
                            var hasNightVip = cachedPlayer.LoadedGroup == Config!.NightVip.InheritGroup && 
                                              cachedPlayer.Active && 
                                              cachedPlayer.Groups.Any(g => g.GroupName == Config.NightVip.InheritGroup && g.ExpiryTime == 0);

                            if (!hasNightVip) continue;
                            Console.WriteLine($"[Mesharsky - VIP] Removing Night VIP from player [ Name: {player.PlayerName} - SteamID: {player.SteamID} ].");
                            RemoveNightVip(player);
                        }
                    }
                }, TimerFlags.REPEAT|TimerFlags.STOP_ON_MAPCHANGE);
            });
        }


        private void Event_PlayerJoin()
        {
            RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid || player.IsBot)
                    return HookResult.Continue;

                var steamId = player.SteamID;
                var playerName = player.PlayerName;

                if (!_databaseLoaded)
                {
                    LogDatabaseNotLoaded(player);
                    return HookResult.Continue;
                }

                var cachedPlayer = GetOrCreatePlayer(steamId, playerName);

                ProcessPlayerJoin(player, cachedPlayer);
                
                return HookResult.Continue;
            });
        }

        private void ProcessPlayerJoin(CCSPlayerController player, Player cachedPlayer)
        {
            var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
            
            // First process the normal VIP groups from database
            if (activeGroups.Count != 0)
            {
                foreach (var group in activeGroups)
                {
                    var service = ServiceManager.GetService(group.GroupName);
                    if (service != null)
                    {
                        AssignPlayerPermissions(player, cachedPlayer, service, group);
                    }
                }
            
                var primaryGroup = activeGroups.First();
                var primaryService = ServiceManager.GetService(primaryGroup.GroupName);

                if (primaryService == null) return;
            
                WelcomeMessageEveryone(player, primaryService, activeGroups.Count);
                ScheduleWelcomeMessage(player, activeGroups);
            }
            else 
            {
                var externalVipGroups = CheckExternalPermissions(player);
                
                if (externalVipGroups.Count > 0)
                {
                    // Apply VIP status without adding to database
                    foreach (var service in externalVipGroups)
                    {
                        ApplyExternalVipPermissions(player, cachedPlayer, service);
                    }
                    
                    var primaryService = externalVipGroups.First();
                    
                    // Show welcome messages
                    WelcomeMessageEveryone(player, primaryService, externalVipGroups.Count);
                    ScheduleExternalWelcomeMessage(player, externalVipGroups);
                }
                // Check for night VIP
                else if (Config!.NightVip.Enabled && IsNightVipTime() && !cachedPlayer.Active)
                {
                    AssignNightVip(player);
                    ScheduleNightVipWelcomeMessage(player);
                }
                else
                {
                    InvalidateCachedPlayer(cachedPlayer);
                    LogNoActiveService(player);
            
                    AddTimer(15.0f, () =>
                    {
                        if (!player.IsValid)
                            return;
                
                        ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
                        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.title", player.PlayerName);
                        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.info");
                        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.suggestion");
                        ChatHelper.PrintLocalizedChat(player, _localizer!, true, "vip.novip.welcome.website");
                        ChatHelper.PrintLocalizedChat(player, _localizer!, false, "global.divider");
                    });
                }
            }
        }

        private static void InvalidateCachedPlayer(Player cachedPlayer)
        {
            cachedPlayer.Active = false;
            cachedPlayer.LoadedGroup = null;
            
            foreach (var group in cachedPlayer.Groups)
            {
                group.Active = false;
            }
        }

        private void Event_PlayerLeave()
        {
            RegisterEventHandler((EventPlayerDisconnect @event, GameEventInfo info) =>
            {
                var playerController = @event.Userid;
                if (playerController == null || playerController.IsBot)
                    return HookResult.Continue;

                var steamId = playerController.SteamID;

                if (!PlayerCache.ContainsKey(steamId)) return HookResult.Continue;
                GoodbyeMessageEveryone(playerController);
                PlayerCache.TryRemove(steamId, out _);
                Console.WriteLine($"[Mesharsky - VIP] Removed player from cache [ SteamID: {steamId} ]");

                return HookResult.Continue;
            });
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            if (IsWarmup())
                return HookResult.Handled;

            _roundStateStarted = true;

            AddTimer(10.0f, () =>
            {
                _roundStateStarted = false;
            });

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if (IsWarmup())
                return HookResult.Continue;

            var player = @event.Userid;

            if (player == null)
                return HookResult.Continue;

            if (!IsValidPlayer(player))
                return HookResult.Continue;

            AddTimer(0.6f, () =>
            {
                if (!player.IsValid)
                    return;
                        
                var steamId = player.SteamID;

                if (!PlayerCache.TryGetValue(steamId, out var cachedPlayer))
                {
                    cachedPlayer = GetOrCreatePlayer(steamId, player.PlayerName);
                }

                var activeGroups = cachedPlayer.Groups.Where(g => g.Active).ToList();
        
                if (activeGroups.Count != 0)
                {
                    var activeServices = activeGroups
                        .Select(g => ServiceManager.GetService(g.GroupName))
                        .Where(s => s != null)
                        .ToList();
                
                    if (activeServices.Count != 0)
                    {
                        PlayerSpawn_CombineBonuses(player, cachedPlayer, activeServices!);
                    }
                }
                else
                {
                    var externalServices = CheckExternalPermissions(player);
                    if (externalServices.Count > 0)
                    {
                        PlayerSpawn_CombineBonuses(player, cachedPlayer, externalServices);
                    }
                }
            });

            return HookResult.Continue;
        }
    }
}