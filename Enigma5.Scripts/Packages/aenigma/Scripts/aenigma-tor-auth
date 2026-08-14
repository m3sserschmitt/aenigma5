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
 
TOR_USER="debian-tor"
 
show_help() {
    echo "Usage: $0 -s SERVICE_NAME -u AUTHORIZED_USER"
    echo ""
    echo "Options:"
    echo "  -s SERVICE_NAME     Name of configured Onion Service."
    echo "  -u AUTHORIZED_USER  Add TOR service authorization for user."
    echo ""
    echo "Example:"
    echo "  sudo $0 -s aenigma-dashboard -u admin"
    exit 1
}
 
[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }
 
# Check if the script is run with sufficient arguments
if [ "$#" -lt 4 ]; then
    show_help
fi
 
# Parse command line arguments
while getopts "s:u:h" opt; do
    case $opt in
        s) SERVICE_NAME=$OPTARG ;;
        u) AUTHORIZED_USER=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done
 
if [[ ! -v SERVICE_NAME || ! -v AUTHORIZED_USER ]]; then
    echo "Error: AUTHORIZED_USER and SERVICE_NAME are required."
    show_help
fi
 
TOR_SERVICE_DIR="/var/lib/tor/$SERVICE_NAME"
AUTHORIZED_CLIENTS_DIR="$TOR_SERVICE_DIR/authorized_clients"
AUTH_FILE="$AUTHORIZED_CLIENTS_DIR/$AUTHORIZED_USER.auth"
AUTH_FILE_PRIVATE="$AUTHORIZED_CLIENTS_DIR/$AUTHORIZED_USER.auth.private"
 
mkdir -pv "$AUTHORIZED_CLIENTS_DIR"
 
# Create a temp directory readable only by root
TEMP_DIR=$(mktemp -d)
chmod 700 "$TEMP_DIR"
 
PUBLIC_PEM_FILE="$TEMP_DIR/k1.pub.pem"
PRIVATE_PEM_FILE="$TEMP_DIR/k1.prv.pem"
PUBLIC_KEY_FILE="$TEMP_DIR/k1.pub.key"
PRIVATE_KEY_FILE="$TEMP_DIR/k1.prv.key"
 
# Ensure temp directory is cleaned up on exit
trap 'rm -rf "$TEMP_DIR"' EXIT
 
openssl genpkey -algorithm x25519 -out "$PRIVATE_PEM_FILE"
 
cat "$PRIVATE_PEM_FILE" \
    | grep -v " PRIVATE KEY" \
    | base64pem -d \
    | tail --bytes=32 \
    | base32 \
    | sed 's/=//g' > "$PRIVATE_KEY_FILE"
 
openssl pkey -in "$PRIVATE_PEM_FILE" -pubout \
    | grep -v " PUBLIC KEY" \
    | base64pem -d \
    | tail --bytes=32 \
    | base32 \
    | sed 's/=//g' > "$PUBLIC_KEY_FILE"
 
echo "descriptor:x25519:$(cat "$PUBLIC_KEY_FILE")" > "$AUTH_FILE"
echo "$(cat "$PRIVATE_KEY_FILE")" > "$AUTH_FILE_PRIVATE"
 
chown -R "$TOR_USER:$TOR_USER" "$AUTHORIZED_CLIENTS_DIR"
chmod -R 700 "$AUTHORIZED_CLIENTS_DIR"
 
systemctl restart tor
 
echo "Access key located at '$AUTH_FILE_PRIVATE'."
