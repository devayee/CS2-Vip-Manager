<?php

require_once 'Config/config.php';

/**
 * Get player information from Steam API with caching
 * 
 * @param string $steamid64 The SteamID64 of the player
 * @return array Player information with name and avatar
 */
function get_steam_player_info($steamid64) {
    global $config;
    
    $default_player = [
        'name' => 'Unknown Player',
        'avatar' => 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg', // Default Steam avatar
        'profileurl' => '',
    ];
    
    if (empty($steamid64)) {
        return $default_player;
    }
    
    // Check cache first
    $cached_data = get_cached_steam_player_info($steamid64);
    if ($cached_data !== null) {
        return $cached_data;
    }
    
    if (empty($config['steam_api_key']) || $config['steam_api_key'] === 'YOUR_STEAM_API_KEY') {
        $db_player = get_player_name_from_db($steamid64);
        if ($db_player) {
            $player_info = [
                'name' => $db_player,
                'avatar' => $default_player['avatar'],
                'profileurl' => "https://steamcommunity.com/profiles/$steamid64",
            ];
            cache_steam_player_info($steamid64, $player_info);
            return $player_info;
        }
        
        error_log('Steam API key not configured. Using default player info.');
        return $default_player;
    }
    
    $api_url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" . $config['steam_api_key'] . "&steamids=" . $steamid64;
    
    try {
        $response = file_get_contents($api_url);
        if ($response === false) {
            throw new Exception('Failed to get response from Steam API');
        }
        
        $data = json_decode($response, true);
        
        if (!isset($data['response']['players'][0])) {
            throw new Exception('No player data returned from Steam API');
        }
        
        $player_data = $data['response']['players'][0];
        
        $player_info = [
            'name' => $player_data['personaname'] ?? $default_player['name'],
            'avatar' => $player_data['avatarfull'] ?? $default_player['avatar'],
            'profileurl' => $player_data['profileurl'] ?? "https://steamcommunity.com/profiles/$steamid64",
        ];
        
        // Cache the result
        cache_steam_player_info($steamid64, $player_info);
        
        return $player_info;
    } catch (Exception $e) {
        error_log('Steam API error: ' . $e->getMessage());
        
        $db_player = get_player_name_from_db($steamid64);
        if ($db_player) {
            $player_info = [
                'name' => $db_player,
                'avatar' => $default_player['avatar'],
                'profileurl' => "https://steamcommunity.com/profiles/$steamid64",
            ];
            return $player_info;
        }
        
        return $default_player;
    }
}


/**
 * Get player name from SteamID64
 * 
 * @param string $steamid64 The SteamID64 of the player
 * @return string The player's name
 */
function get_steam_player_name($steamid64) {
    $player_info = get_steam_player_info($steamid64);
    return $player_info['name'];
}

/**
 * Get player avatar from SteamID64
 * 
 * @param string $steamid64 The SteamID64 of the player
 * @return string URL to the player's avatar
 */
function get_steam_player_avatar($steamid64) {
    $player_info = get_steam_player_info($steamid64);
    return $player_info['avatar'];
}

/**
 * Get Steam profile URL from SteamID64
 * 
 * @param string $steamid64 The SteamID64 of the player
 * @return string The player's Steam profile URL
 */
function get_steam_profile_url($steamid64) {
    $player_info = get_steam_player_info($steamid64);
    return $player_info['profileurl'];
}

/**
 * Cache Steam player info to reduce API calls
 * Implements file-based cache
 * 
 * @param string $steamid64 The SteamID64 of the player
 * @param array $player_info The player info to cache
 * @return bool Success or failure
 */
function cache_steam_player_info($steamid64, $player_info) {
    $cache_dir = __DIR__ . '/cache';
    
    // Create cache directory if it doesn't exist
    if (!file_exists($cache_dir)) {
        mkdir($cache_dir, 0755, true);
    }
    
    $cache_file = $cache_dir . '/steam_' . $steamid64 . '.json';
    $cache_data = [
        'player_info' => $player_info,
        'timestamp' => time(),
    ];
    
    return file_put_contents($cache_file, json_encode($cache_data)) !== false;
}

/**
 * Get cached Steam player info
 * 
 * @param string $steamid64 The SteamID64 of the player
 * @return array|null The cached player info or null if not found/expired
 */
function get_cached_steam_player_info($steamid64) {
    global $config;
    $cache_file = __DIR__ . '/cache/steam_' . $steamid64 . '.json';
    
    if (!file_exists($cache_file)) {
        return null;
    }
    
    $cache_data = json_decode(file_get_contents($cache_file), true);
    $cache_duration = $config['cache_duration'] ?? 86400; // Default to 24 hours if not set
    
    // Check if cache is expired
    if (!$cache_data || time() - $cache_data['timestamp'] > $cache_duration) {
        return null;
    }
    
    return $cache_data['player_info'];
}

/**
 * Batch retrieve player info for multiple SteamIDs
 * More efficient than individual API calls
 * 
 * @param array $steamids Array of SteamID64s
 * @return array Array of player info indexed by SteamID64
 */
function get_steam_players_batch($steamids) {
    global $config;
    
    if (empty($steamids)) {
        return [];
    }
    
    $result = [];
    $to_fetch = [];
    
    // Check cache first for each player
    foreach ($steamids as $steamid) {
        $cached = get_cached_steam_player_info($steamid);
        if ($cached !== null) {
            $result[$steamid] = $cached;
        } else {
            $to_fetch[] = $steamid;
        }
    }
    
    // If all players were in cache, return early
    if (empty($to_fetch)) {
        return $result;
    }
    
    // Check if we have a valid Steam API key
    if (empty($config['steam_api_key']) || $config['steam_api_key'] === 'YOUR_STEAM_API_KEY') {
        // Use database as fallback
        foreach ($to_fetch as $steamid) {
            $db_name = get_player_name_from_db($steamid);
            $result[$steamid] = [
                'name' => $db_name ?: 'Unknown Player',
                'avatar' => 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg',
                'profileurl' => "https://steamcommunity.com/profiles/$steamid",
            ];
            cache_steam_player_info($steamid, $result[$steamid]);
        }
        return $result;
    }
    
    // Steam API can handle up to 100 SteamIDs in one call
    // Split into chunks if needed
    $chunks = array_chunk($to_fetch, 100);
    
    foreach ($chunks as $chunk) {
        $steamids_str = implode(',', $chunk);
        $api_url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" . $config['steam_api_key'] . "&steamids=" . $steamids_str;
        
        try {
            $response = file_get_contents($api_url);
            if ($response === false) {
                throw new Exception('Failed to get response from Steam API');
            }
            
            $data = json_decode($response, true);
            
            if (!isset($data['response']['players'])) {
                throw new Exception('Invalid response from Steam API');
            }
            
            foreach ($data['response']['players'] as $player) {
                $steamid = $player['steamid'];
                $player_info = [
                    'name' => $player['personaname'] ?? 'Unknown Player',
                    'avatar' => $player['avatarfull'] ?? 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg',
                    'profileurl' => $player['profileurl'] ?? "https://steamcommunity.com/profiles/$steamid",
                ];
                
                $result[$steamid] = $player_info;
                cache_steam_player_info($steamid, $player_info);
            }
            
            // For any requested IDs that weren't in the response
            foreach ($chunk as $steamid) {
                if (!isset($result[$steamid])) {
                    $db_name = get_player_name_from_db($steamid);
                    $result[$steamid] = [
                        'name' => $db_name ?: 'Unknown Player',
                        'avatar' => 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg',
                        'profileurl' => "https://steamcommunity.com/profiles/$steamid",
                    ];
                    cache_steam_player_info($steamid, $result[$steamid]);
                }
            }
        } catch (Exception $e) {
            error_log('Steam API batch error: ' . $e->getMessage());
            
            foreach ($chunk as $steamid) {
                if (!isset($result[$steamid])) {
                    $db_name = get_player_name_from_db($steamid);
                    $result[$steamid] = [
                        'name' => $db_name ?: 'Unknown Player',
                        'avatar' => 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg',
                        'profileurl' => "https://steamcommunity.com/profiles/$steamid",
                    ];
                }
            }
        }
    }
    
    return $result;
}