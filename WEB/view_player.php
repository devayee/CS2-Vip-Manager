<?php
require_once 'Config/config.php';
require_once 'player_functions.php';
require_once 'steam_api.php';
require_once 'groups_manager.php';

require_admin();

// Get the requested player
$steamid = $_GET['steamid'] ?? '';
if (!$steamid) {
    // Redirect to main page if no steamid provided
    header('Location: index.php');
    exit;
}

// Process form submissions
if ($_SERVER["REQUEST_METHOD"] == "POST") {
    if (isset($_POST['action']) && $_POST['action'] === 'delete_player') {
        // Delete player functionality
        $conn = get_db_connection();
        
        try {
            $conn->beginTransaction();
            
            $delete_groups = $conn->prepare("DELETE FROM player_groups WHERE steamid64 = :steamid");
            $delete_groups->bindParam(':steamid', $steamid);
            $delete_groups->execute();
            
            $conn->commit();
            
            header('Location: index.php?message=player_deleted');
            exit;
        } catch (PDOException $e) {
            $conn->rollBack();
            $error_message = "Failed to delete player: " . $e->getMessage();
        }
    }
}

$player = get_player_by_steamid($steamid);

$groups_by_status = get_player_groups_by_status($steamid);
$active_groups = $groups_by_status['active'];
$inactive_groups = $groups_by_status['inactive'];

$available_groups = get_available_groups();
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Player Profile - VIP Manager</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        dark: {
                            900: '#0f172a',
                            800: '#1e293b',
                            700: '#334155',
                            600: '#475569',
                            500: '#64748b'
                        }
                    },
                    boxShadow: {
                        glow: '0 0 15px rgba(59, 130, 246, 0.5)'
                    }
                }
            }
        }
    </script>
    <style>
        
        ::-webkit-scrollbar {
            width: 8px;
            height: 8px;
        }
        
        ::-webkit-scrollbar-track {
            background: rgba(30, 41, 59, 0.5);
            border-radius: 4px;
        }
        
        ::-webkit-scrollbar-thumb {
            background: rgba(100, 116, 139, 0.5);
            border-radius: 4px;
        }
        
        ::-webkit-scrollbar-thumb:hover {
            background: rgba(100, 116, 139, 0.8);
        }
    </style>
</head>
<body class="bg-gradient-to-br from-dark-900 to-dark-800 text-gray-200 min-h-screen">
    <div class="container mx-auto px-4 py-8">
        <header class="mb-10">
            <div class="flex flex-col md:flex-row justify-between items-center mb-6 gap-4">
                <h1 class="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
                    <i class="fas fa-user mr-3"></i>Player Profile
                </h1>
                <div class="flex flex-wrap gap-2">
                    <a href="edit_player.php?steamid=<?= $steamid ?>" 
                       class="px-4 py-2 bg-gradient-to-r from-yellow-600 to-orange-600 text-white rounded-lg hover:from-yellow-500 hover:to-orange-500 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                        <i class="fas fa-edit mr-2"></i>Edit Player
                    </a>
                    <a href="index.php" 
                       class="px-4 py-2 bg-gradient-to-r from-gray-700 to-gray-600 text-white rounded-lg hover:from-gray-600 hover:to-gray-500 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                        <i class="fas fa-chevron-left mr-2"></i>Back to Dashboard
                    </a>
                </div>
            </div>
        </header>

        <?php if (isset($error_message)): ?>
            <div 
                x-data="{ show: true }"
                x-show="show"
                x-transition:enter="transition ease-out duration-300"
                x-transition:enter-start="opacity-0 transform -translate-y-2"
                x-transition:enter-end="opacity-100 transform translate-y-0"
                x-transition:leave="transition ease-in duration-300"
                x-transition:leave-start="opacity-100 transform translate-y-0"
                x-transition:leave-end="opacity-0 transform -translate-y-2"
                class="bg-red-800/50 text-white px-6 py-4 rounded-xl mb-6 flex items-center justify-between border border-red-500/30 shadow-lg"
            >
                <div class="flex items-center">
                    <i class="fas fa-exclamation-circle mr-3 text-xl text-red-400"></i>
                    <?= htmlspecialchars($error_message) ?>
                </div>
                <button @click="show = false" class="text-white hover:text-red-200">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        <?php endif; ?>

        <!-- Player Profile Card -->
        <div class="bg-gradient-to-b from-dark-800/80 to-dark-900/80 rounded-2xl p-8 shadow-2xl mb-8 border border-blue-500/20 backdrop-blur-sm relative overflow-hidden transition-all duration-300 hover:shadow-blue-500/10 group">
            
            <div class="absolute -top-24 -right-24 w-48 h-48 bg-blue-500/10 rounded-full blur-2xl group-hover:bg-blue-500/20 transition-all duration-700"></div>
            <div class="absolute -bottom-16 -left-16 w-32 h-32 bg-purple-500/10 rounded-full blur-2xl group-hover:bg-purple-500/20 transition-all duration-700"></div>
            
            <div class="flex flex-col md:flex-row items-center md:items-start gap-8 relative">
                <!-- Player Avatar -->
                <div class="flex-shrink-0">
                    <div class="relative">
                        <a href="<?= htmlspecialchars($player['profileurl']) ?>" target="_blank">
                            <img src="<?= htmlspecialchars($player['avatar']) ?>" alt="Player Avatar" 
                                class="w-40 h-40 rounded-2xl shadow-glow transition-all duration-500 hover:scale-105 hover:shadow-blue-500/30">
                            <div class="absolute -bottom-3 -right-3 bg-gradient-to-r from-blue-600 to-blue-500 text-white p-2 rounded-full shadow-lg">
                                <i class="fas fa-external-link-alt"></i>
                            </div>
                        </a>
                    </div>
                </div>
                
                <!-- Player Details -->
                <div class="flex-grow space-y-6 text-center md:text-left">
                    <div>
                        <h2 class="text-3xl font-bold text-white mb-2"><?= htmlspecialchars($player['name']) ?></h2>
                        <div class="text-gray-400 flex items-center justify-center md:justify-start mb-2">
                            <i class="fas fa-id-card mr-2"></i>
                            <span class="font-mono select-all"><?= htmlspecialchars($player['steamid64']) ?></span>
                        </div>
                        <div class="flex items-center justify-center md:justify-start">
                            <a href="<?= htmlspecialchars($player['profileurl']) ?>" target="_blank" 
                               class="text-blue-400 hover:text-blue-300 transition-colors flex items-center">
                                <i class="fab fa-steam mr-2"></i>Steam Profile
                            </a>
                        </div>
                    </div>
                    
                    <!-- Group Status Summary -->
                    <div class="bg-dark-700/40 rounded-xl p-5 backdrop-blur-sm">
                        <h3 class="text-lg font-medium text-blue-300 mb-4 flex items-center">
                            <i class="fas fa-chart-pie mr-2"></i>VIP Status Summary
                        </h3>
                        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                            <div class="bg-dark-800/60 rounded-lg p-4 border border-green-500/20 flex flex-col items-center">
                                <span class="text-2xl font-bold text-green-400"><?= count($active_groups) ?></span>
                                <span class="text-gray-400 text-sm">Active Groups</span>
                            </div>
                            
                            <div class="bg-dark-800/60 rounded-lg p-4 border border-red-500/20 flex flex-col items-center">
                                <span class="text-2xl font-bold text-red-400"><?= count($inactive_groups) ?></span>
                                <span class="text-gray-400 text-sm">Expired Groups</span>
                            </div>
                            
                            <?php
                            $permanent_count = 0;
                            foreach ($active_groups as $group) {
                                if ($group['expires'] == 0) $permanent_count++;
                            }
                            ?>
                            <div class="bg-dark-800/60 rounded-lg p-4 border border-purple-500/20 flex flex-col items-center">
                                <span class="text-2xl font-bold text-purple-400"><?= $permanent_count ?></span>
                                <span class="text-gray-400 text-sm">Permanent VIPs</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Actions -->
            <div class="flex justify-end mt-6">
                <button type="button" 
                        x-data
                        x-on:click="$dispatch('open-modal', 'delete-player-modal')"
                        class="px-4 py-2 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-lg hover:from-red-500 hover:to-red-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                    <i class="fas fa-user-times mr-2"></i>Delete Player
                </button>
            </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
            <!-- Active Groups -->
            <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-green-500/20 backdrop-blur-sm relative overflow-hidden group transition-all duration-300 hover:shadow-green-500/10">
                
                <div class="absolute -top-20 -right-20 w-40 h-40 bg-green-500/5 rounded-full blur-2xl group-hover:bg-green-500/10 transition-all duration-700"></div>
                
                <div class="flex items-center mb-6 relative">
                    <div class="bg-green-500/10 p-3 rounded-lg mr-4">
                        <i class="fas fa-shield-alt text-green-400 text-2xl"></i>
                    </div>
                    <h2 class="text-xl font-bold text-green-400">Active VIP Groups</h2>
                </div>
                
                <?php if (empty($active_groups)): ?>
                    <div class="bg-dark-700/50 rounded-xl p-6 text-gray-400 flex flex-col items-center justify-center">
                        <i class="fas fa-info-circle mb-3 text-3xl text-gray-500"></i>
                        <p class="text-center">Player has no active VIP groups.</p>
                        <a href="edit_player.php?steamid=<?= $steamid ?>" 
                            class="mt-4 px-4 py-2 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg hover:from-blue-500 hover:to-blue-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                            <i class="fas fa-plus-circle mr-2"></i>Add VIP Group
                        </a>
                    </div>
                <?php else: ?>
                    <div class="overflow-hidden rounded-xl border border-dark-600/50">
                        <table class="w-full text-sm">
                            <thead class="text-xs uppercase bg-dark-700/80">
                                <tr>
                                    <th class="px-6 py-4 text-left text-gray-300 font-medium">Group Name</th>
                                    <th class="px-6 py-4 text-left text-gray-300 font-medium">Expiry</th>
                                    <th class="px-6 py-4 text-left text-gray-300 font-medium">Status</th>
                                </tr>
                            </thead>
                            <tbody class="divide-y divide-dark-600/50">
                                <?php foreach ($active_groups as $group): ?>
                                    <tr class="bg-dark-700/30 hover:bg-dark-700/50 transition-colors">
                                        <td class="px-6 py-4 font-medium text-white">
                                            <?php if (strtolower($group['group_name']) === 'vip'): ?>
                                                <span class="bg-blue-400/10 text-blue-400 p-1 rounded mr-2 inline-flex items-center justify-center w-6 h-6"><i class="fas fa-gem"></i></span>
                                            <?php elseif (strtolower($group['group_name']) === 'svip'): ?>
                                                <span class="bg-purple-400/10 text-purple-400 p-1 rounded mr-2 inline-flex items-center justify-center w-6 h-6"><i class="fas fa-crown"></i></span>
                                            <?php else: ?>
                                                <span class="bg-gray-400/10 text-gray-400 p-1 rounded mr-2 inline-flex items-center justify-center w-6 h-6"><i class="fas fa-tag"></i></span>
                                            <?php endif; ?>
                                            <?= htmlspecialchars($group['group_name']) ?>
                                        </td>
                                        <td class="px-6 py-4">
                                            <?= $group['expires'] == 0 ? "Never" : date("Y-m-d H:i", $group['expires']) ?>
                                        </td>
                                        <td class="px-6 py-4">
                                            <?php if ($group['expires'] == 0): ?>
                                                <span class="inline-flex items-center rounded-md bg-purple-400/10 px-2 py-1 text-xs font-medium text-purple-400 ring-1 ring-inset ring-purple-400/30">
                                                    <i class="fas fa-infinity mr-1"></i> Permanent
                                                </span>
                                            <?php else: ?>
                                                <span class="inline-flex items-center rounded-md bg-green-400/10 px-2 py-1 text-xs font-medium text-green-400 ring-1 ring-inset ring-green-400/30">
                                                    <i class="fas fa-clock mr-1"></i>
                                                    <?= format_time_remaining($group['expires']) ?>
                                                </span>
                                            <?php endif; ?>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </div>
            
            <!-- Expired Groups -->
            <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-red-500/20 backdrop-blur-sm relative overflow-hidden group transition-all duration-300 hover:shadow-red-500/10">
                
                <div class="absolute -bottom-20 -left-20 w-40 h-40 bg-red-500/5 rounded-full blur-2xl group-hover:bg-red-500/10 transition-all duration-700"></div>
                
                <div class="flex items-center mb-6 relative">
                    <div class="bg-red-500/10 p-3 rounded-lg mr-4">
                        <i class="fas fa-hourglass-end text-red-400 text-2xl"></i>
                    </div>
                    <h2 class="text-xl font-bold text-red-400">Expired VIP Groups</h2>
                </div>
                
                <?php if (empty($inactive_groups)): ?>
                    <div class="bg-dark-700/50 rounded-xl p-6 text-gray-400 flex items-center justify-center">
                        <i class="fas fa-info-circle mr-3 text-xl text-gray-500"></i>
                        <span>Player has no expired VIP groups.</span>
                    </div>
                <?php else: ?>
                    <div class="overflow-hidden rounded-xl border border-dark-600/50">
                        <table class="w-full text-sm">
                            <thead class="text-xs uppercase bg-dark-700/80">
                                <tr>
                                    <th class="px-6 py-4 text-left text-gray-300 font-medium">Group Name</th>
                                    <th class="px-6 py-4 text-left text-gray-300 font-medium">Expired On</th>
                                </tr>
                            </thead>
                            <tbody class="divide-y divide-dark-600/50">
                                <?php foreach ($inactive_groups as $group): ?>
                                    <tr class="bg-dark-700/30 hover:bg-dark-700/50 transition-colors">
                                        <td class="px-6 py-4 font-medium text-gray-400">
                                            <s><?= htmlspecialchars($group['group_name']) ?></s>
                                        </td>
                                        <td class="px-6 py-4 text-gray-400">
                                            <?= date("Y-m-d H:i", $group['expires']) ?>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                    <div class="mt-4 flex justify-end">
                        <a href="edit_player.php?steamid=<?= $steamid ?>" 
                           class="px-4 py-2 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-lg hover:from-red-500 hover:to-red-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                            <i class="fas fa-sync-alt mr-2"></i>Manage Expired Groups
                        </a>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <!-- Delete Player Confirmation Modal -->
    <div 
        x-data="{ show: false }" 
        x-show="show" 
        x-on:open-modal.window="if ($event.detail === 'delete-player-modal') show = true" 
        x-on:keydown.escape.window="show = false"
        x-transition:enter="transition ease-out duration-300"
        x-transition:enter-start="opacity-0 transform scale-90"
        x-transition:enter-end="opacity-100 transform scale-100"
        x-transition:leave="transition ease-in duration-300"
        x-transition:leave-start="opacity-100 transform scale-100"
        x-transition:leave-end="opacity-0 transform scale-90"
        class="fixed inset-0 z-50 flex items-center justify-center p-4" 
        style="display: none;"
    >
        <!-- Backdrop -->
        <div x-show="show" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100" x-transition:leave="transition ease-in duration-300" x-transition:leave-start="opacity-100" x-transition:leave-end="opacity-0" class="fixed inset-0 bg-black/75 backdrop-blur-sm" x-on:click="show = false"></div>
        
        <!-- Modal Content -->
        <div class="bg-dark-800 rounded-xl p-6 max-w-md w-full mx-4 shadow-2xl border border-red-500/20 z-10 relative overflow-hidden">
            
            <div class="absolute -top-24 -right-24 w-48 h-48 bg-red-500/10 rounded-full blur-3xl"></div>
            
            <div class="relative">
                <h3 class="text-xl font-bold text-red-400 mb-4 flex items-center">
                    <i class="fas fa-exclamation-triangle mr-2"></i>
                    Confirm Deletion
                </h3>
                <p class="mb-6 text-gray-300">
                    Are you sure you want to delete player <span class="font-semibold text-white"><?= htmlspecialchars($player['name']) ?></span>? 
                    <br><br>
                    This will remove all VIP groups associated with this player. This action cannot be undone.
                </p>
                <div class="flex justify-end gap-3">
                    <button type="button" x-on:click="show = false" class="px-4 py-2 bg-dark-600 text-white rounded-lg hover:bg-dark-500 transition duration-300">
                        Cancel
                    </button>
                    <form method="post" action="">
                        <input type="hidden" name="action" value="delete_player">
                        <button type="submit" class="px-4 py-2 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-lg hover:from-red-500 hover:to-red-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                            <i class="fas fa-trash-alt mr-2"></i>
                            Delete Player
                        </button>
                    </form>
                </div>
            </div>
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