<?php
require_once 'Config/config.php';
require_once 'steam_api.php';
require_once 'groups_manager.php';

/**
 * Get all players with VIP groups with pagination
 * 
 * @param int $page Current page number
 * @param int $limit Items per page
 * @param bool $show_inactive Whether to include inactive players and players with no groups (default: false)
 * @return array List of players with VIP groups and pagination info
 */
function get_all_vip_players($page = 1, $limit = 25, $show_inactive = false) {
    $conn = get_db_connection();
    $offset = ($page - 1) * $limit;
    $now = time();
    
    if ($show_inactive) {
        // For inactive view, get all players including those with expired groups
        $count_sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups";
        $stmt = $conn->prepare($count_sql);
        $stmt->execute();
        $total_players = $stmt->fetchColumn();
        
        $sql = "SELECT DISTINCT steamid64, name FROM player_groups ORDER BY steamid64 LIMIT :limit OFFSET :offset";
        $stmt = $conn->prepare($sql);
        $stmt->bindParam(':limit', $limit, PDO::PARAM_INT);
        $stmt->bindParam(':offset', $offset, PDO::PARAM_INT);
        $stmt->execute();
    } else {
        // When showing only active players, just get those with active groups
        $count_sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups WHERE expires = 0 OR expires > :now";
        $stmt = $conn->prepare($count_sql);
        $stmt->bindParam(':now', $now);
        $stmt->execute();
        $total_players = $stmt->fetchColumn();
        
        $sql = "SELECT DISTINCT steamid64, name FROM player_groups 
                WHERE expires = 0 OR expires > :now 
                ORDER BY steamid64 
                LIMIT :limit OFFSET :offset";
        
        $stmt = $conn->prepare($sql);
        $stmt->bindParam(':now', $now);
        $stmt->bindParam(':limit', $limit, PDO::PARAM_INT);
        $stmt->bindParam(':offset', $offset, PDO::PARAM_INT);
        $stmt->execute();
    }
    
    $players = [];
    while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
        $steamid64 = $row['steamid64'];
        
        $player_info = get_steam_player_info($steamid64);
        
        $players[] = [
            'steamid64' => $steamid64,
            'name' => $row['name'] ?? $player_info['name'], // Use name from DB if available, otherwise from Steam API
            'avatar' => $player_info['avatar'],
            'profileurl' => $player_info['profileurl']
        ];
    }
    
    // Pagination info
    $total_pages = ceil($total_players / $limit);
    
    return [
        'players' => $players,
        'pagination' => [
            'total_players' => $total_players,
            'total_pages' => $total_pages,
            'current_page' => $page,
            'limit' => $limit
        ]
    ];
}

/**
 * Get player details by SteamID
 * 
 * @param string $steamid64 SteamID64 of the player
 * @return array Player details including Steam info
 */
function get_player_by_steamid($steamid64) {
    $conn = get_db_connection();
    
    // Try to get player name from player_groups table first
    $sql = "SELECT name FROM player_groups WHERE steamid64 = :steamid LIMIT 1";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':steamid', $steamid64);
    $stmt->execute();
    $player_row = $stmt->fetch(PDO::FETCH_ASSOC);
    
    // Get player info from Steam
    $player_info = get_steam_player_info($steamid64);
    
    return [
        'steamid64' => $steamid64,
        'name' => $player_row['name'] ?? $player_info['name'], // Use name from DB if available, otherwise from Steam API
        'avatar' => $player_info['avatar'],
        'profileurl' => $player_info['profileurl']
    ];
}

/**
 * Get all groups for a player
 * 
 * @param string $steamid64 SteamID64 of the player
 * @return array List of VIP groups the player has
 */
function get_player_groups($steamid64) {
    $conn = get_db_connection();
    
    $sql = "SELECT * FROM player_groups WHERE steamid64 = :steamid";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':steamid', $steamid64);
    $stmt->execute();
    
    return $stmt->fetchAll(PDO::FETCH_ASSOC);
}

/**
 * Get all groups for a player, both active and inactive
 * 
 * @param string $steamid64 SteamID64 of the player
 * @return array Associative array with 'active' and 'inactive' groups
 */
function get_player_groups_by_status($steamid64) {
    $conn = get_db_connection();
    $now = time();
    
    // Get all groups
    $sql = "SELECT * FROM player_groups WHERE steamid64 = :steamid";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':steamid', $steamid64);
    $stmt->execute();
    
    $groups = $stmt->fetchAll(PDO::FETCH_ASSOC);
    
    // Separate active and inactive groups
    $active_groups = [];
    $inactive_groups = [];
    
    foreach ($groups as $group) {
        if ($group['expires'] == 0 || $group['expires'] > $now) {
            $active_groups[] = $group;
        } else {
            $inactive_groups[] = $group;
        }
    }
    
    return [
        'active' => $active_groups,
        'inactive' => $inactive_groups
    ];
}

/**
 * Add a group to a player
 * 
 * @param string $steamid64 SteamID64 of the player
 * @param string $group_name Name of the VIP group
 * @param int $expiry_days Number of days until expiry (0 = permanent)
 * @param bool $extend_existing Whether to extend an existing group
 * @return array Result with success status and message
 */
function add_group_to_player($steamid64, $group_name, $expiry_days = 0, $extend_existing = false) {
    $conn = get_db_connection();
    
    // Get player name from Steam
    $player_info = get_steam_player_info($steamid64);
    $player_name = $player_info['name'];
    
    // Calculate expiry time (0 = permanent)
    $expiry_time = 0;
    if ($expiry_days > 0) {
        $expiry_time = time() + ($expiry_days * 86400);
    }
    
    // Check if player already has this group
    $check_sql = "SELECT * FROM player_groups WHERE steamid64 = :steamid AND group_name = :group_name";
    $check_stmt = $conn->prepare($check_sql);
    $check_stmt->bindParam(':steamid', $steamid64);
    $check_stmt->bindParam(':group_name', $group_name);
    $check_stmt->execute();
    
    $existing_group = $check_stmt->fetch(PDO::FETCH_ASSOC);
    
    if ($existing_group) {
        // If existing group has no expiry (permanent) and we're not forcing an extension
        if ($existing_group['expires'] == 0 && !$extend_existing) {
            return [
                'success' => false,
                'needs_confirmation' => true,
                'message' => "Player already has a permanent $group_name group.",
                'steamid' => $steamid64,
                'group_name' => $group_name
            ];
        }
        
        // If existing group expires later than new expiry and we're not forcing an extension
        if ($expiry_time != 0 && $existing_group['expires'] > $expiry_time && !$extend_existing) {
            $existing_expiry = date('Y-m-d H:i', $existing_group['expires']);
            $new_expiry = date('Y-m-d H:i', $expiry_time);
            
            return [
                'success' => false,
                'needs_confirmation' => true,
                'message' => "Player already has $group_name group that expires later ($existing_expiry) than the new expiry date ($new_expiry).",
                'steamid' => $steamid64,
                'group_name' => $group_name
            ];
        }
        
        // Calculate the new expiry time
        $new_expiry_time = $expiry_time;
        
        // If we're extending (adding days) and the existing expiry isn't permanent
        if ($extend_existing && $existing_group['expires'] > 0 && $expiry_days > 0) {
            $base_time = max(time(), $existing_group['expires']);
            $new_expiry_time = $base_time + ($expiry_days * 86400);
        } else if ($extend_existing && $expiry_days == 0) {
            $new_expiry_time = 0;
        } else if ($expiry_time == 0) {
            $new_expiry_time = 0;
        }
        
        // Update expiry time and player name
        $update_sql = "UPDATE player_groups SET expires = :expiry_time, name = :name 
                      WHERE steamid64 = :steamid AND group_name = :group_name";
        $update_stmt = $conn->prepare($update_sql);
        $update_stmt->bindParam(':expiry_time', $new_expiry_time);
        $update_stmt->bindParam(':name', $player_name);
        $update_stmt->bindParam(':steamid', $steamid64);
        $update_stmt->bindParam(':group_name', $group_name);
        
        if ($update_stmt->execute()) {
            if ($new_expiry_time == 0) {
                return [
                    'success' => true,
                    'message' => "Set $group_name group for player to permanent."
                ];
            } else if ($extend_existing && $existing_group['expires'] > 0) {
                return [
                    'success' => true,
                    'message' => "Extended $group_name group for player by $expiry_days days (until " . date('Y-m-d H:i', $new_expiry_time) . ")."
                ];
            } else {
                $expiry_text = $new_expiry_time == 0 ? "permanently" : "until " . date('Y-m-d H:i', $new_expiry_time);
                return [
                    'success' => true,
                    'message' => "Updated $group_name group for player $expiry_text."
                ];
            }
        } else {
            return [
                'success' => false,
                'message' => "Failed to update group expiry."
            ];
        }
    } else {
        $insert_sql = "INSERT INTO player_groups (steamid64, group_name, expires, name) 
                      VALUES (:steamid, :group_name, :expiry_time, :name)";
        $insert_stmt = $conn->prepare($insert_sql);
        $insert_stmt->bindParam(':steamid', $steamid64);
        $insert_stmt->bindParam(':group_name', $group_name);
        $insert_stmt->bindParam(':expiry_time', $expiry_time);
        $insert_stmt->bindParam(':name', $player_name);
        
        if ($insert_stmt->execute()) {
            $expiry_text = $expiry_time == 0 ? "permanently" : "until " . date('Y-m-d H:i', $expiry_time);
            return [
                'success' => true,
                'message' => "Added $group_name group to $player_name $expiry_text."
            ];
        } else {
            return [
                'success' => false,
                'message' => "Failed to add group to player."
            ];
        }
    }
}

/**
 * Update a player's existing group
 * 
 * @param string $steamid64 Player's SteamID64
 * @param string $group_name Group name
 * @param int $action 1=Add days, 2=Set days, 3=Set permanent
 * @param int $days Number of days to add or set (ignored for permanent)
 * @return array Result with success status and message
 */
function update_player_group($steamid64, $group_name, $action, $days = 0) {
    $conn = get_db_connection();
    
    // Check if the group exists
    $check_sql = "SELECT * FROM player_groups WHERE steamid64 = :steamid AND group_name = :group_name";
    $check_stmt = $conn->prepare($check_sql);
    $check_stmt->bindParam(':steamid', $steamid64);
    $check_stmt->bindParam(':group_name', $group_name);
    $check_stmt->execute();
    
    $existing_group = $check_stmt->fetch(PDO::FETCH_ASSOC);
    
    if (!$existing_group) {
        return [
            'success' => false,
            'message' => "Group not found for this player."
        ];
    }
    
    // Calculate new expiry time based on action
    $new_expiry_time = $existing_group['expires'];
    
    switch ($action) {
        case 1:
            if ($existing_group['expires'] == 0) {
                return [
                    'success' => false,
                    'message' => "Cannot add days to a permanent group."
                ];
            }
            
            // Add days from current expiry or current time, whichever is later
            $base_time = max(time(), $existing_group['expires']);
            $new_expiry_time = $base_time + ($days * 86400);
            $action_text = "Added $days days to";
            break;
            
        case 2: // Set specific days from now
            $new_expiry_time = time() + ($days * 86400);
            $action_text = "Set";
            break;
            
        case 3: // Set permanent
            $new_expiry_time = 0;
            $action_text = "Set";
            break;
            
        default:
            return [
                'success' => false,
                'message' => "Invalid action specified."
            ];
    }
    
    // Update the group
    $update_sql = "UPDATE player_groups SET expires = :expires WHERE steamid64 = :steamid AND group_name = :group_name";
    $update_stmt = $conn->prepare($update_sql);
    $update_stmt->bindParam(':expires', $new_expiry_time);
    $update_stmt->bindParam(':steamid', $steamid64);
    $update_stmt->bindParam(':group_name', $group_name);
    
    if ($update_stmt->execute()) {
        if ($new_expiry_time == 0) {
            return [
                'success' => true,
                'message' => "$action_text $group_name group to permanent."
            ];
        } else {
            return [
                'success' => true,
                'message' => "$action_text $group_name group to expire on " . date('Y-m-d H:i', $new_expiry_time) . "."
            ];
        }
    } else {
        return [
            'success' => false,
            'message' => "Failed to update group."
        ];
    }
}

/**
 * Update player name across all their groups
 * 
 * @param string $steamid64 SteamID64 of the player
 * @param string $name New name for the player
 * @return bool Success or failure
 */
function update_player_name($steamid64, $name) {
    $conn = get_db_connection();
    
    $sql = "UPDATE player_groups SET name = :name WHERE steamid64 = :steamid";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':name', $name);
    $stmt->bindParam(':steamid', $steamid64);
    
    return $stmt->execute();
}

/**
 * Remove a group from a player
 * 
 * @param string $steamid64 SteamID64 of the player
 * @param string $group_name Name of the VIP group
 * @return bool Success or failure
 */
function remove_group_from_player($steamid64, $group_name) {
    $conn = get_db_connection();
    
    $sql = "DELETE FROM player_groups WHERE steamid64 = :steamid AND group_name = :group_name";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':steamid', $steamid64);
    $stmt->bindParam(':group_name', $group_name);
    return $stmt->execute();
}

/**
 * Search players by name or steamid with pagination
 * 
 * @param string $search_term Search term for player name or SteamID
 * @param int $page Current page number
 * @param int $limit Items per page
 * @param bool $show_inactive Whether to include inactive players (default: false)
 * @return array List of matching players with pagination info
 */
function search_players($search_term, $page = 1, $limit = 25, $show_inactive = false) {
    $conn = get_db_connection();
    $offset = ($page - 1) * $limit;
    $players = [];
    $total_players = 0;
    $now = time();
    
    // First try to find by SteamID64
    if (is_numeric($search_term) && strlen($search_term) >= 7) {
        // For SteamID search
        if ($show_inactive) {
            $count_sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups WHERE steamid64 LIKE :search";
            $sql = "SELECT DISTINCT steamid64, name FROM player_groups WHERE steamid64 LIKE :search ORDER BY steamid64 LIMIT :limit OFFSET :offset";
        } else {
            $count_sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups WHERE steamid64 LIKE :search AND (expires = 0 OR expires > :now)";
            $sql = "SELECT DISTINCT steamid64, name FROM player_groups WHERE steamid64 LIKE :search AND (expires = 0 OR expires > :now) ORDER BY steamid64 LIMIT :limit OFFSET :offset";
        }
        
        $search_param = "%$search_term%";
        
        $count_stmt = $conn->prepare($count_sql);
        $count_stmt->bindParam(':search', $search_param);
        if (!$show_inactive) {
            $count_stmt->bindParam(':now', $now);
        }
        $count_stmt->execute();
        $total_players = $count_stmt->fetchColumn();
        
        $stmt = $conn->prepare($sql);
        $stmt->bindParam(':search', $search_param);
        if (!$show_inactive) {
            $stmt->bindParam(':now', $now);
        }
        $stmt->bindParam(':limit', $limit, PDO::PARAM_INT);
        $stmt->bindParam(':offset', $offset, PDO::PARAM_INT);
        $stmt->execute();
        
        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $player_info = get_steam_player_info($row['steamid64']);
            $players[] = [
                'steamid64' => $row['steamid64'],
                'name' => $row['name'] ?? $player_info['name'],
                'avatar' => $player_info['avatar'],
                'profileurl' => $player_info['profileurl']
            ];
        }
    } else {
        // For name search
        if ($show_inactive) {
            $sql = "SELECT DISTINCT steamid64, name FROM player_groups WHERE name LIKE :search";
        } else {
            $sql = "SELECT DISTINCT steamid64, name FROM player_groups WHERE name LIKE :search AND (expires = 0 OR expires > :now)";
        }
        
        $search_param = "%$search_term%";
        
        $stmt = $conn->prepare($sql);
        $stmt->bindParam(':search', $search_param);
        if (!$show_inactive) {
            $stmt->bindParam(':now', $now);
        }
        $stmt->execute();
        
        $all_players = [];
        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $player_info = get_steam_player_info($row['steamid64']);
            $all_players[] = [
                'steamid64' => $row['steamid64'],
                'name' => $row['name'] ?? $player_info['name'],
                'avatar' => $player_info['avatar'],
                'profileurl' => $player_info['profileurl']
            ];
        }
        
        // Manual pagination for name search
        $total_players = count($all_players);
        $players = array_slice($all_players, $offset, $limit);
    }
    
    // Pagination info
    $total_pages = ceil($total_players / $limit);
    
    return [
        'players' => $players,
        'pagination' => [
            'total_players' => $total_players,
            'total_pages' => $total_pages,
            'current_page' => $page,
            'limit' => $limit
        ]
    ];
}

/**
 * Get stats about VIP usage
 * 
 * @return array Various statistics about VIP usage
 */
function get_vip_stats() {
    $conn = get_db_connection();
    $now = time();
    
    $stats = [
        'total_players' => 0,
        'active_vips' => 0,
        'expiring_soon' => 0,
        'permanent_vips' => 0,
        'inactive_players' => 0,
        'players_no_groups' => 0  // This will always be 0 since we only track players with groups
    ];
    
    // Total players with groups
    $sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups";
    $stmt = $conn->prepare($sql);
    $stmt->execute();
    $stats['total_players'] = $stmt->fetchColumn();
    
    // Active VIPs (players with at least one active group)
    $sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups 
            WHERE expires = 0 OR expires > :now";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':now', $now);
    $stmt->execute();
    $stats['active_vips'] = $stmt->fetchColumn();
    
    // Inactive players (have groups but all expired)
    $sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups 
            WHERE steamid64 NOT IN (
                SELECT DISTINCT steamid64 FROM player_groups 
                WHERE expires = 0 OR expires > :now
            )";
    $stmt = $conn->prepare($sql);
    $stmt->bindParam(':now', $now);
    $stmt->execute();
    $stats['inactive_players'] = $stmt->fetchColumn();
    
    // Expiring in next 7 days
    $sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups 
            WHERE expires > :now AND expires < :week";
    $stmt = $conn->prepare($sql);
    $week = time() + (7 * 86400);
    $stmt->bindParam(':now', $now);
    $stmt->bindParam(':week', $week);
    $stmt->execute();
    $stats['expiring_soon'] = $stmt->fetchColumn();
    
    // Permanent VIPs
    $sql = "SELECT COUNT(DISTINCT steamid64) FROM player_groups WHERE expires = 0";
    $stmt = $conn->prepare($sql);
    $stmt->execute();
    $stats['permanent_vips'] = $stmt->fetchColumn();
    
    return $stats;
}