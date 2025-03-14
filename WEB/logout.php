<?php

require_once 'Config/config.php';
require_once 'steamauth.php';

require_admin();

steam_logout();

header('Location: login.php');
exit;