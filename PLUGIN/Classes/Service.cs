using System.Collections.Generic;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class Service
    {
        public required string Name { get; set; }
        public required string Flag { get; set ;}
        public int PlayerHp { get; set; }
        public int PlayerMaxHp { get; set; }
        public bool PlayerVest { get; set; }
        public int PlayerVestRound { get; set; }
        public bool PlayerHelmet { get; set; }
        public int PlayerHelmetRound { get; set; }
        public bool PlayerDefuser { get; set; }
        public int HeAmount { get; set; }
        public int FlashAmount { get; set; }
        public int SmokeAmount { get; set; }
        public int DecoyAmount { get; set; }
        public int MolotovAmount { get; set; }
        public int HealthshotAmount { get; set; }
        public int PlayerExtraJumps { get; set; }
        public double PlayerExtraJumpHeight { get; set; }
        public bool PlayerBunnyhop { get; set; }
        public bool PlayerWeaponmenu { get; set; }
    }
}
