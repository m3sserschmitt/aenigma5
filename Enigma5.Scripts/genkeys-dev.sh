#!/bin/sh

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

# Get the directory of the running script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Combine it with the relative path "../Scripts"
SCRIPTS_PATH="$SCRIPT_DIR/../Enigma5.App"

openssl genrsa -out ../Enigma5.App/private-key.pem
openssl rsa -in ../Enigma5.App/private-key.pem -outform PEM -pubout -out ../Enigma5.App/public-key.pem

exit 0
