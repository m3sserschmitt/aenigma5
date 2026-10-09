#!/bin/sh

# Aenigma - Federated messaging system
# Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

# Get the directory of the running script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# The keys are written next to the application, wherever the script is started from
APP_DIR="$SCRIPT_DIR/../Enigma5.App"

# Same size as the keys the node creates by itself
openssl genrsa -out "$APP_DIR/private-key.pem" 4096
openssl rsa -in "$APP_DIR/private-key.pem" -outform PEM -pubout -out "$APP_DIR/public-key.pem"

exit 0
