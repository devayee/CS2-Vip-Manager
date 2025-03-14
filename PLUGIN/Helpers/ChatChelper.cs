using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;

namespace Mesharsky_Vip;

public static class ChatHelper
{
    /// <summary>
    /// Prints a localized message to the player's chat.
    /// </summary>
    /// <param name="player">The CCSPlayerController to print to.</param>
    /// <param name="localizer">The IStringLocalizer instance</param>
    /// <param name="includePrefix">If true, the plugin prefix (PluginTag) is included as the first argument.</param>
    /// <param name="key">The translation key (as defined in language JSON file).</param>
    /// <param name="args">Any additional arguments to format the string.</param>
    public static void PrintLocalizedChat(CCSPlayerController player, IStringLocalizer localizer, bool includePrefix, string key, params object[] args)
    {
        if (!player.IsValid)
            return;

        if (MesharskyVip.Config == null)
        {
            Console.WriteLine($"[Mesharsky - VIP] ERROR: Config is NULL. Key: {key}");
            return;
        }

        var sanitizedArgs = args.Select(arg => arg).ToArray();

        if (includePrefix)
        {
            var prefix = MesharskyVip.Config.PluginSettings.PluginTag.ReplaceColorTags();
            sanitizedArgs = new object[] { prefix }.Concat(sanitizedArgs).ToArray();
        }

        try
        {
            var formattedMessage = localizer.ForPlayer(player, key, sanitizedArgs);
            player.PrintToChat(formattedMessage);
        }
        catch (FormatException fe)
        {
            Console.WriteLine($"[Mesharsky - VIP] ERROR: Formatting failed for key: {key} | Args: {string.Join(", ", sanitizedArgs)}");
            Console.WriteLine($"[Mesharsky - VIP] Exception: {fe.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] ERROR: Unexpected error for key: {key}");
            Console.WriteLine($"[Mesharsky - VIP] Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints a localized message to all players' chat.
    /// </summary>
    /// <param name="localizer">The IStringLocalizer instance</param>
    /// <param name="includePrefix">If true, the plugin prefix (PluginTag) is included as the first argument.</param>
    /// <param name="key">The translation key</param>
    /// <param name="args">Arguments for formatting</param>
    public static void PrintLocalizedChatToAll(IStringLocalizer localizer, bool includePrefix, string key, params object[] args)
    {
        if (MesharskyVip.Config == null)
        {
            Console.WriteLine($"[Mesharsky - VIP] ERROR: Config is NULL. Key: {key}");
            return;
        }

        var sanitizedArgs = args.Select(arg => arg).ToArray();

        try
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
            {
                var playerArgs = sanitizedArgs;
                
                if (includePrefix)
                {
                    var prefix = MesharskyVip.Config.PluginSettings.PluginTag.ReplaceColorTags();
                    playerArgs = new object[] { prefix }.Concat(sanitizedArgs).ToArray();
                }
                
                var formattedMessage = localizer.ForPlayer(player, key, playerArgs);
                player.PrintToChat(formattedMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] ERROR: Error printing localized message to all: {ex.Message}");
        }
    }
    
    
}