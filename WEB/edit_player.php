<?php
require_once 'Config/config.php';
require_once 'player_functions.php';
require_once 'groups_manager.php';
require_once 'steam_api.php';

$steamid = $_GET['steamid'] ?? '';
if (!$steamid) {
    header('Location: index.php');
    exit;
}

$player = get_player_by_steamid($steamid);
if (!$player) {
    header('Location: index.php');
    exit;
}

if ($_SERVER["REQUEST_METHOD"] == "POST" && isset($_POST['action'])) {
    $conn = get_db_connection();
    
    switch ($_POST['action']) {
        case 'update_player':
            $name = $_POST['name'] ?? $player['name'];
            
            // Update player name in all their groups
            if (update_player_name($steamid, $name)) {
                $success_message = "Player information updated successfully!";
                // Refresh player data
                $player = get_player_by_steamid($steamid);
            } else {
                $error_message = "Failed to update player information.";
            }
            break;
            
        case 'add_group':
            $group_name = $_POST['group_name'] ?? '';
            $expiry_days = intval($_POST['expiry_days'] ?? 0);
            $extend_existing = isset($_POST['extend_existing']) && $_POST['extend_existing'] === 'yes';
            $extension_type = $_POST['extension_type'] ?? 'replace';
            
            if ($group_name) {
                if ($extend_existing) {
                    if ($extension_type === 'add') {
                        $result = update_player_group($steamid, $group_name, 1, $expiry_days);
                    }
                    else if ($extension_type === 'permanent' || $expiry_days === 0) {
                        $result = update_player_group($steamid, $group_name, 3);
                    }
                    else {
                        $result = update_player_group($steamid, $group_name, 2, $expiry_days);
                    }
                } else {
                    $result = add_group_to_player($steamid, $group_name, $expiry_days, false);
                }
                
                if ($result['success']) {
                    $success_message = $result['message'];
                } elseif (isset($result['needs_confirmation']) && $result['needs_confirmation']) {
                    $confirmation_data = $result;
                } else {
                    $error_message = $result['message'];
                }
            }
            break;
            
        case 'update_group':
            $group_name = $_POST['group_name'] ?? '';
            $action = intval($_POST['update_action'] ?? 0);
            $days = intval($_POST['days'] ?? 0);
            
            if ($group_name) {
                $result = update_player_group($steamid, $group_name, $action, $days);
                
                if ($result['success']) {
                    $success_message = $result['message'];
                } else {
                    $error_message = $result['message'];
                }
            }
            break;
            
        case 'make_permanent':
            $group_name = $_POST['group_name'] ?? '';
            
            if ($group_name) {
                $result = update_player_group($steamid, $group_name, 3);
                
                if ($result['success']) {
                    $success_message = $result['message'];
                } else {
                    $error_message = $result['message'];
                }
            }
            break;
            
        case 'remove_group':
            $group_name = $_POST['group_name'] ?? '';
            
            if ($group_name) {
                if (remove_group_from_player($steamid, $group_name)) {
                    $success_message = "Successfully removed $group_name from player!";
                } else {
                    $error_message = "Failed to remove group from player.";
                }
            }
            break;
    }
    
    $player = get_player_by_steamid($steamid);
}

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
    <title>Edit Player - VIP Manager</title>
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
                    }
                }
            }
        }
    </script>
    <style>
        .fade-enter-active, .fade-leave-active {
            transition: opacity 0.3s;
        }
        .fade-enter, .fade-leave-to {
            opacity: 0;
        }
        
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
        <header class="mb-8">
            <div class="flex flex-col md:flex-row justify-between items-center mb-6 gap-4">
                <h1 class="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-yellow-400 to-orange-500">
                    <i class="fas fa-user-edit mr-3"></i>Edit Player
                </h1>
                <div class="flex flex-wrap gap-2">
                    <a href="view_player.php?steamid=<?= $steamid ?>"
                       class="px-4 py-2 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg hover:from-blue-500 hover:to-blue-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                        <i class="fas fa-user mr-2"></i>View Profile
                    </a>
                    <a href="index.php"
                       class="px-4 py-2 bg-gradient-to-r from-gray-700 to-gray-600 text-white rounded-lg hover:from-gray-600 hover:to-gray-500 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                        <i class="fas fa-chevron-left mr-2"></i>Back to Dashboard
                    </a>
                </div>
            </div>
        </header>

        <!-- Messages -->
        <?php if (isset($success_message)): ?>
            <div 
                x-data="{ show: true }"
                x-show="show"
                x-init="setTimeout(() => show = false, 5000)"
                x-transition:enter="transition ease-out duration-300"
                x-transition:enter-start="opacity-0 transform -translate-y-2"
                x-transition:enter-end="opacity-100 transform translate-y-0"
                x-transition:leave="transition ease-in duration-300"
                x-transition:leave-start="opacity-100 transform translate-y-0"
                x-transition:leave-end="opacity-0 transform -translate-y-2"
                class="bg-green-800/30 text-white px-6 py-4 rounded-xl mb-6 flex items-center justify-between border border-green-500/30 shadow-lg"
            >
                <div class="flex items-center">
                    <i class="fas fa-check-circle mr-3 text-xl text-green-400"></i>
                    <?= htmlspecialchars($success_message) ?>
                </div>
                <button @click="show = false" class="text-white hover:text-green-200">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        <?php endif; ?>
        
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
                class="bg-red-800/30 text-white px-6 py-4 rounded-xl mb-6 flex items-center justify-between border border-red-500/30 shadow-lg"
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

        <div x-data="{ activeTab: 'info' }">
            <!-- Tab Navigation -->
            <div class="mb-6 border-b border-dark-600">
                <nav class="flex flex-wrap -mb-px">
                    <button 
                        @click="activeTab = 'info'" 
                        :class="{ 
                            'border-yellow-500 text-yellow-400': activeTab === 'info',
                            'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-300': activeTab !== 'info'
                        }"
                        class="py-4 px-4 font-medium border-b-2 transition-colors duration-200 ease-in-out flex items-center"
                    >
                        <i class="fas fa-user-circle mr-2"></i>
                        Player Information
                    </button>
                    <button 
                        @click="activeTab = 'active'" 
                        :class="{ 
                            'border-green-500 text-green-400': activeTab === 'active',
                            'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-300': activeTab !== 'active'
                        }"
                        class="py-4 px-4 font-medium border-b-2 transition-colors duration-200 ease-in-out flex items-center"
                    >
                        <i class="fas fa-shield-alt mr-2"></i>
                        Active Groups <span class="ml-2 px-2 py-0.5 text-xs rounded-full bg-green-400/20 text-green-400"><?= count($active_groups) ?></span>
                    </button>
                    <?php if (!empty($inactive_groups)): ?>
                    <button 
                        @click="activeTab = 'expired'" 
                        :class="{ 
                            'border-red-500 text-red-400': activeTab === 'expired',
                            'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-300': activeTab !== 'expired'
                        }"
                        class="py-4 px-4 font-medium border-b-2 transition-colors duration-200 ease-in-out flex items-center"
                    >
                        <i class="fas fa-hourglass-end mr-2"></i>
                        Expired Groups <span class="ml-2 px-2 py-0.5 text-xs rounded-full bg-red-400/20 text-red-400"><?= count($inactive_groups) ?></span>
                    </button>
                    <?php endif; ?>
                    <button 
                        @click="activeTab = 'add'" 
                        :class="{ 
                            'border-blue-500 text-blue-400': activeTab === 'add',
                            'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-300': activeTab !== 'add'
                        }"
                        class="py-4 px-4 font-medium border-b-2 transition-colors duration-200 ease-in-out flex items-center"
                    >
                        <i class="fas fa-plus-circle mr-2"></i>
                        Add New Group
                    </button>
                </nav>
            </div>

            <!-- Tab Content -->
            <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-dark-600/50 backdrop-blur-sm relative overflow-hidden">
                <!-- Player Information Tab -->
                <div x-show="activeTab === 'info'" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
                    <div class="flex items-center mb-6">
                        <div class="bg-yellow-500/10 p-3 rounded-lg mr-4">
                            <i class="fas fa-user-circle text-yellow-400 text-2xl"></i>
                        </div>
                        <h2 class="text-xl font-bold text-yellow-400">Player Information</h2>
                    </div>
                    
                    <div class="flex flex-col md:flex-row gap-8 items-center md:items-start">
                        <!-- Player Avatar and Basic Info -->
                        <div class="flex flex-col items-center md:items-start">
                            <img src="<?= htmlspecialchars($player['avatar']) ?>" alt="Player Avatar" 
                                 class="rounded-xl w-32 h-32 border-4 border-dark-600 shadow-lg mb-4">
                            
                            <div class="text-center md:text-left">
                                <p class="text-sm text-gray-400 mb-1">SteamID64:</p>
                                <div class="bg-dark-700/70 px-3 py-2 rounded-lg font-mono text-sm mb-4 select-all">
                                    <?= htmlspecialchars($player['steamid64']) ?>
                                </div>
                                
                                <a href="<?= htmlspecialchars($player['profileurl']) ?>" target="_blank" 
                                   class="inline-flex items-center px-3 py-2 rounded-lg bg-dark-700 text-blue-400 hover:bg-dark-600 transition duration-200">
                                    <i class="fab fa-steam mr-2"></i> Steam Profile
                                </a>
                            </div>
                        </div>
                        
                        <!-- Player Edit Form -->
                        <div class="flex-grow w-full max-w-md">
                            <form method="post" action="" class="space-y-4">
                                <input type="hidden" name="action" value="update_player">
                                
                                <div>
                                    <label class="block text-sm font-medium text-gray-400 mb-1">Player Name</label>
                                    <div class="relative">
                                        <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                            <i class="fas fa-id-badge text-gray-500"></i>
                                        </span>
                                        <input 
                                            type="text" 
                                            name="name" 
                                            value="<?= htmlspecialchars($player['name']) ?>"
                                            class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-yellow-500 focus:border-transparent transition duration-200 outline-none"
                                            placeholder="Enter player name"
                                        >
                                    </div>
                                    <p class="text-sm text-gray-500 mt-1">
                                        This will update the player's name in all group entries
                                    </p>
                                </div>
                                
                                <button 
                                    type="submit" 
                                    class="w-full px-4 py-3 bg-gradient-to-r from-yellow-600 to-orange-600 text-white rounded-lg hover:from-yellow-500 hover:to-orange-500 transition duration-300 shadow-md hover:shadow-lg flex items-center justify-center"
                                >
                                    <i class="fas fa-save mr-2"></i>Update Player Information
                                </button>
                            </form>
                        </div>
                    </div>
                </div>
                
                <!-- Active Groups Tab -->
                <div x-show="activeTab === 'active'" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
                    <div class="flex items-center mb-6">
                        <div class="bg-green-500/10 p-3 rounded-lg mr-4">
                            <i class="fas fa-shield-alt text-green-400 text-2xl"></i>
                        </div>
                        <h2 class="text-xl font-bold text-green-400">Active VIP Groups</h2>
                    </div>
                    
                    <?php if (empty($active_groups)): ?>
                        <div class="bg-dark-700/50 rounded-xl p-8 text-gray-400 flex flex-col items-center justify-center">
                            <i class="fas fa-info-circle mb-4 text-4xl text-gray-500"></i>
                            <p class="text-center">Player has no active VIP groups.</p>
                            <button 
                                @click="activeTab = 'add'" 
                                class="mt-4 px-4 py-2 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg hover:from-blue-500 hover:to-blue-600 transition duration-300 shadow-md hover:shadow-lg flex items-center"
                            >
                                <i class="fas fa-plus-circle mr-2"></i>Add VIP Group
                            </button>
                        </div>
                    <?php else: ?>
                        <div x-data="{ selected: null }" class="space-y-4">
                            <?php foreach ($active_groups as $index => $group): ?>
                                <div 
                                    class="bg-dark-700/50 border border-dark-600/50 rounded-xl overflow-hidden transition-all duration-300"
                                    :class="selected === <?= $index ?> ? 'ring-2 ring-green-500/50' : ''"
                                >
                                    <!-- Group Header -->
                                    <div 
                                        @click="selected = selected === <?= $index ?> ? null : <?= $index ?>"
                                        class="px-6 py-4 flex justify-between items-center cursor-pointer hover:bg-dark-700/70 transition-colors duration-200"
                                    >
                                        <div class="flex items-center">
                                            <div class="font-medium text-white flex items-center">
                                                <?php if (strtolower($group['group_name']) === 'vip'): ?>
                                                    <span class="bg-blue-400/10 text-blue-400 p-1 rounded mr-2"><i class="fas fa-gem"></i></span>
                                                <?php elseif (strtolower($group['group_name']) === 'svip'): ?>
                                                    <span class="bg-purple-400/10 text-purple-400 p-1 rounded mr-2"><i class="fas fa-crown"></i></span>
                                                <?php else: ?>
                                                    <span class="bg-gray-400/10 text-gray-400 p-1 rounded mr-2"><i class="fas fa-tag"></i></span>
                                                <?php endif; ?>
                                                <?= htmlspecialchars($group['group_name']) ?>
                                            </div>
                                            
                                            <div class="ml-4">
                                                <?php if ($group['expires'] == 0): ?>
                                                    <span class="inline-flex items-center rounded-full bg-purple-400/10 px-2.5 py-0.5 text-xs font-medium text-purple-400 ring-1 ring-inset ring-purple-400/30">
                                                        <i class="fas fa-infinity mr-1"></i> Permanent
                                                    </span>
                                                <?php else: ?>
                                                    <span class="inline-flex items-center rounded-full bg-green-400/10 px-2.5 py-0.5 text-xs font-medium text-green-400 ring-1 ring-inset ring-green-400/30">
                                                        <i class="fas fa-clock mr-1"></i>
                                                        <?= format_time_remaining($group['expires']) ?>
                                                    </span>
                                                <?php endif; ?>
                                            </div>
                                        </div>
                                        
                                        <div class="flex items-center">
                                            <span class="text-sm text-gray-400 mr-2">
                                                <?= $group['expires'] == 0 ? "Never expires" : "Expires: " . date("Y-m-d H:i", $group['expires']) ?>
                                            </span>
                                            <i class="fas fa-chevron-down text-gray-500 transition-transform duration-200" :class="selected === <?= $index ?> ? 'transform rotate-180' : ''"></i>
                                        </div>
                                    </div>
                                    
                                    <!-- Group Actions -->
                                    <div x-show="selected === <?= $index ?>" x-collapse class="px-6 py-4 bg-dark-700/30 border-t border-dark-600/30">
                                        <div class="flex flex-wrap gap-2">
                                            <?php if ($group['expires'] > 0): ?>
                                                <button 
                                                    x-data
                                                    x-on:click="$dispatch('open-modal', {id: 'edit-group-modal', groupName: '<?= htmlspecialchars($group['group_name']) ?>'})"
                                                    class="px-3 py-2 bg-blue-600/80 hover:bg-blue-600 text-white rounded-lg transition duration-200 flex items-center"
                                                >
                                                    <i class="fas fa-edit mr-2"></i> Edit Expiry
                                                </button>
                                                
                                                <form method="post" action="" class="inline" x-data>
                                                    <input type="hidden" name="action" value="make_permanent">
                                                    <input type="hidden" name="group_name" value="<?= htmlspecialchars($group['group_name']) ?>">
                                                    <button 
                                                        type="submit" 
                                                        @click="return confirm('Are you sure you want to make this group permanent? This cannot be undone.')"
                                                        class="px-3 py-2 bg-purple-600/80 hover:bg-purple-600 text-white rounded-lg transition duration-200 flex items-center"
                                                    >
                                                        <i class="fas fa-infinity mr-2"></i> Make Permanent
                                                    </button>
                                                </form>
                                            <?php endif; ?>
                                            
                                            <form method="post" action="" class="inline" x-data>
                                                <input type="hidden" name="action" value="remove_group">
                                                <input type="hidden" name="group_name" value="<?= htmlspecialchars($group['group_name']) ?>">
                                                <button 
                                                    type="submit" 
                                                    @click="return confirm('Are you sure you want to remove this group?')"
                                                    class="px-3 py-2 bg-red-600/80 hover:bg-red-600 text-white rounded-lg transition duration-200 flex items-center"
                                                >
                                                    <i class="fas fa-trash-alt mr-2"></i> Remove Group
                                                </button>
                                            </form>
                                        </div>
                                    </div>
                                </div>
                            <?php endforeach; ?>
                        </div>
                    <?php endif; ?>
                </div>
                
                <!-- Expired Groups Tab -->
                <?php if (!empty($inactive_groups)): ?>
                <div x-show="activeTab === 'expired'" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
                    <div class="flex items-center mb-6">
                        <div class="bg-red-500/10 p-3 rounded-lg mr-4">
                            <i class="fas fa-hourglass-end text-red-400 text-2xl"></i>
                        </div>
                        <h2 class="text-xl font-bold text-red-400">Expired VIP Groups</h2>
                    </div>
                    
                    <div x-data="{ selected: null }" class="space-y-4">
                        <?php foreach ($inactive_groups as $index => $group): ?>
                            <div 
                                class="bg-dark-700/50 border border-dark-600/50 rounded-xl overflow-hidden transition-all duration-300"
                                :class="selected === <?= $index ?> ? 'ring-2 ring-red-500/50' : ''"
                            >
                                <!-- Group Header -->
                                <div 
                                    @click="selected = selected === <?= $index ?> ? null : <?= $index ?>"
                                    class="px-6 py-4 flex justify-between items-center cursor-pointer hover:bg-dark-700/70 transition-colors duration-200"
                                >
                                    <div class="flex items-center">
                                        <div class="font-medium text-gray-400 flex items-center">
                                            <span class="bg-gray-500/10 text-gray-400 p-1 rounded mr-2"><i class="fas fa-tag"></i></span>
                                            <s><?= htmlspecialchars($group['group_name']) ?></s>
                                        </div>
                                        
                                        <div class="ml-4">
                                            <span class="inline-flex items-center rounded-full bg-red-400/10 px-2.5 py-0.5 text-xs font-medium text-red-400 ring-1 ring-inset ring-red-400/30">
                                                <i class="fas fa-exclamation-circle mr-1"></i> Expired
                                            </span>
                                        </div>
                                    </div>
                                    
                                    <div class="flex items-center">
                                        <span class="text-sm text-gray-400 mr-2">
                                            Expired: <?= date("Y-m-d H:i", $group['expires']) ?>
                                        </span>
                                        <i class="fas fa-chevron-down text-gray-500 transition-transform duration-200" :class="selected === <?= $index ?> ? 'transform rotate-180' : ''"></i>
                                    </div>
                                </div>
                                
                                <!-- Group Actions -->
                                <div x-show="selected === <?= $index ?>" x-collapse class="px-6 py-4 bg-dark-700/30 border-t border-dark-600/30">
                                    <div class="flex flex-wrap gap-2">
                                        <button 
                                            x-data
                                            x-on:click="$dispatch('open-modal', {id: 'renew-group-modal', groupName: '<?= htmlspecialchars($group['group_name']) ?>'})"
                                            class="px-3 py-2 bg-green-600/80 hover:bg-green-600 text-white rounded-lg transition duration-200 flex items-center"
                                        >
                                            <i class="fas fa-sync-alt mr-2"></i> Renew Group
                                        </button>
                                        
                                        <form method="post" action="" class="inline" x-data>
                                            <input type="hidden" name="action" value="remove_group">
                                            <input type="hidden" name="group_name" value="<?= htmlspecialchars($group['group_name']) ?>">
                                            <button 
                                                type="submit" 
                                                @click="return confirm('Are you sure you want to remove this expired group?')"
                                                class="px-3 py-2 bg-red-600/80 hover:bg-red-600 text-white rounded-lg transition duration-200 flex items-center"
                                            >
                                                <i class="fas fa-trash-alt mr-2"></i> Remove Group
                                            </button>
                                        </form>
                                    </div>
                                </div>
                            </div>
                        <?php endforeach; ?>
                    </div>
                </div>
                <?php endif; ?>
                
                <!-- Add New Group Tab -->
                <div x-show="activeTab === 'add'" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
                    <div class="flex items-center mb-6">
                        <div class="bg-blue-500/10 p-3 rounded-lg mr-4">
                            <i class="fas fa-plus-circle text-blue-400 text-2xl"></i>
                        </div>
                        <h2 class="text-xl font-bold text-blue-400">Add VIP Group</h2>
                    </div>
                    
                    <form x-data="{ duration: 30, permanent: false }" method="post" action="" class="max-w-md mx-auto">
                        <input type="hidden" name="action" value="add_group">
                        
                        <div class="space-y-6">
                            <div>
                                <label class="block text-sm font-medium text-gray-400 mb-1">Select Group</label>
                                <div class="relative">
                                    <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                        <i class="fas fa-users text-gray-500"></i>
                                    </span>
                                    <select 
                                        name="group_name" 
                                        required 
                                        class="w-full pl-10 pr-10 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition duration-200 appearance-none outline-none"
                                    >
                                        <?php foreach ($available_groups as $group): ?>
                                            <option value="<?= htmlspecialchars($group['name']) ?>">
                                                <?= htmlspecialchars($group['name']) ?> (<?= htmlspecialchars($group['flag']) ?>)
                                            </option>
                                        <?php endforeach; ?>
                                    </select>
                                    <div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
                                        <i class="fas fa-chevron-down text-gray-500"></i>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="bg-dark-700/50 rounded-xl p-4 border border-dark-600/50">
                                <div class="flex items-center justify-between mb-4">
                                    <span class="font-medium text-gray-300">Make Permanent?</span>
                                    <label class="inline-flex items-center cursor-pointer">
                                        <input 
                                            type="checkbox" 
                                            x-model="permanent" 
                                            class="sr-only peer"
                                        >
                                        <div class="relative w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-blue-500 rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-500"></div>
                                    </label>
                                </div>
                                
                                <input 
                                    type="hidden" 
                                    name="expiry_days" 
                                    :value="permanent ? 0 : duration"
                                >
                                
                                <div x-show="!permanent" x-transition class="mb-2">
                                    <label class="block text-sm font-medium text-gray-400 mb-1">Duration (Days)</label>
                                    <div class="flex items-center gap-4">
                                        <div class="flex-grow">
                                            <input 
                                                type="range" 
                                                x-model="duration" 
                                                min="1" 
                                                max="365" 
                                                step="1"
                                                class="w-full h-2 bg-dark-600 rounded-lg appearance-none cursor-pointer accent-blue-500"
                                            >
                                        </div>
                                        <div class="w-16">
                                            <input 
                                                type="number" 
                                                x-model="duration" 
                                                min="1" 
                                                max="365"
                                                class="w-full px-2 py-1 bg-dark-700 border border-dark-600 rounded-lg text-center focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
                                            >
                                        </div>
                                    </div>
                                    <div class="text-sm text-blue-300 mt-2 flex items-center">
                                        <i class="fas fa-calendar-alt mr-2"></i>
                                        Group will expire on: <span x-text="new Date(Date.now() + duration * 86400000).toLocaleDateString('en-US', {year: 'numeric', month: 'short', day: 'numeric'})" class="ml-1 font-medium"></span>
                                    </div>
                                </div>
                                
                                <div x-show="permanent" x-transition>
                                    <div class="text-sm text-purple-300 mt-2 flex items-center">
                                        <i class="fas fa-infinity mr-2"></i>
                                        Group will never expire
                                    </div>
                                </div>
                                <input type="hidden" name="extend_existing" value="no">
                            </div>
                            
                            <button 
                                type="submit" 
                                class="w-full px-4 py-3 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg hover:from-blue-500 hover:to-blue-600 transition duration-300 shadow-md hover:shadow-lg flex items-center justify-center"
                            >
                                <i class="fas fa-plus-circle mr-2"></i>Add Group
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    </div>

    <!-- Edit Group Modal -->
    <div 
        x-data="{ 
            show: false, 
            groupName: '',
            updateAction: '1',
            days: 30
        }" 
        x-show="show" 
        x-on:open-modal.window="if ($event.detail.id === 'edit-group-modal') { show = true; groupName = $event.detail.groupName; }"
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
        <div class="bg-dark-800 rounded-xl p-6 max-w-md w-full mx-4 shadow-2xl border border-blue-500/20 z-10 relative overflow-hidden">
            
            <div class="absolute -top-24 -right-24 w-48 h-48 bg-blue-500/10 rounded-full blur-3xl"></div>
            
            <div class="relative">
                <div class="flex justify-between items-center mb-4">
                    <h3 class="text-xl font-bold text-blue-400 flex items-center">
                        <i class="fas fa-edit mr-2"></i>Edit Group
                    </h3>
                    <button @click="show = false" class="text-gray-400 hover:text-white transition-colors">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                
                <form method="post" action="">
                    <input type="hidden" name="action" value="update_group">
                    <input type="hidden" name="group_name" x-model="groupName">
                    
                    <div class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-400 mb-1">Group Name</label>
                            <div class="px-4 py-3 bg-dark-700/60 border border-dark-600/50 rounded-lg text-white font-medium" x-text="groupName"></div>
                        </div>
                        
                        <div>
                            <label class="block text-sm font-medium text-gray-400 mb-1">Update Action</label>
                            <div class="space-y-2">
                                <label class="flex items-center p-3 bg-dark-700/60 border border-dark-600/50 rounded-lg cursor-pointer hover:bg-dark-700/80 transition-colors">
                                    <input type="radio" name="update_action" value="1" x-model="updateAction" class="mr-3">
                                    <div>
                                        <div class="font-medium text-white">Add days to current expiry</div>
                                        <div class="text-sm text-gray-400">Extends the current period</div>
                                    </div>
                                </label>
                                
                                <label class="flex items-center p-3 bg-dark-700/60 border border-dark-600/50 rounded-lg cursor-pointer hover:bg-dark-700/80 transition-colors">
                                    <input type="radio" name="update_action" value="2" x-model="updateAction" class="mr-3">
                                    <div>
                                        <div class="font-medium text-white">Set new expiry date</div>
                                        <div class="text-sm text-gray-400">Resets the expiry period</div>
                                    </div>
                                </label>
                            </div>
                        </div>
                        
                        <div>
                            <label class="block text-sm font-medium text-gray-400 mb-1">Days</label>
                            <div class="flex items-center gap-4">
                                <div class="flex-grow">
                                    <input 
                                        type="range" 
                                        x-model="days" 
                                        min="1" 
                                        max="365" 
                                        step="1"
                                        class="w-full h-2 bg-dark-600 rounded-lg appearance-none cursor-pointer accent-blue-500"
                                    >
                                </div>
                                <div class="w-16">
                                    <input 
                                        type="number" 
                                        name="days" 
                                        x-model="days" 
                                        min="1" 
                                        max="365"
                                        class="w-full px-2 py-1 bg-dark-700 border border-dark-600 rounded-lg text-center focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
                                    >
                                </div>
                            </div>
                            <div class="text-sm text-blue-300 mt-2" x-show="updateAction === '2'">
                                <div class="flex items-center">
                                    <i class="fas fa-calendar-alt mr-2"></i>
                                    New expiry date: <span x-text="new Date(Date.now() + days * 86400000).toLocaleDateString('en-US', {year: 'numeric', month: 'short', day: 'numeric'})" class="ml-1 font-medium"></span>
                                </div>
                            </div>
                        </div>
                        
                        <div class="flex justify-end gap-3 pt-2">
                            <button type="button" @click="show = false" class="px-4 py-2 bg-dark-600 text-white rounded-lg hover:bg-dark-500 transition duration-200">
                                Cancel
                            </button>
                            <button type="submit" class="px-4 py-2 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg hover:from-blue-500 hover:to-blue-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                                <i class="fas fa-save mr-2"></i>Update Group
                            </button>
                        </div>
                    </div>
                </form>
            </div>
        </div>
    </div>
    
    <!-- Renew Expired Group Modal -->
    <div 
        x-data="{ 
            show: false, 
            groupName: '',
            duration: 30,
            permanent: false
        }" 
        x-show="show" 
        x-on:open-modal.window="if ($event.detail.id === 'renew-group-modal') { show = true; groupName = $event.detail.groupName; }"
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
        <div class="bg-dark-800 rounded-xl p-6 max-w-md w-full mx-4 shadow-2xl border border-green-500/20 z-10 relative overflow-hidden">
            <div class="absolute -top-24 -right-24 w-48 h-48 bg-green-500/10 rounded-full blur-3xl"></div>
            
            <div class="relative">
                <div class="flex justify-between items-center mb-4">
                    <h3 class="text-xl font-bold text-green-400 flex items-center">
                        <i class="fas fa-sync-alt mr-2"></i>Renew Expired Group
                    </h3>
                    <button @click="show = false" class="text-gray-400 hover:text-white transition-colors">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                
                <form method="post" action="">
                    <input type="hidden" name="action" value="add_group">
                    <input type="hidden" name="extend_existing" value="yes">
                    <input type="hidden" name="group_name" x-model="groupName">
                    
                    <div class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-400 mb-1">Expired Group</label>
                            <div class="px-4 py-3 bg-dark-700/60 border border-dark-600/50 rounded-lg text-white font-medium" x-text="groupName"></div>
                        </div>
                        
                        <div class="bg-dark-700/50 rounded-xl p-4 border border-dark-600/50">
                            <div class="flex items-center justify-between mb-4">
                                <span class="font-medium text-gray-300">Make Permanent?</span>
                                <label class="inline-flex items-center cursor-pointer">
                                    <input 
                                        type="checkbox" 
                                        x-model="permanent" 
                                        class="sr-only peer"
                                        @change="if(permanent) { $refs.renewDays.value = 0; } else { $refs.renewDays.value = duration; }"
                                    >
                                    <div class="relative w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-green-500 rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
                                </label>
                            </div>
                            
                            <div x-show="!permanent" x-transition class="mb-2">
                                <label class="block text-sm font-medium text-gray-400 mb-1">Duration (Days)</label>
                                <div class="flex items-center gap-4">
                                    <div class="flex-grow">
                                        <input 
                                            type="range" 
                                            x-model="duration" 
                                            min="1" 
                                            max="365" 
                                            step="1"
                                            class="w-full h-2 bg-dark-600 rounded-lg appearance-none cursor-pointer accent-green-500"
                                            @input="$refs.renewDays.value = duration"
                                        >
                                    </div>
                                    <div class="w-16">
                                        <input 
                                            type="number" 
                                            name="expiry_days" 
                                            x-model="duration" 
                                            min="1" 
                                            max="365"
                                            class="w-full px-2 py-1 bg-dark-700 border border-dark-600 rounded-lg text-center focus:ring-2 focus:ring-green-500 focus:border-transparent outline-none"
                                            x-ref="renewDays"
                                        >
                                    </div>
                                </div>
                                <div class="text-sm text-green-300 mt-2 flex items-center">
                                    <i class="fas fa-calendar-alt mr-2"></i>
                                    Group will expire on: <span x-text="new Date(Date.now() + duration * 86400000).toLocaleDateString('en-US', {year: 'numeric', month: 'short', day: 'numeric'})" class="ml-1 font-medium"></span>
                                </div>
                            </div>
                            
                            <div x-show="permanent" x-transition>
                                <input type="hidden" name="expiry_days" value="0" x-ref="permanentRenewValue"> 
                                <div class="text-sm text-purple-300 mt-2 flex items-center">
                                    <i class="fas fa-infinity mr-2"></i>
                                    Group will never expire
                                </div>
                            </div>
                        </div>
                        
                        <div class="flex justify-end gap-3 pt-2">
                            <button type="button" @click="show = false" class="px-4 py-2 bg-dark-600 text-white rounded-lg hover:bg-dark-500 transition duration-200">
                                Cancel
                            </button>
                            <button type="submit" class="px-4 py-2 bg-gradient-to-r from-green-600 to-green-700 text-white rounded-lg hover:from-green-500 hover:to-green-600 transition duration-300 shadow-md hover:shadow-lg flex items-center">
                                <i class="fas fa-sync-alt mr-2"></i>Renew Group
                            </button>
                        </div>
                    </div>
                </form>
            </div>
        </div>
    </div>
    
    <!-- Extension Confirmation Modal -->
    <?php if (isset($confirmation_data) && $confirmation_data['needs_confirmation']): ?>
    <div 
        x-data="{ 
            show: true,
            extensionType: 'replace'
        }"
        x-show="show"
        x-transition:enter="transition ease-out duration-300"
        x-transition:enter-start="opacity-0 transform scale-90"
        x-transition:enter-end="opacity-100 transform scale-100"
        x-transition:leave="transition ease-in duration-300"
        x-transition:leave-start="opacity-100 transform scale-100"
        x-transition:leave-end="opacity-0 transform scale-90"
        class="fixed inset-0 z-50 flex items-center justify-center p-4"
    >
        <!-- Backdrop -->
        <div class="fixed inset-0 bg-black/75 backdrop-blur-sm"></div>
        
        <!-- Modal Content -->
        <div class="bg-dark-800 rounded-xl p-6 max-w-md w-full mx-4 shadow-2xl border border-yellow-500/20 z-10 relative overflow-hidden">
            
            <div class="absolute -top-24 -right-24 w-48 h-48 bg-yellow-500/10 rounded-full blur-3xl"></div>
            
            <div class="relative">
                <div class="flex justify-between items-center mb-4">
                    <h3 class="text-xl font-bold text-yellow-400 flex items-center">
                        <i class="fas fa-exclamation-triangle mr-2"></i>Group Already Exists
                    </h3>
                    <button @click="show = false" class="text-gray-400 hover:text-white transition-colors">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                
                <p class="mb-6 text-gray-300">
                    <?= htmlspecialchars($confirmation_data['message']) ?>
                </p>
                
                <div class="mb-6">
                    <p class="text-white font-medium mb-3">What would you like to do?</p>
                    <form id="extend-form" method="post" action="">
                        <input type="hidden" name="action" value="add_group">
                        <input type="hidden" name="steamid" value="<?= htmlspecialchars($confirmation_data['steamid']) ?>">
                        <input type="hidden" name="group_name" value="<?= htmlspecialchars($confirmation_data['group_name']) ?>">
                        <input type="hidden" name="extend_existing" value="yes">
                        
                        <div class="space-y-3">
                            <label class="flex items-center p-3 bg-dark-700/60 border border-dark-600/50 rounded-lg cursor-pointer hover:bg-dark-700/80 transition-colors">
                                <input type="radio" name="extension_type" value="replace" x-model="extensionType" checked class="mr-3">
                                <div>
                                    <div class="font-medium text-white">Replace expiry period</div>
                                    <div class="text-sm text-gray-400">Set to <?= htmlspecialchars($_POST['expiry_days'] ?? 30) ?> days from now</div>
                                </div>
                            </label>
                            
                            <label class="flex items-center p-3 bg-dark-700/60 border border-dark-600/50 rounded-lg cursor-pointer hover:bg-dark-700/80 transition-colors">
                                <input type="radio" name="extension_type" value="add" x-model="extensionType" class="mr-3">
                                <div>
                                    <div class="font-medium text-white">Extend current period</div>
                                    <div class="text-sm text-gray-400">Add <?= htmlspecialchars($_POST['expiry_days'] ?? 30) ?> days to current expiry</div>
                                </div>
                            </label>
                            
                            <label class="flex items-center p-3 bg-dark-700/60 border border-dark-600/50 rounded-lg cursor-pointer hover:bg-dark-700/80 transition-colors">
                                <input type="radio" name="extension_type" value="permanent" x-model="extensionType" class="mr-3">
                                <div>
                                    <div class="font-medium text-white">Make permanent</div>
                                    <div class="text-sm text-gray-400">Group will never expire</div>
                                </div>
                            </label>
                        </div>
                        
                        <input type="hidden" name="expiry_days" value="<?= htmlspecialchars($_POST['expiry_days'] ?? 30) ?>">
                    </form>
                </div>
                
                <div class="flex justify-end gap-3">
                    <button @click="show = false" class="px-4 py-2 bg-dark-600 text-white rounded-lg hover:bg-dark-500 transition duration-200">
                        Cancel
                    </button>
                    <button 
                        @click="document.getElementById('extend-form').submit()"
                        class="px-4 py-2 bg-gradient-to-r from-yellow-600 to-yellow-700 text-white rounded-lg hover:from-yellow-500 hover:to-yellow-600 transition duration-300 shadow-md hover:shadow-lg flex items-center"
                    >
                        <i class="fas fa-check mr-2"></i>Apply Changes
                    </button>
                </div>
            </div>
        </div>
    </div>
    <?php endif; ?>

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