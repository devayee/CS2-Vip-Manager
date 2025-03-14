<?php

require_once 'Config/config.php';
require_once 'steamauth.php';

if (is_admin()) {
    header('Location: index.php');
    exit;
}

// Process login
$login_error = null;
if (isset($_GET['openid_claimed_id'])) {
    error_log("Processing Steam login: " . json_encode($_GET));
    $user = process_steam_login();
    if ($user) {
        error_log("Steam login success: " . json_encode($user));
        if (in_array($user['steamid'], $config['admins'])) {
            header('Location: index.php');
            exit;
        } else {
            error_log("Not an admin: " . $user['steamid']);
            steam_logout();
            $login_error = "You don't have permission to access this panel. SteamID: " . $user['steamid'];
        }
    } else {
        // Login failed
        error_log("Steam login failed");
        $login_error = "Steam authentication failed. Please try again.";
    }
}

$login_url = get_steam_auth_url();
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Login - VIP Manager</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <style>
        .btn-steam {
            transition: all 0.3s ease;
        }
        .btn-steam:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.3);
        }
    </style>
</head>
<body class="bg-gray-900 text-gray-200 min-h-screen flex flex-col items-center justify-center relative">
    <div class="max-w-md w-full bg-gray-800 rounded-lg p-8 shadow-lg">
        <div class="text-center mb-8">
            <h1 class="text-3xl font-bold text-blue-400 mb-2">CS2 VIP Manager</h1>
            <p class="text-gray-400">Please login with Steam to continue</p>
        </div>
        
        <?php if ($login_error): ?>
            <div class="bg-red-800 text-white px-4 py-3 rounded-lg mb-6">
                <?= htmlspecialchars($login_error) ?>
            </div>
        <?php endif; ?>
        
        <div class="flex justify-center">
            <a href="<?= htmlspecialchars($login_url) ?>" class="px-6 py-3 bg-gray-700 text-white rounded-md hover:bg-gray-600 flex items-center btn-steam">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mr-2" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M12 2C6.48 2 2 6.48 2 12c0 5.52 4.48 10 10 10s10-4.48 10-10C22 6.48 17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8 0-4.41 3.59-8 8-8s8 3.59 8 8c0 4.41-3.59 8-8 8z"/>
                    <path d="M11 10c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm6-5H7v2h10V5z"/>
                </svg>
                Login with Steam
            </a>
        </div>
        
        <div class="mt-8 text-center text-sm text-gray-500">
            <p>You need admin permissions to access this panel.</p>
            <p>Please contact the administrator if you need access.</p>
        </div>
    </div>

    <footer class="mt-12 py-6 bg-gradient-to-b from-gray-900/90 to-gray-900 text-gray-300 border-t border-gray-800">
        <div class="container mx-auto px-4">
            <div class="flex flex-col sm:flex-row justify-between items-center">
                <div class="mb-4 sm:mb-0">
                    <p>&copy; <?= date('Y') ?> CS2 VIP Manager by Mesharsky</p>
                </div>
                
                <div class="flex space-x-4">
                    <a href="https://github.com/Mesharsky" class="text-gray-400 hover:text-white transition-colors duration-200" aria-label="GitHub">
                        <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path fill-rule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clip-rule="evenodd"></path>
                        </svg>
                    </a>
                    
                    <a href="https://paypal.me/mesharskyh2k" class="text-gray-400 hover:text-white transition-colors duration-200" aria-label="PayPal Donation">
                        <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path d="M7.076 21.337H2.47a.641.641 0 0 1-.633-.74L4.944 3.72a.78.78 0 0 1 .771-.667h6.269c2.921 0 4.979 1.98 4.576 4.852-.537 3.832-3.73 5.961-7.646 5.961h-2.37l-1.04 7.468a.63.63 0 0 1-.624.538h-2.04"/>
                            <path d="M18.836 9.894c.135-.945.097-1.601-.144-2.171-.478-1.184-1.67-1.839-3.38-1.839h-4.773a.646.646 0 0 0-.636.522l-2.694 17.06a.647.647 0 0 0 .636.77h2.239a.646.646 0 0 0 .636-.522l.723-4.578a.646.646 0 0 1 .636-.523h1.497c3.264 0 5.396-1.402 6.022-4.962.308-1.85.011-3.362-1.001-4.314.082.016.165.036.247.064a3.57 3.57 0 0 1 .896 1.038c.42.597.573 1.352.425 2.257l-.329 2.198z"/>
                        </svg>
                    </a>
                </div>
            </div>
        </div>
    </footer>
</body>
</html>