#!/bin/bash

# Aenigma - Federal messaging system
# Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

set -e

SERVICE_NAME="aenigma"
TOR_SERVICE_DIR="/var/lib/tor/$SERVICE_NAME-onion-service"
TORRC_FILE="/etc/tor/torrc"
TOR_USER="debian-tor"

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -l LOCAL_PORT -o ONION_PORT"
    echo ""
    echo "Options:"
    echo "  -l LOCAL_PORT  The port on which local service is running (e.g. 8080)."
    echo "  -o ONION_PORT  The port on which the onion service should accept connections (e.g. 80)."
    echo ""
    echo "Example:"
    echo "  sudo $0 -l 8080 -o 80"
    exit 1
}

# Check if the script is run with sufficient arguments
if [ "$#" -lt 4 ]; then
    show_help
fi

# Parse command line arguments
while getopts "l:o:h" opt; do
    case $opt in
        l) LOCAL_PORT=$OPTARG ;;
        o) ONION_PORT=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if DOMAIN and APP_PORT are provided
if [ -z "$LOCAL_PORT" ] || [ -z "$ONION_PORT" ]; then
    echo "Error: Both local port and onion port are required."
    show_help
fi

LOCAL_SERVICE="127.0.0.1:$LOCAL_PORT"

# ------------------------------
# 1. Check root
# ------------------------------
if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

# ------------------------------
# 2. Install Tor if missing
# ------------------------------
if ! command -v tor >/dev/null 2>&1; then
    apt update
    apt install -y tor
fi

# ------------------------------
# 3. Enable and start Tor
# ------------------------------
systemctl enable tor
systemctl start tor

# ------------------------------
# 4. Ensure onion service directory
# ------------------------------
if [[ ! -d "$TOR_SERVICE_DIR" ]]; then
    mkdir -pv "$TOR_SERVICE_DIR"
    chown -Rv "$TOR_USER:$TOR_USER" "$TOR_SERVICE_DIR"
    chmod 700 "$TOR_SERVICE_DIR"
fi

# ------------------------------
# 5. Configure torrc (idempotent)
# ------------------------------
ONION_CONFIG="
HiddenServiceDir ${TOR_SERVICE_DIR}
HiddenServicePort ${ONION_PORT} ${LOCAL_SERVICE}
"
if ! grep -q "HiddenServiceDir ${TOR_SERVICE_DIR}" "$TORRC_FILE"; then
    echo "$ONION_CONFIG" >> "$TORRC_FILE"
fi

# ------------------------------
# 6. Restart Tor to apply config
# ------------------------------
systemctl restart tor

# ------------------------------
# 7. Wait for hostname generation
# ------------------------------
echo "Waiting for onion service address to become available..."
for i in {1..120}; do
    if [[ -f "${TOR_SERVICE_DIR}/hostname" ]]; then
        break
    fi
    sleep 1
done

if [[ ! -f "${TOR_SERVICE_DIR}/hostname" ]]; then
    echo "Error: Onion service address was not generated"
    exit 1
fi

# ------------------------------
# 8. Output onion address
# ------------------------------
ONION_ADDRESS="$(cat ${TOR_SERVICE_DIR}/hostname)"
echo "Onion service address: $ONION_ADDRESS"

aenigma-set-config -p OnionService -v $ONION_ADDRESS
