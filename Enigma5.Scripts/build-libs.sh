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

set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
LIBAENIGMA_DIR="$SCRIPT_DIR/../Libaenigma7"

$LIBAENIGMA_DIR/build.sh
$LIBAENIGMA_DIR/build-arm64.sh

cp -v $LIBAENIGMA_DIR/build/libaenigma.so $LIBAENIGMA_DIR/../Enigma5.Crypto/runtimes/linux-$(dpkg --print-architecture)/native/
cp -v $LIBAENIGMA_DIR/build-arm64/libaenigma.so $LIBAENIGMA_DIR/../Enigma5.Crypto/runtimes/linux-arm64/native/

exit 0
