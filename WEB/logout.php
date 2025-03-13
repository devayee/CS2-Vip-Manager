<?php

require_once 'Config/config.php';
require_once 'steamauth.php';

steam_logout();

header('Location: login.php');
exit;