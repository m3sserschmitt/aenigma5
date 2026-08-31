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

export DEBIAN_FRONTEND=noninteractive
 
# ── Standard cleanup ──────────────────────────────────────────────────────
apt-get clean
apt-get autoremove -y
apt-get remove -y --purge tor

rm -rvf     /tmp/* /var/tmp/* /var/cache/apt/archives/*.deb
rm -vf      /etc/ssh/ssh_host_*_key /etc/ssh/ssh_host_*_key.pub

rm -vf      /var/log/aenigma/*
rm -vf      /var/lib/aenigma/public-key.pem
rm -vf      /var/lib/aenigma/private-key.pem
rm -vf      /var/lib/aenigma/db/aenigmaDb.sqlite
rm -vf      /var/lib/aenigma/db/aenigmaDb.sqlite-shm
rm -vf      /var/lib/aenigma/db/aenigmaDb.sqlite-wal

aenigma-config -p OnionService  -v null
aenigma-config -p Hostname      -v null

sudo journalctl --vacuum-time=1s 2>/dev/null || true
sudo find /var/log -type f -exec truncate -s 0 {} \;

# ── Reset machine-id ──────────────────────────────────────────────────────
# Ensures the client's first boot is correctly detected as a "first boot"
rm -vf  /etc/machine-id /var/lib/dbus/machine-id
touch   /etc/machine-id
 
# ── Zero free space ────────────
dd if=/dev/zero of=/EMPTY bs=1M 2>/dev/null || true
rm -f /EMPTY
sync
 
echo "Build complete."
echo "Users access the VM via 'vagrant ssh' — no password needed."
