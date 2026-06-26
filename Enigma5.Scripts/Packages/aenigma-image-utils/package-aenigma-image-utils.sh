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
OUT_DIR="$SCRIPT_DIR/Deb"
POSTINST_SCRIPT="$SCRIPT_DIR/postinst"
POSTRM_SCRIPT="$SCRIPT_DIR/postrm"
SERVICES="$SCRIPT_DIR/Services/*.service"

show_help() {
    echo "Usage: $0 -v VERSION -a ARCH"
    echo ""
    echo "Options:"
    echo "  -v VERSION  The version of the application (e.g., 1.0.0-debian_amd64)"
    echo "  -a ARCH     The architecture for which is this package is built (e.g., "amd64", "arm64")"
    echo ""
    echo "Example:"
    echo "  $0 -v 1.0.0 -v 1.0.0 -a amd64"
    exit 1
}

# Parse command line arguments
while getopts "v:a:h" opt; do
    case $opt in
        v) VERSION=$OPTARG ;;
        a) ARCH=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if version argument is provided
if [[ ! -v VERSION || ! -v ARCH ]]; then
    echo "Error: VERSION and ARCH are required."
    show_help
fi

PKG_DIR="$OUT_DIR/aenigma-image-utils_$VERSION-debian_$ARCH"

if [ -d "$PKG_DIR" ]; then
    echo "Cleaning up existing package directory: $PKG_DIR"
    rm -rf "$PKG_DIR"
fi

mkdir -pv $PKG_DIR/DEBIAN
mkdir -pv $PKG_DIR/usr/lib/systemd/system

cp -v $POSTINST_SCRIPT $PKG_DIR/DEBIAN/postinst
cp -v $POSTRM_SCRIPT $PKG_DIR/DEBIAN/postrm
cp -v $SERVICES $PKG_DIR/usr/lib/systemd/system

chmod -v 755 $PKG_DIR/DEBIAN/postinst
chmod -v 755 $PKG_DIR/DEBIAN/postrm
chmod -v 755 $PKG_DIR/usr/lib/systemd/system/*.service

cat <<EOF > $PKG_DIR/DEBIAN/control
Package: aenigma-image-utils
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Depends: aenigma (>= 5.0.0)
Maintainer: Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>
Description: Additional scripts and services for aenigma debian images
EOF

dpkg-deb --build --root-owner-group "$PKG_DIR"
