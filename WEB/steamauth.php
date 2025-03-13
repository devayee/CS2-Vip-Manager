<?php

require_once 'Config/config.php';

/**
 * Get login URL for Steam authentication
 * 
 * @return string Login URL
 */
function get_steam_auth_url() {
    // Get the return URL (current URL)
    $protocol = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
    $return_url = $protocol . '://' . $_SERVER['HTTP_HOST'] . $_SERVER['PHP_SELF'];
    
    // Steam OpenID URL
    $steam_login_url = 'https://steamcommunity.com/openid/login';
    
    $params = array(
        'openid.ns'         => 'http://specs.openid.net/auth/2.0',
        'openid.mode'       => 'checkid_setup',
        'openid.return_to'  => $return_url,
        'openid.realm'      => $protocol . '://' . $_SERVER['HTTP_HOST'],
        'openid.identity'   => 'http://specs.openid.net/auth/2.0/identifier_select',
        'openid.claimed_id' => 'http://specs.openid.net/auth/2.0/identifier_select',
    );
    
    // Build login URL
    $login_url = $steam_login_url . '?' . http_build_query($params);
    
    error_log("Generated login URL: " . $login_url);
    
    return $login_url;
}

/**
 * Process Steam login
 * 
 * @return array User data if login successful, null if not
 */
function process_steam_login() {
    global $config;
    
    if (!isset($_GET['openid_claimed_id']) || !isset($_GET['openid_identity'])) {
        error_log("No OpenID claim found in request");
        return null;
    }
    
    if (!preg_match('#^https?://steamcommunity.com/openid/id/(\d+)$#', $_GET['openid_claimed_id'], $matches)) {
        error_log("Invalid Steam ID format in claimed_id: " . $_GET['openid_claimed_id']);
        return null;
    }
    
    $steamid = $matches[1];
    
    $_SESSION[$config['auth_session_name']] = $steamid;
    
    error_log("Steam ID extracted: $steamid");
    

    $steam_api_url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={$config['steam_api_key']}&steamids=$steamid";
    $response = @file_get_contents($steam_api_url);
    
    if ($response === false) {
        error_log("Error fetching Steam API data: " . error_get_last()['message']);
        return ['steamid' => $steamid];
    }
    
    $data = json_decode($response, true);
    
    if (isset($data['response']['players'][0])) {
        $player_data = $data['response']['players'][0];
        error_log("Successfully retrieved player data for $steamid: " . $player_data['personaname']);
        return [
            'steamid' => $steamid,
            'name' => $player_data['personaname'],
            'avatar' => $player_data['avatarfull'],
            'profileurl' => $player_data['profileurl']
        ];
    }
    
    return ['steamid' => $steamid];
}

/**
 * Get current authenticated steam user
 * 
 * @return array|null User data if authenticated, null if not
 */
function get_current_steam_user() {
    global $config;
    
    if (!isset($_SESSION[$config['auth_session_name']])) {
        return null;
    }
    
    $steamid = $_SESSION[$config['auth_session_name']];
    

    $steam_api_url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={$config['steam_api_key']}&steamids=$steamid";
    $response = @file_get_contents($steam_api_url);
    
    if ($response === false) {
        error_log("Error fetching Steam API data for current user: " . error_get_last()['message']);
        return ['steamid' => $steamid];
    }
    
    $data = json_decode($response, true);
    
    if (isset($data['response']['players'][0])) {
        $player_data = $data['response']['players'][0];
        return [
            'steamid' => $steamid,
            'name' => $player_data['personaname'],
            'avatar' => $player_data['avatarfull'],
            'profileurl' => $player_data['profileurl']
        ];
    }
    
    return ['steamid' => $steamid];
}

/**
 * Logout user
 */
function steam_logout() {
    global $config;
    
    unset($_SESSION[$config['auth_session_name']]);
    session_unset();
    session_destroy();
}