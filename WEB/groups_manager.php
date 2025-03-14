<?php
/**
 * Dynamic group management for VIP system
 */
require_once 'db_connection.php';

require_admin();

/**
 * Get all available VIP groups from database
 * 
 * @return array List of all groups
 */
function get_available_groups() {
    $conn = get_db_connection();
    
    try {
        $table_check = $conn->query("SHOW TABLES LIKE 'vip_groups'");
        if ($table_check->rowCount() == 0) {
            $conn->exec("CREATE TABLE IF NOT EXISTS vip_groups (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(50) NOT NULL UNIQUE,
                flag VARCHAR(50) NOT NULL,
                description TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )");
            
            $conn->exec("INSERT INTO vip_groups (name, flag, description) 
                         VALUES ('VIP', '@mesharsky/vip', 'Standard VIP access'),
                                ('SVIP', '@mesharsky/svip', 'Super VIP access')");
        }
        
        // Get all groups
        $stmt = $conn->prepare("SELECT * FROM vip_groups ORDER BY name");
        $stmt->execute();
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    } catch (PDOException $e) {
        error_log("Group query error: " . $e->getMessage());
        
        return [
            ['id' => 1, 'name' => 'VIP', 'flag' => '@mesharsky/vip', 'description' => 'Standard VIP access'],
            ['id' => 2, 'name' => 'SVIP', 'flag' => '@mesharsky/svip', 'description' => 'Super VIP access']
        ];
    }
}

/**
 * Get a specific group by name
 * 
 * @param string $name Group name
 * @return array|null Group information or null if not found
 */
function get_group_by_name($name) {
    $conn = get_db_connection();
    
    try {
        $stmt = $conn->prepare("SELECT * FROM vip_groups WHERE name = :name");
        $stmt->bindParam(':name', $name);
        $stmt->execute();
        return $stmt->fetch(PDO::FETCH_ASSOC);
    } catch (PDOException $e) {
        error_log("Get group error: " . $e->getMessage());
        return null;
    }
}

/**
 * Add a new VIP group
 * 
 * @param string $name Group name
 * @param string $flag Group permission flag
 * @param string $description Group description
 * @return bool Success or failure
 */
function add_vip_group($name, $flag, $description = '') {
    $conn = get_db_connection();
    
    try {
        // Check if group already exists
        $check = $conn->prepare("SELECT COUNT(*) FROM vip_groups WHERE name = :name");
        $check->bindParam(':name', $name);
        $check->execute();
        
        if ($check->fetchColumn() > 0) {
            return false;
        }
        
        // Add new group
        $stmt = $conn->prepare("INSERT INTO vip_groups (name, flag, description) VALUES (:name, :flag, :description)");
        $stmt->bindParam(':name', $name);
        $stmt->bindParam(':flag', $flag);
        $stmt->bindParam(':description', $description);
        return $stmt->execute();
    } catch (PDOException $e) {
        error_log("Add group error: " . $e->getMessage());
        return false;
    }
}

/**
 * Update an existing VIP group
 * 
 * @param int $id Group ID
 * @param string $name Group name
 * @param string $flag Group permission flag
 * @param string $description Group description
 * @return bool Success or failure
 */
function update_vip_group($id, $name, $flag, $description = '') {
    $conn = get_db_connection();
    
    try {
        $stmt = $conn->prepare("UPDATE vip_groups SET name = :name, flag = :flag, description = :description WHERE id = :id");
        $stmt->bindParam(':id', $id);
        $stmt->bindParam(':name', $name);
        $stmt->bindParam(':flag', $flag);
        $stmt->bindParam(':description', $description);
        return $stmt->execute();
    } catch (PDOException $e) {
        error_log("Update group error: " . $e->getMessage());
        return false;
    }
}

/**
 * Delete a VIP group
 * 
 * @param int $id Group ID
 * @return bool Success or failure
 */
function delete_vip_group($id) {
    $conn = get_db_connection();
    
    try {
        // First check if any players are using this group
        $group = $conn->prepare("SELECT name FROM vip_groups WHERE id = :id");
        $group->bindParam(':id', $id);
        $group->execute();
        $group_name = $group->fetchColumn();
        
        if (!$group_name) {
            return false;
        }
        
        $check = $conn->prepare("SELECT COUNT(*) FROM player_groups WHERE group_name = :group_name");
        $check->bindParam(':group_name', $group_name);
        $check->execute();
        
        if ($check->fetchColumn() > 0) {
            return false;
        }
        
        $stmt = $conn->prepare("DELETE FROM vip_groups WHERE id = :id");
        $stmt->bindParam(':id', $id);
        return $stmt->execute();
    } catch (PDOException $e) {
        error_log("Delete group error: " . $e->getMessage());
        return false;
    }
}