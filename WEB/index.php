<?php
require_once 'Config/config.php';
require_once 'player_functions.php';
require_once 'steam_api.php';
require_once 'groups_manager.php';
require_once 'steamauth.php';

require_admin();

$page = isset($_GET['page']) ? max(1, intval($_GET['page'])) : 1;
$limit = isset($_GET['limit']) ? max(10, min(100, intval($_GET['limit']))) : 25;
$show_inactive = isset($_GET['show_inactive']) && $_GET['show_inactive'] == '1';

if ($_SERVER["REQUEST_METHOD"] == "POST") {
    if (isset($_POST['action'])) {
        switch ($_POST['action']) {
            case 'add_group':
                $steamid = $_POST['steamid'] ?? '';
                $group_name = $_POST['group_name'] ?? '';
                $expiry_days = intval($_POST['expiry_days'] ?? 0);
                $extend_existing = isset($_POST['extend_existing']) && $_POST['extend_existing'] === 'yes';
                
                if ($steamid && $group_name) {
                    $result = add_group_to_player($steamid, $group_name, $expiry_days, $extend_existing);
                    
                    if ($result['success']) {
                        $success_message = $result['message'];
                    } elseif (isset($result['needs_confirmation']) && $result['needs_confirmation']) {
                        // Store info for confirmation dialog
                        $confirmation_data = $result;
                    } else {
                        $error_message = $result['message'];
                    }
                }
                break;
                
            case 'add_new_group':
                $name = $_POST['name'] ?? '';
                $flag = $_POST['flag'] ?? '';
                $description = $_POST['description'] ?? '';
                
                if ($name && $flag) {
                    if (add_vip_group($name, $flag, $description)) {
                        $success_message = "Successfully added new VIP group: $name!";
                    } else {
                        $error_message = "Failed to add new VIP group. Group may already exist.";
                    }
                }
                break;
        }
    }
}

$search_term = $_GET['search'] ?? '';
if (!empty($search_term)) {
    $result = search_players($search_term, $page, $limit, $show_inactive);
    $players = $result['players'];
    $pagination = $result['pagination'];
} else {
    $result = get_all_vip_players($page, $limit, $show_inactive);
    $players = $result['players'];
    $pagination = $result['pagination'];
}

$all_player_groups = [];
$all_expired_groups = [];
$conn = get_db_connection();

if (!empty($players)) {
    $steamids = array_column($players, 'steamid64');
    
    if (count($steamids) > 0) {
        $placeholders = implode(',', array_fill(0, count($steamids), '?'));
        $now = time();
        
        $stmt = $conn->prepare("SELECT steamid64, group_name, expires FROM player_groups WHERE steamid64 IN ($placeholders)");
        $stmt->execute($steamids);
        
        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $steamid = $row['steamid64'];
            
            if (!isset($all_player_groups[$steamid])) {
                $all_player_groups[$steamid] = [];
            }
            if (!isset($all_expired_groups[$steamid])) {
                $all_expired_groups[$steamid] = [];
            }
            
            if ($row['expires'] == 0 || $row['expires'] > $now) {
                $all_player_groups[$steamid][] = $row['group_name'];
            } else {
                $all_expired_groups[$steamid][] = $row['group_name'];
                
                if ($show_inactive) {
                    $all_player_groups[$steamid][] = $row['group_name'] . ' (Expired)';
                }
            }
        }
    }
}

$vip_stats = get_vip_stats();

$available_groups = get_available_groups();

$current_user = get_current_steam_user();
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>VIP Manager Dashboard</title>
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
                <h1 class="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-500">
                    <i class="fas fa-users-cog mr-3"></i>CS2 VIP Manager
                </h1>
                
                <div class="flex items-center gap-4">
                    <?php if ($current_user): ?>
                        <div class="flex items-center gap-3 bg-dark-800/70 px-4 py-2 rounded-xl border border-dark-600/50">
                            <img src="<?= htmlspecialchars($current_user['avatar']) ?>" alt="Profile" class="w-8 h-8 rounded-full border-2 border-blue-500/30">
                            <span class="text-gray-300"><?= htmlspecialchars($current_user['name']) ?></span>
                            <a href="logout.php" class="px-3 py-1 bg-red-700/80 hover:bg-red-600 text-white rounded-lg text-sm transition duration-200">
                                <i class="fas fa-sign-out-alt mr-1"></i> Logout
                            </a>
                        </div>
                    <?php endif; ?>
                </div>
            </div>
            
            <!-- Stats Cards -->
            <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mt-8">
                <div class="bg-dark-800/50 rounded-xl p-4 shadow-lg border border-blue-500/10 backdrop-blur-sm transition-all duration-300 hover:shadow-blue-500/5 group">
                    <div class="flex justify-between items-start">
                        <div>
                            <h3 class="text-gray-400 text-sm font-medium">Total Players</h3>
                            <p class="text-3xl font-bold text-white mt-2"><?= $vip_stats['total_players'] ?></p>
                        </div>
                        <div class="bg-blue-500/10 p-2 rounded-lg group-hover:bg-blue-500/20 transition-colors duration-300">
                            <i class="fas fa-users text-blue-400"></i>
                        </div>
                    </div>
                </div>
                
                <div class="bg-dark-800/50 rounded-xl p-4 shadow-lg border border-green-500/10 backdrop-blur-sm transition-all duration-300 hover:shadow-green-500/5 group">
                    <div class="flex justify-between items-start">
                        <div>
                            <h3 class="text-gray-400 text-sm font-medium">Active VIPs</h3>
                            <p class="text-3xl font-bold text-green-400 mt-2"><?= $vip_stats['active_vips'] ?></p>
                        </div>
                        <div class="bg-green-500/10 p-2 rounded-lg group-hover:bg-green-500/20 transition-colors duration-300">
                            <i class="fas fa-user-shield text-green-400"></i>
                        </div>
                    </div>
                </div>
                
                <div class="bg-dark-800/50 rounded-xl p-4 shadow-lg border border-red-500/10 backdrop-blur-sm transition-all duration-300 hover:shadow-red-500/5 group">
                    <div class="flex justify-between items-start">
                        <div>
                            <h3 class="text-gray-400 text-sm font-medium">Inactive Players</h3>
                            <p class="text-3xl font-bold text-red-400 mt-2"><?= $vip_stats['inactive_players'] ?></p>
                        </div>
                        <div class="bg-red-500/10 p-2 rounded-lg group-hover:bg-red-500/20 transition-colors duration-300">
                            <i class="fas fa-user-slash text-red-400"></i>
                        </div>
                    </div>
                </div>
                
                <div class="bg-dark-800/50 rounded-xl p-4 shadow-lg border border-gray-500/10 backdrop-blur-sm transition-all duration-300 hover:shadow-gray-500/5 group">
                    <div class="flex justify-between items-start">
                        <div>
                            <h3 class="text-gray-400 text-sm font-medium">No Groups</h3>
                            <p class="text-3xl font-bold text-gray-400 mt-2"><?= $vip_stats['players_no_groups'] ?></p>
                        </div>
                        <div class="bg-gray-500/10 p-2 rounded-lg group-hover:bg-gray-500/20 transition-colors duration-300">
                            <i class="fas fa-user-times text-gray-400"></i>
                        </div>
                    </div>
                </div>
                
                <div class="bg-dark-800/50 rounded-xl p-4 shadow-lg border border-yellow-500/10 backdrop-blur-sm transition-all duration-300 hover:shadow-yellow-500/5 group">
                    <div class="flex justify-between items-start">
                        <div>
                            <h3 class="text-gray-400 text-sm font-medium">Expiring Soon</h3>
                            <p class="text-3xl font-bold text-yellow-400 mt-2"><?= $vip_stats['expiring_soon'] ?></p>
                        </div>
                        <div class="bg-yellow-500/10 p-2 rounded-lg group-hover:bg-yellow-500/20 transition-colors duration-300">
                            <i class="fas fa-hourglass-half text-yellow-400"></i>
                        </div>
                    </div>
                </div>
                
                <div class="bg-dark-800/50 rounded-xl p-4 shadow-lg border border-purple-500/10 backdrop-blur-sm transition-all duration-300 hover:shadow-purple-500/5 group">
                    <div class="flex justify-between items-start">
                        <div>
                            <h3 class="text-gray-400 text-sm font-medium">Permanent VIPs</h3>
                            <p class="text-3xl font-bold text-purple-400 mt-2"><?= $vip_stats['permanent_vips'] ?></p>
                        </div>
                        <div class="bg-purple-500/10 p-2 rounded-lg group-hover:bg-purple-500/20 transition-colors duration-300">
                            <i class="fas fa-infinity text-purple-400"></i>
                        </div>
                    </div>
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

        <div x-data="{ activeTab: 'players' }">
            <!-- Tab Navigation -->
            <div class="mb-6 border-b border-dark-600">
                <nav class="flex flex-wrap -mb-px">
                    <button 
                        @click="activeTab = 'players'" 
                        :class="{ 
                            'border-blue-500 text-blue-400': activeTab === 'players',
                            'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-300': activeTab !== 'players'
                        }"
                        class="py-4 px-4 font-medium border-b-2 transition-colors duration-200 ease-in-out flex items-center"
                    >
                        <i class="fas fa-users mr-2"></i>
                        Manage Players
                    </button>
                    <button 
                        @click="activeTab = 'groups'" 
                        :class="{ 
                            'border-purple-500 text-purple-400': activeTab === 'groups',
                            'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-300': activeTab !== 'groups'
                        }"
                        class="py-4 px-4 font-medium border-b-2 transition-colors duration-200 ease-in-out flex items-center"
                    >
                        <i class="fas fa-layer-group mr-2"></i>
                        Manage Groups
                    </button>
                </nav>
            </div>

            <!-- Players Tab Content -->
            <div x-show="activeTab === 'players'" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
                <!-- Search & Add Section -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                    <!-- Search Form -->
                    <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-blue-500/20 backdrop-blur-sm relative overflow-hidden">
                        <div class="absolute -top-24 -right-24 w-48 h-48 bg-blue-500/10 rounded-full blur-3xl"></div>
                        
                        <div class="flex items-center mb-6 relative">
                            <div class="bg-blue-500/10 p-3 rounded-lg mr-4">
                                <i class="fas fa-search text-blue-400 text-2xl"></i>
                            </div>
                            <h2 class="text-xl font-bold text-blue-400">Search Players</h2>
                        </div>
                        
                        <form method="get" action="" class="relative">
                            <div class="flex gap-2">
                                <div class="relative flex-grow">
                                    <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                        <i class="fas fa-search text-gray-500"></i>
                                    </span>
                                    <input 
                                        type="text" 
                                        name="search" 
                                        placeholder="Enter player name or SteamID" 
                                        value="<?= htmlspecialchars($search_term) ?>"
                                        class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition duration-200 outline-none"
                                    >
                                </div>
                                <button 
                                    type="submit" 
                                    class="px-4 py-3 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg hover:from-blue-500 hover:to-blue-600 transition duration-300 shadow-md hover:shadow-lg flex items-center"
                                >
                                    <i class="fas fa-search mr-2"></i>Search
                                </button>
                            </div>
                        </form>
                    </div>
                    
                    <!-- Add Group Form -->
                    <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-green-500/20 backdrop-blur-sm relative overflow-hidden">
                        <div class="absolute -bottom-24 -left-24 w-48 h-48 bg-green-500/10 rounded-full blur-3xl"></div>
                        
                        <div class="flex items-center mb-6 relative">
                            <div class="bg-green-500/10 p-3 rounded-lg mr-4">
                                <i class="fas fa-plus-circle text-green-400 text-2xl"></i>
                            </div>
                            <h2 class="text-xl font-bold text-green-400">Add VIP Group</h2>
                        </div>
                        
                        <form x-data="{ duration: 30, permanent: false }" method="post" action="" id="add-group-form">
                            <input type="hidden" name="action" value="add_group">
                            
                            <div class="space-y-4">
                                <div>
                                    <label class="block text-sm font-medium text-gray-400 mb-1">SteamID64</label>
                                    <div class="relative">
                                        <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                            <i class="fas fa-fingerprint text-gray-500"></i>
                                        </span>
                                        <input 
                                            type="text" 
                                            name="steamid" 
                                            required 
                                            class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent transition duration-200 outline-none"
                                            placeholder="Enter player's SteamID64"
                                        >
                                    </div>
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-gray-400 mb-1">Group</label>
                                    <div class="relative">
                                        <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                            <i class="fas fa-users text-gray-500"></i>
                                        </span>
                                        <select 
                                            name="group_name" 
                                            required 
                                            class="w-full pl-10 pr-10 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent transition duration-200 appearance-none outline-none"
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
                                            <div class="relative w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-green-500 rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
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
                                                    class="w-full h-2 bg-dark-600 rounded-lg appearance-none cursor-pointer accent-green-500"
                                                >
                                            </div>
                                            <div class="w-16">
                                                <input 
                                                    type="number" 
                                                    x-model="duration" 
                                                    min="1" 
                                                    max="365"
                                                    class="w-full px-2 py-1 bg-dark-700 border border-dark-600 rounded-lg text-center focus:ring-2 focus:ring-green-500 focus:border-transparent outline-none"
                                                >
                                            </div>
                                        </div>
                                        <div class="text-sm text-green-300 mt-2 flex items-center">
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
                                </div>
                                
                                <button 
                                    type="submit" 
                                    class="w-full px-4 py-3 bg-gradient-to-r from-green-600 to-green-700 text-white rounded-lg hover:from-green-500 hover:to-green-600 transition duration-300 shadow-md hover:shadow-lg flex items-center justify-center"
                                >
                                    <i class="fas fa-plus-circle mr-2"></i>Add Group
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
                
                <!-- Players List -->
                <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-dark-600/50 backdrop-blur-sm relative overflow-hidden">
                    <div class="flex justify-between items-center mb-6">
                        <div class="flex items-center">
                            <div class="bg-purple-500/10 p-3 rounded-lg mr-4">
                                <i class="fas fa-list-ul text-purple-400 text-2xl"></i>
                            </div>
                            <h2 class="text-xl font-bold text-purple-400">Players with VIP Groups</h2>
                        </div>
                        
                        <div class="flex items-center gap-4">
                            <!-- Show Inactive Players Toggle -->
                            <div class="flex items-center gap-2">
                                <span class="text-sm text-gray-400">Show Inactive:</span>
                                <label class="inline-flex items-center cursor-pointer">
                                    <input 
                                        type="checkbox" 
                                        id="show-inactive" 
                                        <?= $show_inactive ? 'checked' : '' ?>
                                        class="sr-only peer"
                                        onchange="toggleInactive(this.checked)"
                                    >
                                    <div class="relative w-11 h-6 bg-gray-600 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-purple-500 rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-purple-500"></div>
                                </label>
                            </div>
                            
                            <!-- Results per page dropdown -->
                            <div class="flex items-center gap-2">
                                <span class="text-sm text-gray-400">Show:</span>
                                <select 
                                    onchange="changeResultsPerPage(this.value)" 
                                    class="px-2 py-1 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent transition duration-200 outline-none text-sm"
                                >
                                    <option value="10" <?= $limit == 10 ? 'selected' : '' ?>>10</option>
                                    <option value="25" <?= $limit == 25 ? 'selected' : '' ?>>25</option>
                                    <option value="50" <?= $limit == 50 ? 'selected' : '' ?>>50</option>
                                    <option value="100" <?= $limit == 100 ? 'selected' : '' ?>>100</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    
                    <?php if (empty($players)): ?>
                        <div class="bg-dark-700/50 rounded-xl p-8 text-gray-400 flex flex-col items-center justify-center">
                            <i class="fas fa-info-circle mb-4 text-4xl text-gray-500"></i>
                            <p class="text-center">No players found.</p>
                        </div>
                    <?php else: ?>
                        <div class="overflow-x-auto overflow-y-auto max-h-[520px] rounded-xl">
                            <table class="w-full text-sm text-left">
                                <thead class="text-xs uppercase bg-dark-700/80 sticky top-0 z-10">
                                    <tr>
                                        <th class="px-6 py-4 text-gray-300 font-medium">Avatar</th>
                                        <th class="px-6 py-4 text-gray-300 font-medium">Name</th>
                                        <th class="px-6 py-4 text-gray-300 font-medium">SteamID</th>
                                        <th class="px-6 py-4 text-gray-300 font-medium">Groups</th>
                                        <th class="px-6 py-4 text-gray-300 font-medium">Status</th>
                                        <th class="px-6 py-4 text-gray-300 font-medium">Actions</th>
                                    </tr>
                                </thead>
                                <tbody class="divide-y divide-dark-600/30">
                                    <?php foreach ($players as $player): ?>
                                        <?php 
                                        $steamid = $player['steamid64'];
                                        $active_groups = $all_player_groups[$steamid] ?? [];
                                        $expired_groups = $all_expired_groups[$steamid] ?? [];
                                        
                                        // Determine if player is active (has at least one active group)
                                        $has_active_groups = !empty($active_groups) && (!$show_inactive || count($active_groups) > count($expired_groups));
                                        $has_expired_groups = !empty($expired_groups);
                                        
                                        // Get the groups to display
                                        $display_groups = $show_inactive ? array_merge($active_groups, $expired_groups) : $active_groups;
                                        $display_groups = array_unique($display_groups);
                                        ?>
                                        <tr class="bg-dark-700/30 hover:bg-dark-700/50 transition-colors">
                                            <td class="px-6 py-4">
                                                <a href="<?= htmlspecialchars($player['profileurl']) ?>" target="_blank">
                                                    <img src="<?= htmlspecialchars($player['avatar']) ?>" alt="Avatar" class="w-10 h-10 rounded-full border-2 border-dark-600 hover:border-blue-500 transition-all duration-300 hover:scale-110">
                                                </a>
                                            </td>
                                            <td class="px-6 py-4 font-medium text-white">
                                                <?= htmlspecialchars($player['name']) ?>
                                            </td>
                                            <td class="px-6 py-4 font-mono text-sm">
                                                <?= htmlspecialchars($player['steamid64']) ?>
                                            </td>
                                            <td class="px-6 py-4">
                                                <?php if (!empty($display_groups)): ?>
                                                    <div class="flex flex-wrap gap-1">
                                                        <?php foreach ($display_groups as $group_name): ?>
                                                            <?php
                                                            $is_expired = strpos($group_name, '(Expired)') !== false;
                                                            $clean_name = str_replace(' (Expired)', '', $group_name);
                                                            
                                                            $bg_color = 'bg-gray-600/20';
                                                            $text_color = 'text-gray-300';
                                                            $border_color = 'border-gray-600/30';
                                                            
                                                            if (strtolower($clean_name) === 'vip') {
                                                                $bg_color = $is_expired ? 'bg-blue-600/10' : 'bg-blue-600/20';
                                                                $text_color = $is_expired ? 'text-blue-300/70' : 'text-blue-300';
                                                                $border_color = $is_expired ? 'border-blue-600/20' : 'border-blue-600/30';
                                                            } elseif (strtolower($clean_name) === 'svip') {
                                                                $bg_color = $is_expired ? 'bg-purple-600/10' : 'bg-purple-600/20';
                                                                $text_color = $is_expired ? 'text-purple-300/70' : 'text-purple-300';
                                                                $border_color = $is_expired ? 'border-purple-600/20' : 'border-purple-600/30';
                                                            }
                                                            ?>
                                                            <span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium <?= $bg_color ?> <?= $text_color ?> border border-inset <?= $border_color ?>">
                                                                <?= htmlspecialchars($clean_name) ?>
                                                                <?php if ($is_expired): ?>
                                                                    <span class="ml-1 text-red-300 text-opacity-70">(Exp)</span>
                                                                <?php endif; ?>
                                                            </span>
                                                        <?php endforeach; ?>
                                                    </div>
                                                <?php else: ?>
                                                    <span class="text-gray-400">None</span>
                                                <?php endif; ?>
                                            </td>
                                            <td class="px-6 py-4">
                                                <?php if ($has_active_groups): ?>
                                                    <span class="inline-flex items-center rounded-full bg-green-400/10 px-2.5 py-0.5 text-xs font-medium text-green-400 ring-1 ring-inset ring-green-400/30">
                                                        <i class="fas fa-check-circle mr-1"></i> Active
                                                    </span>
                                                <?php elseif ($has_expired_groups): ?>
                                                    <span class="inline-flex items-center rounded-full bg-red-400/10 px-2.5 py-0.5 text-xs font-medium text-red-400 ring-1 ring-inset ring-red-400/30">
                                                        <i class="fas fa-times-circle mr-1"></i> Expired
                                                    </span>
                                                <?php else: ?>
                                                    <span class="inline-flex items-center rounded-full bg-gray-400/10 px-2.5 py-0.5 text-xs font-medium text-gray-400 ring-1 ring-inset ring-gray-400/30">
                                                        <i class="fas fa-minus-circle mr-1"></i> No Groups
                                                    </span>
                                                <?php endif; ?>
                                            </td>
                                            <td class="px-6 py-4">
                                                <div class="flex items-center space-x-3">
                                                    <a href="view_player.php?steamid=<?= $steamid ?>" 
                                                       class="text-blue-400 hover:text-blue-300 transition-colors">
                                                        <i class="fas fa-eye"></i>
                                                    </a>
                                                    <a href="edit_player.php?steamid=<?= $steamid ?>" 
                                                       class="text-yellow-400 hover:text-yellow-300 transition-colors">
                                                        <i class="fas fa-edit"></i>
                                                    </a>
                                                </div>
                                            </td>
                                        </tr>
                                    <?php endforeach; ?>
                                </tbody>
                            </table>
                        </div>
                        
                        <!-- Pagination -->
                        <?php if ($pagination['total_pages'] > 1): ?>
                            <div class="flex justify-between items-center mt-6">
                                <div class="text-sm text-gray-400">
                                    Showing <?= count($players) ?> of <?= $pagination['total_players'] ?> players
                                </div>
                                <div class="flex space-x-1">
                                    <?php if ($page > 1): ?>
                                        <a href="?<?= http_build_query(array_merge($_GET, ['page' => $page - 1])) ?>" 
                                           class="px-3 py-1 bg-dark-700 rounded hover:bg-dark-600 transition-colors flex items-center">
                                            <i class="fas fa-chevron-left mr-1"></i> Prev
                                        </a>
                                    <?php endif; ?>
                                    
                                    <?php 

                                    $start_page = max(1, $page - 2);
                                    $end_page = min($pagination['total_pages'], $start_page + 4);
                                    if ($end_page - $start_page < 4 && $pagination['total_pages'] > 4) {
                                        $start_page = max(1, $end_page - 4);
                                    }
                                    ?>
                                    
                                    <?php for ($i = $start_page; $i <= $end_page; $i++): ?>
                                        <a href="?<?= http_build_query(array_merge($_GET, ['page' => $i])) ?>" 
                                           class="px-3 py-1 rounded transition-colors <?= $i == $page ? 'bg-blue-600 text-white' : 'bg-dark-700 text-gray-300 hover:bg-dark-600' ?>">
                                            <?= $i ?>
                                        </a>
                                    <?php endfor; ?>
                                    
                                    <?php if ($page < $pagination['total_pages']): ?>
                                        <a href="?<?= http_build_query(array_merge($_GET, ['page' => $page + 1])) ?>" 
                                           class="px-3 py-1 bg-dark-700 rounded hover:bg-dark-600 transition-colors flex items-center">
                                            Next <i class="fas fa-chevron-right ml-1"></i>
                                        </a>
                                    <?php endif; ?>
                                </div>
                            </div>
                        <?php endif; ?>
                    <?php endif; ?>
                </div>
            </div>

            <!-- Groups Tab Content -->
            <div x-show="activeTab === 'groups'" x-transition:enter="transition ease-out duration-300" x-transition:enter-start="opacity-0" x-transition:enter-end="opacity-100">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-10">
                    <!-- Available Groups -->
                    <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-purple-500/20 backdrop-blur-sm relative overflow-hidden">
                        <div class="absolute -top-24 -left-24 w-48 h-48 bg-purple-500/10 rounded-full blur-3xl"></div>
                        
                        <div class="flex items-center mb-6 relative">
                            <div class="bg-purple-500/10 p-3 rounded-lg mr-4">
                                <i class="fas fa-layer-group text-purple-400 text-2xl"></i>
                            </div>
                            <h2 class="text-xl font-bold text-purple-400">Available VIP Groups</h2>
                        </div>
                        
                        <?php if (empty($available_groups)): ?>
                            <div class="bg-dark-700/50 rounded-xl p-8 text-gray-400 flex flex-col items-center justify-center">
                                <i class="fas fa-info-circle mb-4 text-4xl text-gray-500"></i>
                                <p class="text-center">No groups defined yet.</p>
                            </div>
                        <?php else: ?>
                            <div class="overflow-x-auto rounded-xl border border-dark-600/50">
                                <table class="w-full text-sm text-left">
                                    <thead class="text-xs uppercase bg-dark-700/80">
                                        <tr>
                                            <th class="px-6 py-3 text-gray-300 font-medium">Name</th>
                                            <th class="px-6 py-3 text-gray-300 font-medium">Flag</th>
                                            <th class="px-6 py-3 text-gray-300 font-medium">Description</th>
                                            <th class="px-6 py-3 text-gray-300 font-medium">Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody class="divide-y divide-dark-600/30">
                                        <?php foreach ($available_groups as $group): ?>
                                            <tr class="bg-dark-700/30 hover:bg-dark-700/50 transition-colors">
                                                <td class="px-6 py-4 font-medium text-white">
                                                    <?php if (strtolower($group['name']) === 'vip'): ?>
                                                        <span class="inline-flex items-center rounded-full bg-blue-400/10 px-2 py-0.5 text-xs font-medium text-blue-400 mr-2">
                                                            <i class="fas fa-gem"></i>
                                                        </span>
                                                    <?php elseif (strtolower($group['name']) === 'svip'): ?>
                                                        <span class="inline-flex items-center rounded-full bg-purple-400/10 px-2 py-0.5 text-xs font-medium text-purple-400 mr-2">
                                                            <i class="fas fa-crown"></i>
                                                        </span>
                                                    <?php else: ?>
                                                        <span class="inline-flex items-center rounded-full bg-gray-400/10 px-2 py-0.5 text-xs font-medium text-gray-400 mr-2">
                                                            <i class="fas fa-tag"></i>
                                                        </span>
                                                    <?php endif; ?>
                                                    <?= htmlspecialchars($group['name']) ?>
                                                </td>
                                                <td class="px-6 py-4 font-mono text-xs">
                                                    <?= htmlspecialchars($group['flag']) ?>
                                                </td>
                                                <td class="px-6 py-4 text-gray-300">
                                                    <?= htmlspecialchars($group['description'] ?? '') ?>
                                                </td>
                                                <td class="px-6 py-4">
                                                    <a href="edit_group.php?id=<?= $group['id'] ?>" 
                                                       class="inline-flex items-center px-3 py-1 bg-yellow-600/80 hover:bg-yellow-600 text-white rounded-lg transition-colors text-xs">
                                                        <i class="fas fa-edit mr-1"></i> Edit
                                                    </a>
                                                </td>
                                            </tr>
                                        <?php endforeach; ?>
                                    </tbody>
                                </table>
                            </div>
                        <?php endif; ?>
                    </div>
                    
                    <!-- Add New Group Form -->
                    <div class="bg-dark-800/50 rounded-xl p-6 shadow-xl border border-green-500/20 backdrop-blur-sm relative overflow-hidden">
                        <div class="absolute -bottom-24 -right-24 w-48 h-48 bg-green-500/10 rounded-full blur-3xl"></div>
                        
                        <div class="flex items-center mb-6 relative">
                            <div class="bg-green-500/10 p-3 rounded-lg mr-4">
                                <i class="fas fa-plus-circle text-green-400 text-2xl"></i>
                            </div>
                            <h2 class="text-xl font-bold text-green-400">Create New VIP Group</h2>
                        </div>
                        
                        <form method="post" action="">
                            <input type="hidden" name="action" value="add_new_group">
                            <div class="space-y-4">
                                <div>
                                    <label class="block text-sm font-medium text-gray-400 mb-1">Group Name</label>
                                    <div class="relative">
                                        <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                            <i class="fas fa-layer-group text-gray-500"></i>
                                        </span>
                                        <input 
                                            type="text" 
                                            name="name" 
                                            required 
                                            class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent transition duration-200 outline-none"
                                            placeholder="Enter group name"
                                        >
                                    </div>
                                </div>
                                <div>
                                    <label class="block text-sm font-medium text-gray-400 mb-1">Permission Flag</label>
                                    <div class="relative">
                                        <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                            <i class="fas fa-flag text-gray-500"></i>
                                        </span>
                                        <input 
                                            type="text" 
                                            name="flag" 
                                            required 
                                            placeholder="e.g. @mesharsky/custom"
                                            class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent transition duration-200 outline-none"
                                        >
                                    </div>
                                </div>
                                <div>
                                    <label class="block text-sm font-medium text-gray-400 mb-1">Description (Optional)</label>
                                    <div class="relative">
                                        <span class="absolute top-3 left-3 flex items-center pointer-events-none">
                                            <i class="fas fa-info-circle text-gray-500"></i>
                                        </span>
                                        <textarea 
                                            name="description" 
                                            rows="3" 
                                            class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-transparent transition duration-200 outline-none"
                                            placeholder="Enter group description"
                                        ></textarea>
                                    </div>
                                </div>
                                <button 
                                    type="submit" 
                                    class="w-full px-4 py-3 bg-gradient-to-r from-green-600 to-green-700 text-white rounded-lg hover:from-green-500 hover:to-green-600 transition duration-300 shadow-md hover:shadow-lg flex items-center justify-center"
                                >
                                    <i class="fas fa-plus-circle mr-2"></i>Create Group
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <?php if (isset($confirmation_data) && $confirmation_data['needs_confirmation']): ?>
    <!-- Group Extension Confirmation Modal -->
    <div 
        x-data="{ extensionType: 'replace' }"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm"
    >
        <!-- Modal Content -->
        <div class="bg-dark-800 rounded-xl p-6 max-w-md w-full mx-4 shadow-2xl border border-yellow-500/20 z-10 relative overflow-hidden">
            <div class="absolute -top-24 -right-24 w-48 h-48 bg-yellow-500/10 rounded-full blur-3xl"></div>
            
            <div class="relative">
                <h3 class="text-xl font-bold text-yellow-400 mb-4 flex items-center">
                    <i class="fas fa-exclamation-triangle mr-2"></i>
                    Group Already Exists
                </h3>
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
                    <button 
                        onclick="closeModal()" 
                        class="px-4 py-2 bg-dark-600 text-white rounded-lg hover:bg-dark-500 transition duration-200"
                    >
                        Cancel
                    </button>
                    <button 
                        onclick="submitExtendForm()"
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

    <script>
        function closeModal() {
            window.location.reload();
        }
        
        function submitExtendForm() {
            document.getElementById('extend-form').submit();
        }
        
        // Toggle inactive players
        function toggleInactive(checked) {
            var url = new URL(window.location.href);
            url.searchParams.set('show_inactive', checked ? '1' : '0');
            url.searchParams.set('page', 1);
            window.location.href = url.toString();
        }
        
        // Change results per page
        function changeResultsPerPage(limit) {
            var url = new URL(window.location.href);
            url.searchParams.set('limit', limit);
            url.searchParams.set('page', 1);
            window.location.href = url.toString();
        }
    </script>
</body>
</html>