using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            if (IsWarmup())
                return HookResult.Handled;

            _roundStateStarted = true;
            
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
            if (gameRules != null)
                Console.WriteLine($"[Mesharsky - VIP] Round started: " +
                                  $"TotalRounds={gameRules.TotalRoundsPlayed}, " +
                                  $"IsPistol={IsPistolRound()}, " +
                                  $"EffectiveRound={GetEffectiveRoundNumber()}");

            AddTimer(10.0f, () =>
            {
                _roundStateStarted = false;
            });

            return HookResult.Continue;
        }
    }
}