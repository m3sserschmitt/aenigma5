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

SERVICE_NAME="aenigma"

[[ $EUID -ne 0 ]] && { echo "ERROR: Run as root: sudo bash $0"; exit 1; }

echo "Enabling $SERVICE_NAME service ... "
systemctl enable "$SERVICE_NAME"
echo "Done."

echo "Starting $SERVICE_NAME service ..."
systemctl restart "$SERVICE_NAME"
echo "Done."
exit 0
