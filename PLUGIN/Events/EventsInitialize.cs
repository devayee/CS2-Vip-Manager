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
            // Register map events
            RegisterMapEvents();
            
            // Register player events
            RegisterPlayerEvents();
            
            // Other initializations
            InitializeSmokeColor();
            RegisterKillScreenEvents();
            RegisterFastReloadEvents();
            RegisterInfiniteAmmoEvents();
            InitializeWeaponMenu();
        }
    }
}