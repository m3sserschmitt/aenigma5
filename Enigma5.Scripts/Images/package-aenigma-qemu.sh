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

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

QEMU_DIR="$SCRIPT_DIR/Qemu"
VIRTUALBOX_DIR="$SCRIPT_DIR/Virtualbox"
VAGRANTFILE="$SCRIPT_DIR/Vagrantfiles/Qemu/Vagrantfile"

rm -rvf $QEMU_DIR
mkdir -pv $QEMU_DIR

(cd $QEMU_DIR && tar -xvf $VIRTUALBOX_DIR/aenigma-virtualbox.box)

shopt -s nullglob
VMDK_FILES=($QEMU_DIR/*.vmdk)
shopt -u nullglob

if [[ ${#VMDK_FILES[@]} -ne 1 ]]; then
    echo "Error: expected exactly one .vmdk file, found ${#VMDK_FILES[@]}" >&2
    exit 1
fi

SOURCE_VMDK="${VMDK_FILES[0]}"

cat > $QEMU_DIR/metadata.json <<'EOF'
{
  "provider": "libvirt",
  "format": "qcow2",
  "virtual_size": 64
}
EOF

qemu-img convert -f vmdk -O qcow2 $SOURCE_VMDK $QEMU_DIR/box.img

(cd $QEMU_DIR && tar cvzf aenigma-qemu.box metadata.json box.img)

shopt -s extglob
(cd $QEMU_DIR && rm -vf !(aenigma-qemu.box))
shopt -u extglob

cp -v $VAGRANTFILE $QEMU_DIR

exit 0
