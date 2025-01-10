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

APP_NAME="aenigma"
ARCH="amd64"
EXECUTABLE_NAME="Enigma5.App"
POSTINST_SCRIPT="../Scripts/postinst"
POSTRM_SCRIPT="../Scripts/postrm"
GENKEYS_NAME="aenigma-genkeys"
CONFIG_NAME="aenigma-configure"
AENIGMA_GENKEYS_SCRIPT="../Scripts/$GENKEYS_NAME.sh"
AENIGMA_CONFIG_SCRIPT="../Scripts/$CONFIG_NAME.sh"

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -v VERSION"
    echo ""
    echo "Options:"
    echo "  -v VERSION    The version of the application (e.g., 1.0.0)"
    echo ""
    echo "Example:"
    echo "  $0 -v 1.0.0"
    exit 1
}

# Parse command line arguments
while getopts "v:h" opt; do
    case $opt in
        v) VERSION=$OPTARG ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Check if version argument is provided
if [ -z "$VERSION" ]; then
    echo "Error: Version is required."
    show_help
fi

PKG_DIR="${APP_NAME}_${VERSION}-1_$ARCH"

# Step 1: Cleanup old package directory structure, then create a new one
if [ -d "$PKG_DIR" ]; then
    echo "Cleaning up existing package directory: $PKG_DIR"
    rm -rf "$PKG_DIR"
fi

# Create a fresh package directory structure
mkdir -pv $PKG_DIR/DEBIAN
mkdir -pv $PKG_DIR/usr/local/$APP_NAME
mkdir -pv $PKG_DIR/usr/local/bin
mkdir -pv $PKG_DIR/usr/local/etc/$APP_NAME
mkdir -pv $PKG_DIR/var/log/$APP_NAME

# Step 2: Publish the .NET app
dotnet publish -c Release -r linux-x64 --self-contained true -o $PKG_DIR/usr/local/$APP_NAME

# Step 3: Copy application files to /usr/local/APP_NAME
cp -v $POSTINST_SCRIPT $PKG_DIR/DEBIAN/postinst
cp -v $POSTRM_SCRIPT $PKG_DIR/DEBIAN/postrm
cp -v $AENIGMA_GENKEYS_SCRIPT $PKG_DIR/usr/local/$APP_NAME/
cp -v $AENIGMA_CONFIG_SCRIPT $PKG_DIR/usr/local/$APP_NAME/
rm -v $PKG_DIR/usr/local/$APP_NAME/appsettings.*.json
rm -v $PKG_DIR/usr/local/$APP_NAME/appsettings.json
chmod -v +x $PKG_DIR/DEBIAN/postinst
chmod -v +x $PKG_DIR/DEBIAN/postrm
chmod -v +x $PKG_DIR/usr/local/$APP_NAME/$GENKEYS_NAME.sh
chmod -v +x $PKG_DIR/usr/local/$APP_NAME/$CONFIG_NAME.sh

# Step 4: Create a symbolic link in /usr/local/bin pointing to the scripts
ln -sv /usr/local/$APP_NAME/$GENKEYS_NAME.sh $PKG_DIR/usr/local/bin/$GENKEYS_NAME
ln -sv /usr/local/$APP_NAME/$CONFIG_NAME.sh $PKG_DIR/usr/local/bin/$CONFIG_NAME

# Step 5: Create control file
echo "Creating DEBIAN/control file" 
cat <<EOF > $PKG_DIR/DEBIAN/control
Package: $APP_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Depends: openssl (>= 3.0.0)
Maintainer: Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>
Description: Federal messaging system
EOF

# Step 6: Build the Debian package
dpkg-deb --build $PKG_DIR

echo "Package built: ${PKG_DIR}.deb"
