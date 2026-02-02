#!/bin/bash

# Aenigma - Federal messaging system
# Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT_DIR="$SCRIPT_DIR/Deb"
SERVICE_NAME="aenigma"
EXECUTABLE_NAME="Enigma5.App"
POSTINST_SCRIPT="$SCRIPT_DIR/postinst"
POSTRM_SCRIPT="$SCRIPT_DIR/postrm"

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -v VERSION -c CONFIG -a ARCH"
    echo ""
    echo "Options:"
    echo "  -v VERSION  The version of the application (e.g., 1.0.0)"
    echo "  -c CONFIG   The config used for this package ("azure", "ubuntu")"
    echo "  -c ARCH     The config used for this package ("amd64", "arm64")"
    echo ""
    echo "Example:"
    echo "  $0 -v 1.0.0 -c ubuntu -a amd64"
    exit 1
}

# Parse command line arguments
while getopts "v:c:a:h" opt; do
    case $opt in
        v) VERSION=$OPTARG ;;
        c) CONFIG=$OPTARG ;;
        a) ARCH=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if version argument is provided
if [[ ! -v VERSION || ! -v CONFIG || ! -v ARCH ]]; then
    echo "Error: VERSION, CONFIG, ARCH are required."
    show_help
    exit 1
fi

PKG_DIR="$OUT_DIR/${SERVICE_NAME}_${VERSION}-${CONFIG}_${ARCH}"
CONFIG_DIR="$SCRIPT_DIR/Configs/$CONFIG"

if [[ ! -d "$CONFIG_DIR" ]]; then
    echo "Error: $CONFIG_DIR does not exist."
    exit 1
fi

# Step 1: Cleanup old package directory structure, then create a new one
if [ -d "$PKG_DIR" ]; then
    echo "Cleaning up existing package directory: $PKG_DIR"
    rm -rf "$PKG_DIR"
fi

# Step 2: Create a fresh package directory structure
mkdir -pv $PKG_DIR/DEBIAN
mkdir -pv $PKG_DIR/usr/local/$SERVICE_NAME

# Step 3: Publish the .NET app
dotnet publish $SCRIPT_DIR/../$EXECUTABLE_NAME/$EXECUTABLE_NAME.csproj -c Release -r linux-$ARCH --self-contained true -o $PKG_DIR/usr/local/$SERVICE_NAME

# Step 4: Copy application files to /usr/local/APP_NAME
cp -v $POSTINST_SCRIPT $PKG_DIR/DEBIAN/postinst
cp -v $POSTRM_SCRIPT $PKG_DIR/DEBIAN/postrm
cp -v $CONFIG_DIR/* $PKG_DIR/usr/local/$SERVICE_NAME
chmod -v +x $PKG_DIR/DEBIAN/postinst
chmod -v +x $PKG_DIR/DEBIAN/postrm

# Step 5: Create control file
echo "Creating DEBIAN/control file" 
cat <<EOF > $PKG_DIR/DEBIAN/control
Package: $SERVICE_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Depends: openssl (>= 3.0.0), jq, basez
Maintainer: Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>
Description: Federal messaging system
EOF

# Step 6: Build the Debian package
dpkg-deb --build --root-owner-group $PKG_DIR
echo "Package built: ${PKG_DIR}.deb"
