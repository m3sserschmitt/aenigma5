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

set -eux
 
export DEBIAN_FRONTEND=noninteractive
 
# ── Base update ─────────────────────────────────────────────────────────
apt update
apt install -y gnupg ca-certificates curl
 
# ── Tor: install straight from Debian's own repo
# ── Install Aenigma repo
curl -fsSL https://packages.aenigma.ro/aenigma.gpg.key | gpg --dearmor -o /etc/apt/trusted.gpg.d/packages.aenigma.ro.gpg
echo "deb [arch=amd64 signed-by=/etc/apt/trusted.gpg.d/packages.aenigma.ro.gpg] \
https://packages.aenigma.ro stable main" | tee /etc/apt/sources.list.d/packages.aenigma.ro.list

apt update
apt install -y tor aenigma aenigma-image-utils

systemctl enable tor
systemctl enable aenigma
systemctl enable aenigma-standard-setup
systemctl enable regenerate-ssh-host-keys

# ── MOTD ──────────────────────────────────────────────────────────────────
# The aenigma-status script is shipped by the aenigma .deb package and
# installed at /usr/local/bin/aenigma-status. 
# Symlink aenigma's status script into the MOTD directory
ln -sf /usr/local/bin/aenigma-status /etc/update-motd.d/99-zz-aenigma-status
 
# Ensure PAM's pam_motd module is active for both console and SSH logins
if ! grep -q "pam_motd" /etc/pam.d/login 2>/dev/null; then
    echo "session optional pam_motd.so motd=/run/motd.dynamic" \
        >> /etc/pam.d/login
fi
if ! grep -q "pam_motd" /etc/pam.d/sshd 2>/dev/null; then
    echo "session optional pam_motd.so motd=/run/motd.dynamic" \
        >> /etc/pam.d/sshd
fi
