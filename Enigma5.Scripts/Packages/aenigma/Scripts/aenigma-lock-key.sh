#!/bin/bash

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

SERVICE_USER="aenigma"
KEYS_DIR="/usr/local/etc/$SERVICE_USER"
PRIVATE_KEY_FILE="$KEYS_DIR/private-key.pem"
PRIVATE_KEY_LOCKED_FILE="$KEYS_DIR/private-key-locked.pem"

[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }

[[ ! -f "$PRIVATE_KEY_FILE" ]] && { echo "ERROR: Key not found: $PRIVATE_KEY_FILE"; exit 1; }

openssl pkey -in "$PRIVATE_KEY_FILE" -out "$PRIVATE_KEY_LOCKED_FILE" -aes256
mv "$PRIVATE_KEY_LOCKED_FILE" "$PRIVATE_KEY_FILE"
chown -v "$SERVICE_USER":"$SERVICE_USER" "$PRIVATE_KEY_FILE"
chmod -v 700 "$PRIVATE_KEY_FILE"

echo "Key successfully locked: $PRIVATE_KEY_FILE"
