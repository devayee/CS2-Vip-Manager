using Tomlyn;
using Tomlyn.Model;

namespace Mesharsky_Vip;

#pragma warning disable 8601, 8604

public partial class MesharskyVip
{
    public static VipConfig? Config { get; private set; }

    public void LoadConfiguration()
    {
        var configPath = Path.Combine(ModuleDirectory, "Config/Configuration.toml");
        var configText = File.ReadAllText(configPath);
        var model = Toml.ToModel(configText);

        var dbTable = (TomlTable)model["DatabaseConnection"];
        var databaseConnection = new DatabaseConnectionConfig
        {
            Host = dbTable["host"].ToString(),
            Username = dbTable["username"].ToString(),
            Database = dbTable["database"].ToString(),
            Password = dbTable["password"].ToString(),
            Port = int.Parse(dbTable["port"].ToString())
        };

        var pluginTable = (TomlTable)model["PluginSettings"];
        var pluginSettings = new PluginSettingsConfig
        {
            PluginTag = pluginTable.TryGetValue("PluginTag", out var tag) ? tag.ToString() : "{red}[VIP]{default}",
            OnlineList = bool.Parse(pluginTable["online_list"].ToString()),
            BonusesList = bool.Parse(pluginTable["bonuses_list"].ToString()),
            BypassFlag = pluginTable["bypass_flag"].ToString(),
            BypassFlagGive = pluginTable["bypass_flag_give"].ToString()
        };
        
        var vipTestConfig = new VipTestConfig();
        if (model.TryGetValue("VipTest", out var vipTestTableObj) && vipTestTableObj is TomlTable vipTestTable)
        {
            if (vipTestTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                vipTestConfig.Enabled = enabled;
    
            if (vipTestTable.TryGetValue("test_group", out var testGroupObj) && testGroupObj is string testGroup)
                vipTestConfig.TestGroup = testGroup;
    
            if (vipTestTable.TryGetValue("test_duration", out var testDurationObj) && testDurationObj is long testDuration)
                vipTestConfig.TestDuration = (int)testDuration;
    
            if (vipTestTable.TryGetValue("test_cooldown", out var testCooldownObj) && testCooldownObj is long testCooldown)
                vipTestConfig.TestCooldown = (int)testCooldown;
    
            if (vipTestTable.TryGetValue("test_command", out var testCommandObj) && testCommandObj is TomlArray testCommandArray)
            {
                vipTestConfig.TestCommand = [.. testCommandArray.Cast<string>()];
            }
        }

        var nightVipConfig = new NightVipConfig();
        if (model.TryGetValue("NightVip", out var nightVipTableObj) && nightVipTableObj is TomlTable nightVipTable)
        {
            if (nightVipTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                nightVipConfig.Enabled = enabled;
            
            if (nightVipTable.TryGetValue("inherit_group", out var inheritGroupObj) && inheritGroupObj is string inheritGroup)
                nightVipConfig.InheritGroup = inheritGroup;
            
            if (nightVipTable.TryGetValue("flag", out var flagObj) && flagObj is string flag)
                nightVipConfig.Flag = flag;
            
            if (nightVipTable.TryGetValue("start_hour", out var startHourObj) && startHourObj is long startHour)
                nightVipConfig.StartHour = (int)startHour;
            
            if (nightVipTable.TryGetValue("end_hour", out var endHourObj) && endHourObj is long endHour)
                nightVipConfig.EndHour = (int)endHour;
        }
        // For backward compatibility with older configs
        else if (pluginTable.TryGetValue("night_vip", out var nightVipEnabledObj))
        {
            nightVipConfig.Enabled = bool.Parse(nightVipEnabledObj.ToString());
            
            if (pluginTable.TryGetValue("night_vip_flag", out var nightVipFlagObj))
                nightVipConfig.Flag = nightVipFlagObj.ToString();
            
            nightVipConfig.InheritGroup = "VIP";
            
            if (pluginTable.TryGetValue("night_vip_start_hour", out var startHourObj))
                nightVipConfig.StartHour = int.Parse(startHourObj.ToString());
            
            if (pluginTable.TryGetValue("night_vip_end_hours", out var endHourObj))
                nightVipConfig.EndHour = int.Parse(endHourObj.ToString());
        }

        var commandSettings = new CommandSettingsConfig();
        
        if (model.TryGetValue("CommandSettings", out var commandTableObj) && commandTableObj is TomlTable commandTable)
        {
            // Parse player commands
            if (commandTable.TryGetValue("vip_command", out var vipCmdObj) && vipCmdObj is TomlArray vipCmdArray)
            {
                commandSettings.VipCommand = [.. vipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("benefits_command", out var benefitsCmdObj) && benefitsCmdObj is TomlArray benefitsCmdArray)
            {
                commandSettings.BenefitsCommand = [.. benefitsCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("online_command", out var onlineCmdObj) && onlineCmdObj is TomlArray onlineCmdArray)
            {
                commandSettings.OnlineCommand = [.. onlineCmdArray.Cast<string>()];
            }
            
            // Parse admin commands
            if (commandTable.TryGetValue("addvip_command", out var addVipCmdObj) && addVipCmdObj is TomlArray addVipCmdArray)
            {
                commandSettings.AddVipCommand = [.. addVipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("removevip_command", out var removeVipCmdObj) && removeVipCmdObj is TomlArray removeVipCmdArray)
            {
                commandSettings.RemoveVipCommand = [.. removeVipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("listvip_command", out var listVipCmdObj) && listVipCmdObj is TomlArray listVipCmdArray)
            {
                commandSettings.ListVipCommand = [.. listVipCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("listavailable_command", out var listAvailableCmdObj) && listAvailableCmdObj is TomlArray listAvailableCmdArray)
            {
                commandSettings.ListAvailableCommand = [.. listAvailableCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("addvipsteam_command", out var addVipSteamCmdObj) && addVipSteamCmdObj is TomlArray addVipSteamCmdArray)
            {
                commandSettings.AddVipSteamCommand = [.. addVipSteamCmdArray.Cast<string>()];
            }
            
            if (commandTable.TryGetValue("removevipsteam_command", out var removeVipSteamCmdObj) && removeVipSteamCmdObj is TomlArray removeVipSteamCmdArray)
            {
                commandSettings.RemoveVipSteamCommand = [.. removeVipSteamCmdArray.Cast<string>()];
            }
        }

        var groupSettings = new List<GroupSettingsConfig>();
        var groupSettingsArray = (TomlTableArray)model["GroupSettings"];
        foreach (var group in groupSettingsArray)
        {
            var groupConfig = new GroupSettingsConfig
            {
                Name = group["name"].ToString(),
                Flag = group["flag"].ToString(),
                PlayerHp = int.Parse(group["player_hp"].ToString()),
                PlayerMaxHp = int.Parse(group["player_max_hp"].ToString()),
                PlayerVest = bool.Parse(group["player_vest"].ToString()),
                PlayerVestRound = int.Parse(group["player_vest_round"].ToString()),
                PlayerHelmet = bool.Parse(group["player_helmet"].ToString()),
                PlayerHelmetRound = int.Parse(group["player_helmet_round"].ToString()),
                PlayerDefuser = bool.Parse(group["player_defuser"].ToString()),
                HeAmount = int.Parse(group["he_amount"].ToString()),
                FlashAmount = int.Parse(group["flash_amount"].ToString()),
                SmokeAmount = int.Parse(group["smoke_amount"].ToString()),
                DecoyAmount = int.Parse(group["decoy_amount"].ToString()),
                MolotovAmount = int.Parse(group["molotov_amount"].ToString()),
                HealthshotAmount = int.Parse(group["healthshot_amount"].ToString()),
                PlayerExtraJumps = int.Parse(group["player_extra_jumps"].ToString()),
                PlayerExtraJumpHeight = double.Parse(group["player_extra_jump_height"].ToString()),
                PlayerBunnyhop = bool.Parse(group["player_bunnyhop"].ToString()),
                PlayerWeaponmenu = bool.Parse(group["player_weaponmenu"].ToString())
            };
            
            if (group.TryGetValue("smoke_color", out var smokeColorObj) && smokeColorObj is TomlTable smokeColorTable)
            {
                groupConfig.SmokeColor = new SmokeColorConfig();
    
                if (smokeColorTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                    groupConfig.SmokeColor.Enabled = enabled;
    
                if (smokeColorTable.TryGetValue("random", out var randomObj) && randomObj is bool random)
                    groupConfig.SmokeColor.Random = random;
    
                if (smokeColorTable.TryGetValue("red", out var redObj) && redObj is long red)
                    groupConfig.SmokeColor.Red = (int)red;
    
                if (smokeColorTable.TryGetValue("green", out var greenObj) && greenObj is long green)
                    groupConfig.SmokeColor.Green = (int)green;
    
                if (smokeColorTable.TryGetValue("blue", out var blueObj) && blueObj is long blue)
                    groupConfig.SmokeColor.Blue = (int)blue;
            }

            groupSettings.Add(groupConfig);

            var service = new Service
            {
                Name = groupConfig.Name,
                Flag = groupConfig.Flag,
                PlayerHp = groupConfig.PlayerHp,
                PlayerMaxHp = groupConfig.PlayerMaxHp,
                PlayerVest = groupConfig.PlayerVest,
                PlayerVestRound = groupConfig.PlayerVestRound,
                PlayerHelmet = groupConfig.PlayerHelmet,
                PlayerHelmetRound = groupConfig.PlayerHelmetRound,
                PlayerDefuser = groupConfig.PlayerDefuser,
                HeAmount = groupConfig.HeAmount,
                FlashAmount = groupConfig.FlashAmount,
                SmokeAmount = groupConfig.SmokeAmount,
                DecoyAmount = groupConfig.DecoyAmount,
                MolotovAmount = groupConfig.MolotovAmount,
                HealthshotAmount = groupConfig.HealthshotAmount,
                PlayerExtraJumps = groupConfig.PlayerExtraJumps,
                PlayerExtraJumpHeight = groupConfig.PlayerExtraJumpHeight,
                PlayerBunnyhop = groupConfig.PlayerBunnyhop,
                PlayerWeaponmenu = groupConfig.PlayerWeaponmenu,
                SmokeColorEnabled = groupConfig.SmokeColor.Enabled,
                SmokeColorRandom = groupConfig.SmokeColor.Random,
                SmokeColorR = groupConfig.SmokeColor.Red,
                SmokeColorG = groupConfig.SmokeColor.Green,
                SmokeColorB = groupConfig.SmokeColor.Blue
            };

            ServiceManager.RegisterService(service);
            Console.WriteLine($"[Mesharsky - VIP] Registered service: {service.Name}");
        }

        Config = new VipConfig
        { 
            DatabaseConnection = databaseConnection,
            PluginSettings = pluginSettings,
            CommandSettings = commandSettings,
            GroupSettings = groupSettings,
            NightVip = nightVipConfig,
            VipTest = vipTestConfig
        };
    }
}