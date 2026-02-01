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

set -Eeuo pipefail

TORRC_FILE="/etc/tor/torrc"
TOR_USER="debian-tor"

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -l LOCAL_ADDRESS -o ONION_PORT -s SERVICE_NAME [ -u AUTHORIZED_USER | -c CONFIG_ADDRESS ]"
    echo ""
    echo "Options:"
    echo "  -l LOCAL_ADDRESS    The local address your service is running (e.g. 127.0.0.1:8080)."
    echo "  -o ONION_PORT       The port on which the onion service should accept connections (e.g. 80)."
    echo "  -s SERVICE_NAME     Name of configured Onion Service (e.g. \"aenigma\")."
    echo "  -u AUTHORIZED_USER  Add TOR service authorization for user (e.g. \"aenigma\" sets \"HiddenServiceAuthorizeClient stealth aenigma\")."
    echo "  -c CONFIG_ADDRESS   Add hidden service address to aenigma config (1 -> update, 0 -> do not update)."
    echo ""
    echo "Example:"
    echo "  sudo $0 -l 127.0.0.1:8080 -o 80 -s aenigma-dashboard -u admin"
    exit 1
}

if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

# Check if the script is run with sufficient arguments
if [ "$#" -lt 6 ]; then
    show_help
fi

# Parse command line arguments
while getopts "l:o:s:u:c:h" opt; do
    case $opt in
        l) LOCAL_ADDRESS=$OPTARG ;;
        o) ONION_PORT=$OPTARG ;;
        s) SERVICE_NAME=$OPTARG ;;
        u) AUTHORIZED_USER=$OPTARG ;;
        c) CONFIG_ADDRESS=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if DOMAIN and APP_PORT are provided
if [[ ! -v LOCAL_ADDRESS || ! -v ONION_PORT || ! -v SERVICE_NAME ]]; then
    echo "Error: LOCAL_ADDRESS, ONION_PORT and SERVICE_NAME are required."
    show_help
    exit 1
fi

TOR_SERVICE_DIR="/var/lib/tor/$SERVICE_NAME"
ONION_SERVICE_ADDRESS_FILE="$TOR_SERVICE_DIR/hostname"

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
    chmod g-s "$TOR_SERVICE_DIR"
    chmod -v 700 "$TOR_SERVICE_DIR"
fi

# ------------------------------
# 5. Configure torrc (idempotent)
# ------------------------------
ONION_CONFIG="
HiddenServiceDir ${TOR_SERVICE_DIR}
HiddenServicePort ${ONION_PORT} ${LOCAL_ADDRESS}"

if [[ -v AUTHORIZED_USER ]]; then

ONION_CONFIG="${ONION_CONFIG}
HiddenServiceAuthorizeClient stealth ${AUTHORIZED_USER}"

aenigma-tor-auth -s "$SERVICE_NAME" -u "$AUTHORIZED_USER"
fi

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
    if [[ -f "$ONION_SERVICE_ADDRESS_FILE" ]]; then
        break
    fi
    sleep 1
done

if [[ ! -f "$ONION_SERVICE_ADDRESS_FILE" ]]; then
    echo "Error: Onion service address was not generated."
    exit 1
fi

# ------------------------------
# 8. Output onion address
# ------------------------------
ONION_SERVICE_ADDRESS="$(cat "$ONION_SERVICE_ADDRESS_FILE")"
echo -e "\e[31mOnion service address\e[0m: $ONION_SERVICE_ADDRESS"
if [[ -v CONFIG_ADDRESS && ${CONFIG_ADDRESS} -eq 1 ]]; then
aenigma-config -p "OnionService" -v "$ONION_SERVICE_ADDRESS"
fi

exit 0
