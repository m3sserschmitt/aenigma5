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

PUBLIC_PEM_FILE="/tmp/k1.pub.pem"
PRIVATE_PEM_FILE="/tmp/k1.prv.pem"
PUBLIC_KEY_FILE="/tmp/k1.pub.key"
PRIVATE_KEY_FILE="/tmp/k1.prv.key"
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

if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

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
    exit 1
fi

TOR_SERVICE_DIR="/var/lib/tor/$SERVICE_NAME"
AUTHORIZED_CLIENTS_DIR="$TOR_SERVICE_DIR/authorized_clients"

if [[ ! -d "$AUTHORIZED_CLIENTS_DIR" ]]; then
    mkdir -pv "$AUTHORIZED_CLIENTS_DIR"
fi

chown -Rv "$TOR_USER:$TOR_USER" "$AUTHORIZED_CLIENTS_DIR"
chmod -v 700 "$AUTHORIZED_CLIENTS_DIR"

AUTH_FILE="$AUTHORIZED_CLIENTS_DIR/$AUTHORIZED_USER.auth"

openssl genpkey -algorithm x25519 -out "$PRIVATE_PEM_FILE"

cat "$PRIVATE_PEM_FILE" |\
    grep -v " PRIVATE KEY" |\
    base64pem -d |\
    tail --bytes=32 |\
    base32 |\
    sed 's/=//g' > "$PRIVATE_KEY_FILE"

openssl pkey -in "$PRIVATE_PEM_FILE" -pubout |\
    grep -v " PUBLIC KEY" |\
    base64pem -d |\
    tail --bytes=32 |\
    base32 |\
    sed 's/=//g' > "$PUBLIC_KEY_FILE"

echo "descriptor:x25519:$(cat "$PUBLIC_KEY_FILE")" | sudo tee $AUTH_FILE
chown -v "$TOR_USER:$TOR_USER" "$AUTH_FILE"
echo -e "\e[31mX25519 Private Key\e[0m: $(cat "$PRIVATE_KEY_FILE")"

rm -f "$PUBLIC_PEM_FILE" "$PRIVATE_PEM_FILE" "$PRIVATE_KEY_FILE" "$PUBLIC_KEY_FILE"
