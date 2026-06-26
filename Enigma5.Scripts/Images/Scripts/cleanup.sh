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

# Runs as root via sudo over Packer's build-time SSH session
# (authenticated as the 'vagrant' user on the bento base box).

set -eux
 
# ── Standard cleanup ──────────────────────────────────────────────────────
apt-get clean
apt-get autoremove -y
rm -rf /tmp/* /var/tmp/* /var/cache/apt/archives/*.deb
journalctl --vacuum-size=1M 2>/dev/null || true

# ── Reset machine-id ──────────────────────────────────────────────────────
# Ensures the client's first boot is correctly detected as a "first boot"
rm -f /etc/machine-id /var/lib/dbus/machine-id
touch /etc/machine-id
 
# ── Zero free space (helps Vagrant compress the box on export) ────────────
dd if=/dev/zero of=/EMPTY bs=1M 2>/dev/null || true
rm -f /EMPTY
sync
 
echo "Build complete."
echo "Users access the VM via 'vagrant ssh' — no password needed."
