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
PUBLIC_KEY_FILE="$KEYS_DIR/public-key.pem"
KEY_SIZE="4096"

if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

mkdir -pv "$KEYS_DIR"

# Generate the private key with the given size and file name
echo "Generating private key ... "
openssl genrsa -aes256 -out "$PRIVATE_KEY_FILE" "$KEY_SIZE"
echo "Done."

# Extract the public key from the private key and save it to the specified file
echo "Exporting public key ... "
openssl rsa -in "$PRIVATE_KEY_FILE" -outform PEM -pubout -out "$PUBLIC_KEY_FILE"
echo "Done."

echo "Keys generated successfully:"
echo "Private key: $PRIVATE_KEY_FILE"
echo "Public key: $PUBLIC_KEY_FILE"

# Changing ownership for generated files
echo "Changing keys ownership to $SERVICE_USER ..."
chown -v "$SERVICE_USER":"$SERVICE_USER" "$PRIVATE_KEY_FILE"
chown -v "$SERVICE_USER":"$SERVICE_USER" "$PUBLIC_KEY_FILE"
chmod -R 700 "$KEYS_DIR"

echo "Done."
exit 0
