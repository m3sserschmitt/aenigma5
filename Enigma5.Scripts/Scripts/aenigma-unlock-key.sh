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

SERVICE_USER="aenigma"
KEYS_DIR="/usr/local/etc/$SERVICE_USER"
PRIVATE_KEY_FILE="$KEYS_DIR/private-key.pem"
PRIVATE_KEY_UNLOCKED_FILE="$KEYS_DIR/private-key-unlocked.pem"

if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

openssl pkey -in "$PRIVATE_KEY_FILE" -out "$PRIVATE_KEY_UNLOCKED_FILE"
mv "$PRIVATE_KEY_UNLOCKED_FILE" "$PRIVATE_KEY_FILE"
chown -v "$SERVICE_USER":"$SERVICE_USER" "$PRIVATE_KEY_FILE"
chmod -R 700 "$KEYS_DIR"
