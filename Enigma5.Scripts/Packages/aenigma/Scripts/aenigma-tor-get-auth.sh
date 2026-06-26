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

show_help() {
    echo "Usage: $0 -s SERVICE_NAME"
    echo ""
    echo "Options:"
    echo "  -s SERVICE_NAME     Name of configured Onion Service."
    echo ""
    echo "Example:"
    echo "  sudo $0 -s aenigma-dashboard"
    exit 1
}
 
[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }
 
# Parse command line arguments
while getopts "s:h" opt; do
    case $opt in
        s) SERVICE_NAME=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done
 
if [[ ! -v SERVICE_NAME ]]; then
    echo "Error: SERVICE_NAME is required."
    show_help
fi
 
TOR_SERVICE_DIR="/var/lib/tor/$SERVICE_NAME"
AUTHORIZED_CLIENTS_DIR="$TOR_SERVICE_DIR/authorized_clients"
 
if [[ ! -d "$AUTHORIZED_CLIENTS_DIR" ]]; then
    echo "Error: $AUTHORIZED_CLIENTS_DIR not found. It seems like Tor was not configured."
    exit 1
fi
 
found_any=0
for auth_path in "$AUTHORIZED_CLIENTS_DIR"/*.auth.private; do
    [[ -f "$auth_path" ]] || continue
    auth_name="$(basename "$auth_path")"
    echo "name: $auth_name"
    echo "access key:  $(cat "$auth_path")"
    echo ""
    found_any=1
done
 
if [[ $found_any -eq 0 ]]; then
    echo "No authentication keys found for service: $SERVICE_NAME"
fi
