namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class Service
    {
        public required string Name { get; set; }
        public required string Flag { get; set ;}
        
        // Health & Armor
        public int PlayerHp { get; set; }
        public int PlayerMaxHp { get; set; }
        public bool PlayerVest { get; set; }
        public int PlayerVestRound { get; set; }
        public bool PlayerHelmet { get; set; }
        public int PlayerHelmetRound { get; set; }
        public bool PlayerDefuser { get; set; }
        
        // Grenades
        public int HeAmount { get; set; }
        public int FlashAmount { get; set; }
        public int SmokeAmount { get; set; }
        public int DecoyAmount { get; set; }
        public int MolotovAmount { get; set; }
        public int HealthshotAmount { get; set; }
        
        // Special Abilities
        public int PlayerExtraJumps { get; set; }
        public double PlayerExtraJumpHeight { get; set; }
        public bool PlayerBunnyhop { get; set; }
        
        // Smoke Color
        public bool SmokeColorEnabled { get; set; } = false;
        public bool SmokeColorRandom { get; set; } = false;
        public int SmokeColorR { get; set; } = 255;
        public int SmokeColorG { get; set; } = 255;
        public int SmokeColorB { get; set; } = 255;
        
        // Integrated Features
        public bool InfiniteAmmo { get; set; } = false;
        public bool FastReload { get; set; } = false;
        public bool KillScreen { get; set; } = false;
        
        // Kill Health Bonuses
        public int HealthPerKill { get; set; } = 0;
        public int HealthPerHeadshot { get; set; } = 0;
        public int HealthPerKnifeKill { get; set; } = 0;
        public int HealthPerNoScope { get; set; } = 0;
        
        // Weapon Menu
        public WeaponMenuConfig WeaponMenu { get; set; } = new();
    }
}
