using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Mesharsky_Vip
{
    public partial class MesharskyVip
    {
        private Timer? _onMapStart;

        private void LoadEvents()
        {
            RegisterMapEvents();
            
            RegisterPlayerEvents();
            
            InitializeSmokeColor();
            RegisterPlayerDeathEvent();
            RegisterFastReloadEvents();
            RegisterInfiniteAmmoEvents();
            InitializeWeaponMenu();
        }
    }
}