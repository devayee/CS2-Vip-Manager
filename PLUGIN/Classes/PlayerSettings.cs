using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public class PlayerSettings
    {
        public int JumpsUsed { get; set; } = 0;
        public PlayerButtons LastButtons { get; set; }
        public PlayerFlags LastFlags { get; set; }
        public bool UsingExtraJump { get; set; }
    }
}