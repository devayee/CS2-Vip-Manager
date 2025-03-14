namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private void RegisterPlayerCommands()
    {
        var commandManager = GetCommandManager();
        
        commandManager.RegisterCommand("vip_command", "Show your VIP groups", cmd_ShowVipGroups);
        commandManager.RegisterCommand("benefits_command", "Show your VIP benefits", cmd_ShowVipBenefits);
        commandManager.RegisterCommand("online_command", "Show online VIP players", cmd_ShowOnlineVips);
        commandManager.RegisterCommand("viptest_command", "Test VIP features for a limited time", cmd_VipTest);
        
        Console.WriteLine("[Mesharsky - VIP] Player commands registered");
    }
}