<?php
require_once 'db_connection.php';
require_once 'groups_manager.php';

require_admin();

$group_id = isset($_GET['id']) ? intval($_GET['id']) : 0;

if ($group_id <= 0) {
    header('Location: index.php');
    exit;
}

$conn = get_db_connection();

try {
    $stmt = $conn->prepare("SELECT * FROM vip_groups WHERE id = :id");
    $stmt->bindParam(':id', $group_id);
    $stmt->execute();
    $group = $stmt->fetch(PDO::FETCH_ASSOC);
    
    if (!$group) {
        header('Location: index.php');
        exit;
    }
} catch (PDOException $e) {
    $error_message = "Database error: " . $e->getMessage();
    $group = ['name' => '', 'flag' => '', 'description' => ''];
}

if ($_SERVER["REQUEST_METHOD"] == "POST" && isset($_POST['action'])) {
    switch ($_POST['action']) {
        case 'update_group':
            $name = $_POST['name'] ?? $group['name'];
            $flag = $_POST['flag'] ?? $group['flag'];
            $description = $_POST['description'] ?? $group['description'];
            
            try {
                $check = $conn->prepare("SELECT COUNT(*) FROM vip_groups WHERE name = :name AND id != :id");
                $check->bindParam(':name', $name);
                $check->bindParam(':id', $group_id);
                $check->execute();
                
                if ($check->fetchColumn() > 0) {
                    $error_message = "Another group with this name already exists.";
                } else {
                    $update = $conn->prepare("UPDATE vip_groups SET name = :name, flag = :flag, description = :description WHERE id = :id");
                    $update->bindParam(':name', $name);
                    $update->bindParam(':flag', $flag);
                    $update->bindParam(':description', $description);
                    $update->bindParam(':id', $group_id);
                    
                    if ($update->execute()) {
                        $success_message = "Group updated successfully!";
                        
                        if ($name !== $group['name']) {
                            $update_refs = $conn->prepare("UPDATE player_groups SET group_name = :new_name WHERE group_name = :old_name");
                            $update_refs->bindParam(':new_name', $name);
                            $update_refs->bindParam(':old_name', $group['name']);
                            $update_refs->execute();
                        }
                        
                        $stmt = $conn->prepare("SELECT * FROM vip_groups WHERE id = :id");
                        $stmt->bindParam(':id', $group_id);
                        $stmt->execute();
                        $group = $stmt->fetch(PDO::FETCH_ASSOC);
                    } else {
                        $error_message = "Failed to update group.";
                    }
                }
            } catch (PDOException $e) {
                $error_message = "Database error: " . $e->getMessage();
            }
            break;
            
        case 'delete_group':
            try {
                $check = $conn->prepare("SELECT COUNT(*) FROM player_groups WHERE group_name = :group_name");
                $check->bindParam(':group_name', $group['name']);
                $check->execute();
                
                if ($check->fetchColumn() > 0) {
                    $error_message = "Cannot delete group because it is assigned to one or more players.";
                } else {
                    $delete = $conn->prepare("DELETE FROM vip_groups WHERE id = :id");
                    $delete->bindParam(':id', $group_id);
                    
                    if ($delete->execute()) {
                        header('Location: index.php');
                        exit;
                    } else {
                        $error_message = "Failed to delete group.";
                    }
                }
            } catch (PDOException $e) {
                $error_message = "Database error: " . $e->getMessage();
            }
            break;
    }
}

try {
    $stats = [
        'total_players' => 0,
        'active_players' => 0,
        'expired_players' => 0
    ];
    
    $stmt = $conn->prepare("SELECT COUNT(*) FROM player_groups WHERE group_name = :group_name");
    $stmt->bindParam(':group_name', $group['name']);
    $stmt->execute();
    $stats['total_players'] = $stmt->fetchColumn();
    
    $stmt = $conn->prepare("SELECT COUNT(*) FROM player_groups WHERE group_name = :group_name AND (expires = 0 OR expires > :now)");
    $stmt->bindParam(':group_name', $group['name']);
    $now = time();
    $stmt->bindParam(':now', $now);
    $stmt->execute();
    $stats['active_players'] = $stmt->fetchColumn();
    
    // Expired players
    $stats['expired_players'] = $stats['total_players'] - $stats['active_players'];
} catch (PDOException $e) {
    error_log("Error getting group stats: " . $e->getMessage());
    $stats = ['total_players' => 0, 'active_players' => 0, 'expired_players' => 0];
}
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Edit VIP Group - VIP Manager</title>
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
                    <i class="fas fa-layer-group mr-3"></i>Edit VIP Group
                </h1>
                <div>
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

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
            <!-- Edit Group Form -->
            <div class="lg:col-span-2 bg-dark-800/50 rounded-xl p-6 shadow-xl border border-yellow-500/20 backdrop-blur-sm relative overflow-hidden">
                <div class="absolute -top-24 -right-24 w-48 h-48 bg-yellow-500/10 rounded-full blur-3xl"></div>
                <div class="absolute -bottom-24 -left-24 w-48 h-48 bg-orange-500/10 rounded-full blur-3xl"></div>
                
                <div class="relative">
                    <div class="flex items-center mb-6">
                        <div class="bg-yellow-500/10 p-3 rounded-lg mr-4">
                            <i class="fas fa-info-circle text-yellow-400 text-2xl"></i>
                        </div>
                        <h2 class="text-xl font-bold text-yellow-400">Group Information</h2>
                    </div>
                    
                    <form method="post" action="">
                        <input type="hidden" name="action" value="update_group">
                        <div class="space-y-4">
                            <div>
                                <label class="block text-sm font-medium text-gray-400 mb-1">Group Name</label>
                                <div class="relative">
                                    <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                        <i class="fas fa-tag text-gray-500"></i>
                                    </span>
                                    <input 
                                        type="text" 
                                        name="name" 
                                        value="<?= htmlspecialchars($group['name']) ?>" 
                                        required
                                        class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-yellow-500 focus:border-transparent transition duration-200 outline-none"
                                    >
                                </div>
                                <p class="text-xs text-gray-500 mt-1 flex items-center">
                                    <i class="fas fa-info-circle mr-1"></i>
                                    Changing the name will update all player associations
                                </p>
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
                                        value="<?= htmlspecialchars($group['flag']) ?>" 
                                        required
                                        class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-yellow-500 focus:border-transparent transition duration-200 outline-none"
                                    >
                                </div>
                                <p class="text-xs text-gray-500 mt-1 flex items-center">
                                    <i class="fas fa-info-circle mr-1"></i>
                                    Use format like @mesharsky/vip
                                </p>
                            </div>
                            
                            <div>
                                <label class="block text-sm font-medium text-gray-400 mb-1">Description</label>
                                <div class="relative">
                                    <span class="absolute top-3 left-3 flex items-center pointer-events-none">
                                        <i class="fas fa-align-left text-gray-500"></i>
                                    </span>
                                    <textarea 
                                        name="description" 
                                        rows="3" 
                                        class="w-full pl-10 pr-4 py-3 bg-dark-700 border border-dark-600 rounded-lg focus:ring-2 focus:ring-yellow-500 focus:border-transparent transition duration-200 outline-none"
                                    ><?= htmlspecialchars($group['description'] ?? '') ?></textarea>
                                </div>
                            </div>
                            
                            <div class="flex flex-col sm:flex-row gap-4 pt-2">
                                <button 
                                    type="submit" 
                                    class="flex-1 px-4 py-3 bg-gradient-to-r from-yellow-600 to-yellow-700 text-white rounded-lg hover:from-yellow-500 hover:to-yellow-600 transition duration-300 shadow-md hover:shadow-lg flex items-center justify-center"
                                >
                                    <i class="fas fa-save mr-2"></i>Update Group
                                </button>
                                
                                <button 
                                    type="submit" 
                                    form="delete-form" 
                                    class="px-4 py-3 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-lg hover:from-red-500 hover:to-red-600 transition duration-300 shadow-md hover:shadow-lg flex items-center justify-center"
                                    onclick="return confirm('Are you sure you want to delete this group? This cannot be undone!');"
                                >
                                    <i class="fas fa-trash-alt mr-2"></i>Delete Group
                                </button>
                            </div>
                        </div>
                    </form>
                    
                    <!-- Separate form for delete to avoid accidental submissions -->
                    <form id="delete-form" method="post" action="" class="hidden">
                        <input type="hidden" name="action" value="delete_group">
                    </form>
                </div>
            </div>
            
            <!-- Group Usage Stats -->
            <div class="lg:col-span-1 bg-dark-800/50 rounded-xl p-6 shadow-xl border border-purple-500/20 backdrop-blur-sm relative overflow-hidden">
                <div class="absolute -top-24 -right-24 w-48 h-48 bg-purple-500/10 rounded-full blur-3xl"></div>
                
                <div class="relative">
                    <div class="flex items-center mb-6">
                        <div class="bg-purple-500/10 p-3 rounded-lg mr-4">
                            <i class="fas fa-chart-pie text-purple-400 text-2xl"></i>
                        </div>
                        <h2 class="text-xl font-bold text-purple-400">Group Usage</h2>
                    </div>
                    
                    <div class="space-y-4">
                        <div class="bg-dark-700/60 rounded-xl p-4 border border-dark-600/50 transition-all duration-300 hover:bg-dark-700/80">
                            <div class="flex justify-between items-start">
                                <div>
                                    <h3 class="text-xs uppercase text-gray-400 mb-1">Total Players</h3>
                                    <p class="text-2xl font-bold text-white"><?= $stats['total_players'] ?></p>
                                </div>
                                <div class="bg-blue-500/10 p-2 rounded-lg">
                                    <i class="fas fa-users text-blue-400"></i>
                                </div>
                            </div>
                        </div>
                        
                        <div class="bg-dark-700/60 rounded-xl p-4 border border-dark-600/50 transition-all duration-300 hover:bg-dark-700/80">
                            <div class="flex justify-between items-start">
                                <div>
                                    <h3 class="text-xs uppercase text-gray-400 mb-1">Active Players</h3>
                                    <p class="text-2xl font-bold text-green-400"><?= $stats['active_players'] ?></p>
                                </div>
                                <div class="bg-green-500/10 p-2 rounded-lg">
                                    <i class="fas fa-user-check text-green-400"></i>
                                </div>
                            </div>
                        </div>
                        
                        <div class="bg-dark-700/60 rounded-xl p-4 border border-dark-600/50 transition-all duration-300 hover:bg-dark-700/80">
                            <div class="flex justify-between items-start">
                                <div>
                                    <h3 class="text-xs uppercase text-gray-400 mb-1">Expired Players</h3>
                                    <p class="text-2xl font-bold text-red-400"><?= $stats['expired_players'] ?></p>
                                </div>
                                <div class="bg-red-500/10 p-2 rounded-lg">
                                    <i class="fas fa-user-times text-red-400"></i>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="mt-8 bg-dark-700/40 rounded-xl p-4 border border-dark-600/30">
                        <h3 class="text-xs uppercase text-gray-400 mb-3 flex items-center">
                            <i class="fas fa-info-circle mr-2"></i>Group Details
                        </h3>
                        <div class="space-y-2 text-sm">
                            <div class="flex justify-between">
                                <span class="text-gray-400">Group ID:</span>
                                <span class="text-gray-300 font-mono"><?= $group_id ?></span>
                            </div>
                            <div class="flex justify-between">
                                <span class="text-gray-400">Created:</span>
                                <span class="text-gray-300"><?= isset($group['created_at']) ? date('Y-m-d H:i', strtotime($group['created_at'])) : 'Unknown' ?></span>
                            </div>
                        </div>
                    </div>
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