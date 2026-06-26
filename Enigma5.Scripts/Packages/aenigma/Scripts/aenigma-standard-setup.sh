#!/bin/bash

# Aenigma - Federated messaging system
# Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

# This file is part of Aenigma project.

# Aenigma is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.

# Aenigma is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License
# along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.

set -Eeuo pipefail

AENIGMA_TOR_SERVICE_NAME="aenigma"
AENIGMA_DASHBOARD_TOR_SERVICE_NAME="aenigma-dashboard"
TOR_SERVICES_DIR="/var/lib/tor"
AENIGMA_TOR_SERVICE_DIR="$TOR_SERVICES_DIR/$AENIGMA_TOR_SERVICE_NAME"
AENIGMA_DASHBOARD_TOR_SERVICE_DIR="$TOR_SERVICES_DIR/$AENIGMA_DASHBOARD_TOR_SERVICE_NAME"

[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }

if [[ ! -f "$AENIGMA_TOR_SERVICE_DIR/hostname" ]]; then
    echo "Configuring onion service for aenigma service..."
    aenigma-tor -l 127.0.0.1:8080 -o 80 -s "$AENIGMA_TOR_SERVICE_NAME" -c 1
fi

if [[ ! -f "$AENIGMA_DASHBOARD_TOR_SERVICE_DIR/hostname" ]]; then
    echo "Configuring onion service for aenigma dashboard service..."
    aenigma-tor -l 127.0.0.1:8081 -o 80 -s "$AENIGMA_DASHBOARD_TOR_SERVICE_NAME" -u "$AENIGMA_DASHBOARD_TOR_SERVICE_NAME"
fi
