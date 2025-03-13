<?php
/**
 * Configuration file for VIP Manager
 */

// Database connection settings (SAME AS IN THE PLUGIN, DONT BE A DIPSHIT PLEASE)
$config = [
    'db_host' => '',
    'db_name' => '',
    'db_user' => '',
    'db_pass' => '',
    
    // Steam API settings
    'steam_api_key' => '', // You can get it from: https://steamcommunity.com/dev/apikey
    
    // Cache settings
    'cache_duration' => 86400,
    
    // Admin users who have access to the panel
    'admins' => [
        '76561198380337444', // Replace with your SteamID64
        '76561198100544531',
    ],
    
    // Authentication settings
    'auth_session_name' => 'vip_manager_auth',
    'steam_login_domain' => 'example.com', // Replace with your domain (Do not include https://)
];

// Don't modify anything below this line
// ----------------------------------------

// Create and initialize session if not already started
if (session_status() === PHP_SESSION_NONE) {
    session_start();
}

/**
 * Get database connection using the config settings
 * 
 * @return PDO Database connection
 */
function get_db_connection() {
    global $config;
    
    try {
        $conn = new PDO("mysql:host={$config['db_host']};dbname={$config['db_name']}", $config['db_user'], $config['db_pass']);

        $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        return $conn;
    } catch(PDOException $e) {
        die("Connection failed: " . $e->getMessage());
    }
}

/**
 * Check if user is authenticated and has admin rights
 * 
 * @return bool True if user is authenticated and has admin rights
 */
function is_admin() {
    global $config;
    
    if (!isset($_SESSION[$config['auth_session_name']])) {
        return false;
    }
    
    $steamid = $_SESSION[$config['auth_session_name']];
    return in_array($steamid, $config['admins']);
}

function require_admin() {
    if (!is_admin()) {
        header('Location: login.php');
        exit;
    }
}

function format_time_remaining($expiry_time) {
    if ($expiry_time == 0) {
        return "Never";
    }
    
    $now = time();
    $remaining = $expiry_time - $now;
    
    if ($remaining <= 0) {
        return "Expired";
    }
    
    $days = floor($remaining / 86400);
    $hours = floor(($remaining % 86400) / 3600);
    
    if ($days > 0) {
        return "$days days, $hours hours";
    } else {
        return "$hours hours";
    }
}

function format_expiry_date($expiry_time) {
    if ($expiry_time == 0) {
        return "Never";
    }
    
    return date("Y-m-d H:i", $expiry_time);
}