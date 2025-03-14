using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    public new class CommandManager(MesharskyVip plugin)
    {
        private readonly Dictionary<string, CommandRegistration> _commands = new();

        /// <summary>
        /// Registers a command with all its aliases from the config
        /// </summary>
        /// <param name="commandKey">The key in CommandSettings (e.g., "vip_command")</param>
        /// <param name="description">Command description</param>
        /// <param name="callback">Command callback method</param>
        /// <returns>True if the command was registered successfully</returns>
        public void RegisterCommand(string commandKey, string description, CommandInfo.CommandCallback callback)
        {
            if (Config == null)
            {
                Console.WriteLine($"[Mesharsky - VIP] WARNING: Cannot register command {commandKey} because config is not loaded");
                return;
            }

            List<string>? aliases = null;
            
            switch (commandKey.ToLower())
            {
                // Player commands
                case "vip_command":
                    aliases = Config.CommandSettings.VipCommand;
                    break;
                case "benefits_command":
                    aliases = Config.CommandSettings.BenefitsCommand;
                    break;
                case "online_command":
                    aliases = Config.CommandSettings.OnlineCommand;
                    break;
                    
                // Admin commands
                case "addvip_command":
                    aliases = Config.CommandSettings.AddVipCommand;
                    break;
                case "removevip_command":
                    aliases = Config.CommandSettings.RemoveVipCommand;
                    break;
                case "listvip_command":
                    aliases = Config.CommandSettings.ListVipCommand;
                    break;
                case "listavailable_command":
                    aliases = Config.CommandSettings.ListAvailableCommand;
                    break;
                case "addvipsteam_command":
                    aliases = Config.CommandSettings.AddVipSteamCommand;
                    break;
                case "removevipsteam_command":
                    aliases = Config.CommandSettings.RemoveVipSteamCommand;
                    break;
                case "viptest_command":
                    aliases = Config.VipTest.TestCommand;
                    break;
                    
                default:
                    Console.WriteLine($"[Mesharsky - VIP] WARNING: Unknown command key: {commandKey}");
                    return;
            }

            if (aliases.Count == 0)
            {
                Console.WriteLine($"[Mesharsky - VIP] WARNING: No aliases defined for command {commandKey}");
                return;
            }
            
            var registration = new CommandRegistration
            {
                CommandKey = commandKey,
                Description = description,
                Callback = callback,
                Aliases = aliases
            };
            
            _commands[commandKey] = registration;
            
            foreach (var alias in aliases)
            {
                var csgoAlias = alias.StartsWith("!") ? alias[1..] : alias;
                
                plugin.AddCommand(csgoAlias, description, callback);
                Console.WriteLine($"[Mesharsky - VIP] Registered command alias: {alias}");
            }
        }
        
        /// <summary>
        /// Unregisters a command and all its aliases
        /// </summary>
        /// <param name="commandKey">The key of the command to unregister</param>
        public void UnregisterCommand(string commandKey)
        {
            if (!_commands.TryGetValue(commandKey, out var registration))
            {
                Console.WriteLine($"[Mesharsky - VIP] WARNING: Cannot unregister command {commandKey} because it is not registered");
                return;
            }
            
            foreach (var alias in registration.Aliases)
            {
                var csgoAlias = alias.StartsWith("!") ? alias[1..] : alias;
                
                plugin.RemoveCommand(csgoAlias, registration.Callback);
                Console.WriteLine($"[Mesharsky - VIP] Unregistered command alias: {alias}");
            }
            
            _commands.Remove(commandKey);
            Console.WriteLine($"[Mesharsky - VIP] Command {commandKey} unregistered successfully");
        }
        
        /// <summary>
        /// Gets all registered commands
        /// </summary>
        public IReadOnlyDictionary<string, CommandRegistration> GetCommands()
        {
            return _commands;
        }
        
        /// <summary>
        /// Gets a registered command by its key
        /// </summary>
        public CommandRegistration? GetCommand(string commandKey)
        {
            return _commands.TryGetValue(commandKey, out var cmd) ? cmd : null;
        }
    }
    
    /// <summary>
    /// Represents a registered command with all its aliases
    /// </summary>
    public class CommandRegistration
    {
        public required string CommandKey { get; init; }
        public required string Description { get; init; }
        public required CommandInfo.CommandCallback Callback { get; init; }
        public required List<string> Aliases { get; init; }
    }
    
    private CommandManager? _commandManager;

    private CommandManager GetCommandManager()
    {
        return _commandManager ??= new CommandManager(this);
    }
}