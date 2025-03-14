using MySqlConnector;
using Dapper;

namespace Mesharsky_Vip;

public partial class MesharskyVip
{
    private static void DB_LoadConnectionString()
    {
        var dbConfig = Config!.DatabaseConnection;
        _connectionString = $"server={dbConfig.Host};database={dbConfig.Database};user={dbConfig.Username};password={dbConfig.Password};port={dbConfig.Port};";
    }

    private void DB_CreateTables()
    {
        using var connection = new MySqlConnection(_connectionString);
        try
        {
            connection.Open();
            Console.WriteLine("Database connection opened successfully.");
            
            var legacyTableExists = false;
            try 
            {
                var legacyCheck = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'players'");
                legacyTableExists = legacyCheck > 0;
            }
            catch (Exception)
            {
                // Ignore - table doesn't exist
            }
            
            const string createPlayerGroupsTableSql = @"
                CREATE TABLE IF NOT EXISTS player_groups (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    steamid64 BIGINT NOT NULL,
                    group_name VARCHAR(50) NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    expires INT UNSIGNED NOT NULL,
                    UNIQUE INDEX unique_player_group (steamid64, group_name),
                    INDEX(steamid64)
                );
            ";
            
            connection.Execute(createPlayerGroupsTableSql);
            
            // Migrate data from legacy table if it exists
            if (legacyTableExists)
            {
                MigrateExistingData(connection);
            }
            
            _databaseLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database tables: {ex.Message}");
        }
    }

    private static void DB_CreateVipTestTable()
    {
        using var connection = new MySqlConnection(_connectionString);
        try
        {
            connection.Open();
            
            const string createVipTestTableSql = @"
                CREATE TABLE IF NOT EXISTS vip_test_history (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    steamid64 BIGINT NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    last_test_date INT UNSIGNED NOT NULL,
                    UNIQUE INDEX unique_player (steamid64)
                );
            ";
            
            connection.Execute(createVipTestTableSql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating vip_test_history table: {ex.Message}");
        }
    }
    
    private static void MigrateExistingData(MySqlConnection connection)
    {
        try
        {
            var playerGroupsCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM player_groups");
            
            var playersCount = 0;
            try
            {
                playersCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM players");
            }
            catch (Exception)
            {
                return;
            }

            if (playerGroupsCount != 0 || playersCount <= 0) return;
            Console.WriteLine("[Mesharsky - VIP] Migrating existing player data to new multi-group system...");
            
            try
            {
                const string migrateSql = @"
                    INSERT INTO player_groups (steamid64, group_name, name, expires)
                    SELECT steamid64, `group`, name, expires FROM players
                    WHERE `group` != 'None' AND `group` != ''
                ";
                    
                var rowsAffected = connection.Execute(migrateSql);
                Console.WriteLine($"[Mesharsky - VIP] Migration complete. {rowsAffected} player groups migrated.");
                
                connection.Execute("DROP TABLE IF EXISTS players");
                Console.WriteLine("[Mesharsky - VIP] Legacy table 'players' dropped after successful migration.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mesharsky - VIP] Error during data migration SQL execution: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mesharsky - VIP] Error during data migration: {ex.Message}");
        }
    }
    
    private bool HasUsedVipTest(ulong steamId)
    {
        if (Config!.VipTest.TestCooldown == 0)
        {
            // If cooldown is 0 (forever), just check if they've ever used it
            using var connection = new MySqlConnection(_connectionString);
            var count = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM vip_test_history WHERE steamid64 = @SteamID",
                new { SteamID = steamId }
            );
            
            return count > 0;
        }
        else
        {
            // Check if they've used it within the cooldown period
            using var connection = new MySqlConnection(_connectionString);
            var lastTestDate = connection.ExecuteScalar<int?>(
                "SELECT last_test_date FROM vip_test_history WHERE steamid64 = @SteamID",
                new { SteamID = steamId }
            );
            
            if (lastTestDate == null)
                return false;
            
            var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cooldownSeconds = Config.VipTest.TestCooldown * 86400; 
            
            return (currentTime - lastTestDate.Value) < cooldownSeconds;
        }
    }
    
    private int GetRemainingCooldownDays(ulong steamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        var lastTestDate = connection.ExecuteScalar<int?>(
            "SELECT last_test_date FROM vip_test_history WHERE steamid64 = @SteamID",
            new { SteamID = steamId }
        );
        
        if (lastTestDate == null)
            return 0;
        
        var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secondsElapsed = currentTime - lastTestDate.Value;
        var cooldownSeconds = Config!.VipTest.TestCooldown * 86400; 
        
        if (secondsElapsed >= cooldownSeconds)
            return 0;
        
        return (int)Math.Ceiling((cooldownSeconds - secondsElapsed) / 86400.0);
    }
    
    private void RecordVipTestUsage(ulong steamId, string playerName)
    {
        using var connection = new MySqlConnection(_connectionString);
        var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        connection.Execute(
            "INSERT INTO vip_test_history (steamid64, name, last_test_date) " +
            "VALUES (@SteamID, @Name, @Date) " +
            "ON DUPLICATE KEY UPDATE name = @Name, last_test_date = @Date",
            new { SteamID = steamId, Name = playerName, Date = currentTime }
        );
    }
}