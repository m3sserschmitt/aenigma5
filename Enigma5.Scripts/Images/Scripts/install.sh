#!/bin/bash

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

set -eux

export DEBIAN_FRONTEND=noninteractive

# ── Base update ─────────────────────────────────────────────────────────
apt-get update -qq
apt-get install -y gnupg ca-certificates curl

# ── Install Aenigma repo
curl -fsSL https://packages.aenigma.ro/aenigma.gpg.key | gpg --dearmor -o /etc/apt/trusted.gpg.d/packages.aenigma.ro.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/trusted.gpg.d/packages.aenigma.ro.gpg] https://packages.aenigma.ro stable main" | tee /etc/apt/sources.list.d/packages.aenigma.ro.list

apt-get update -qq
curl -sI https://packages.aenigma.ro/dists/stable/Release
apt-get install --print-uris -y aenigma aenigma-image-utils
apt-get install -y aenigma aenigma-image-utils

# ── MOTD ──────────────────────────────────────────────────────────────────
# Symlink aenigma's status script into the MOTD directory
ln -sf /usr/bin/aenigma-status /etc/update-motd.d/99-zz-aenigma-status
 
# Ensure PAM's pam_motd module is active for both console and SSH logins
if ! grep -q "pam_motd" /etc/pam.d/login 2>/dev/null; then
    echo "session optional pam_motd.so motd=/run/motd.dynamic" \
        >> /etc/pam.d/login
fi
if ! grep -q "pam_motd" /etc/pam.d/sshd 2>/dev/null; then
    echo "session optional pam_motd.so motd=/run/motd.dynamic" \
        >> /etc/pam.d/sshd
fi
