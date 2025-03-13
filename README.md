# VIP Manager Plugin

Hey everyone! I'm excited to release my **VIP Manager** plugin 🚀  

You can see the plugin and website presentation here:  
🎥 [YouTube Video](https://www.youtube.com/watch?v=-_8g3y1Z28Y)  

**‼️ REQUIRED LIBRARY FOR PLUGIN TO WORK:**  
[CS2ScreenMenuAPI](https://github.com/T3Marius/CS2ScreenMenuAPI) (Thanks @mariust3 for the nice release)  

---

## ✨ Key Features

### 🏆 VIP Group System
- **Time-Based Access**: VIP groups can be permanent or time-limited with automatic expiration
- **Night VIP**: Free VIP access during specified hours (e.g., off-peak times)
- **Smart Multi-Group Support**: Players can belong to multiple VIP groups simultaneously, with the plugin automatically providing the best benefits across all active groups (no duplicate benefits, just the highest values from each category)

### 🔧 Admin Management
- **In-game Administration**: User-friendly menu system for adding, removing, and extending VIP privileges
- **Offline Player Support**: Add/remove VIP by SteamID64 for players who aren't currently online
- **Duration Options**: Set VIP access for various periods (1 day to 1 year or permanent)
- **Group Management**: View, modify, and track all VIP users

### 🎁 VIP Benefits
- :gun: **Equipment**: Automatic armor, helmet, and defuser (CT) at round start
- **Health Bonuses**: Increased player health and maximum health
- **Grenades**: Free grenades at round start (HE, flashbang, smoke, molotov, etc.)
- **Healthshots**: Automatic healthshots at round start
- **Movement Abilities**: Multi-jump capabilities (double/triple), custom jump height, auto-bhop
- **Weapon Access**: Special weapons menu access (!guns) **(NOT YET IMPLEMENTED)**

### 👥 Player Features
- **Self-Service**: Players can check their VIP status and benefits
- **VIP Community**: View other online VIP players
- **Welcome Messages**: Custom welcome messages for VIP players
- **Notifications**: Server notifications when VIP players join or leave

### ⚙️ Technical Features
- **Multi-language Support**: Full translation system (English and Polish included)
- **Localized Date Formats**: Dates display in the player's language format
- **TOML Configuration**: Clean, readable TOML-based configuration system (because **FUCK JSON** 🤬)

---

## 📥 Installation Instructions

1. Upload all files to your server  
2. Set up your database and groups inside:  
   ```
   csgo/addons/counterstrikesharp/plugins/Mesharsky_Vip/Config/Configuration.toml
   ```
3. If you are not a dumb fuck, then you are done. If you somehow managed to break it, **ur problem.** 🤷‍♂️

---

## 🌐 WEB PANEL INSTALLATION

**PHP 8.1+ required**  
Website has been created with **PHP, TailwindCSS, and Alpine.js**  

### 🛠 Steps:
1. Upload all files into your `public_html`
2. Set up the database connection and Steam API key in:  
   ```
   public_html/Config/config.php
   ```
3. To access the panel, put your **SteamID64** in `config.php` here:

   ```php
   // Admin users who have access to the panel
   'admins' => [
       '76561198380337533', // Replace with your SteamID64
       '76561198100544780',
   ],
   ```

4. As mentioned before, if you are dumb, **you're on your own.** 🤡

---
🔥 **Enjoy your VIP Manager Plugin!** 🔥
