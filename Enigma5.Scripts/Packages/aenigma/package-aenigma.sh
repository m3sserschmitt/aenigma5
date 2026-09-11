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

set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT_DIR="$SCRIPT_DIR/Deb"
SERVICE_NAME="aenigma"
EXECUTABLE_NAME="Enigma5.App"
CHANGELOG_FILE="$SCRIPT_DIR/changelog"
COPYRIGHT_FILE="$SCRIPT_DIR/copyright"
LINTIAN_OVERRIDES_FILE="$SCRIPT_DIR/$SERVICE_NAME.lintian-overrides"
MANPAGES_FILE="$SCRIPT_DIR/$SERVICE_NAME.manpages"
POSTINST_SCRIPT="$SCRIPT_DIR/postinst"
POSTRM_SCRIPT="$SCRIPT_DIR/postrm"
PROJECT_FILE="$SCRIPT_DIR/../../../$EXECUTABLE_NAME/$EXECUTABLE_NAME.csproj"
SERVICE_FILES="$SCRIPT_DIR/Services/*.service"
SCRIPT_FILES="$SCRIPT_DIR/Scripts/*"
MANPAGE_FILES="$SCRIPT_DIR/Manpages/*"

# Function to display usage/help message
show_help() {
    echo "Usage: $0 -v VERSION -c CONFIG -a ARCH"
    echo ""
    echo "Options:"
    echo "  -v VERSION  The version of the application (e.g., 1.0.0)"
    echo "  -c CONFIG   The config used for this package (e.g., "debian")"
    echo "  -a ARCH     The architecture for which is this package is built (e.g., "amd64", "arm64")"
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

PKG_DIR="$OUT_DIR/${SERVICE_NAME}_${VERSION}_${ARCH}"
DEB_FILE="$PKG_DIR.deb"
APP_SETTINGS_DIR="$SCRIPT_DIR/Configs/$CONFIG"
CONFIG_FILE="$APP_SETTINGS_DIR/appsettings.json"

if [[ ! -f "$CONFIG_FILE" ]]; then
    echo "Error: $CONFIG_FILE does not exist."
    exit 1
fi

# Step 1: Cleanup old package directory structure, then create a new one
if [ -d "$PKG_DIR" ]; then
    echo "Cleaning up existing package directory: $PKG_DIR"
    rm -rf "$PKG_DIR"
fi

# Step 2: Create a fresh package directory structure
mkdir -pv $PKG_DIR/debian
mkdir -pv $PKG_DIR/usr/lib/$SERVICE_NAME
mkdir -pv $PKG_DIR/usr/lib/systemd/system
mkdir -pv $PKG_DIR/usr/bin
mkdir -pv $PKG_DIR/etc/$SERVICE_NAME
mkdir -pv $PKG_DIR/man

# Step 3: Publish the .NET app
dotnet publish $PROJECT_FILE -c Release -r linux-$ARCH --self-contained true -o $PKG_DIR/usr/lib/$SERVICE_NAME

# Step 4: Copy application files to /usr/lib/APP_NAME
cp -v $POSTINST_SCRIPT $PKG_DIR/debian/postinst
cp -v $POSTRM_SCRIPT $PKG_DIR/debian/postrm
cp -v $LINTIAN_OVERRIDES_FILE $PKG_DIR/debian
cp -v $CHANGELOG_FILE $PKG_DIR/debian/changelog
cp -v $COPYRIGHT_FILE $PKG_DIR/debian/copyright
cp -v $MANPAGES_FILE $PKG_DIR/debian
cp -v $APP_SETTINGS_DIR/* $PKG_DIR/usr/lib/$SERVICE_NAME
cp -v $SERVICE_FILES $PKG_DIR/usr/lib/systemd/system
cp -v $SCRIPT_FILES $PKG_DIR/usr/bin
cp -v $CONFIG_FILE $PKG_DIR/etc/$SERVICE_NAME
cp -v $MANPAGE_FILES $PKG_DIR/man

# Step 5: Create control file
echo "Creating debian/control file"
cat <<EOF > $PKG_DIR/debian/control
Source: $SERVICE_NAME
Section: utils
Priority: optional
Maintainer: Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>
Build-Depends: debhelper-compat (= 13)
Standards-Version: 4.6.2

Package: $SERVICE_NAME
Architecture: $ARCH
Depends: \${misc:Depends}, libc6, openssl (>= 3.0.0), jq (>= 1.6), basez (>= 1.6.2)
Description: Federated messaging system
 Aenigma is a federated messaging system providing onion-service
 and VPN-based transport for self-hosted deployments.
EOF

echo "Creating debian/rules file"
cat <<EOF > $PKG_DIR/debian/rules
#!/usr/bin/make -f
%:
	dh \$@

override_dh_install:
	dh_install
	find debian/$SERVICE_NAME/usr/lib/$SERVICE_NAME/runtimes -mindepth 1 -maxdepth 1 ! -name "linux-$ARCH" -exec rm -rf {} +
	find debian/$SERVICE_NAME/usr/lib/$SERVICE_NAME  -type d -exec chmod 755 {} +
	find debian/$SERVICE_NAME/usr/lib/$SERVICE_NAME  -type f -exec chmod 644 {} +
	find debian/$SERVICE_NAME/usr/lib/$SERVICE_NAME -name '*.dll' -exec chmod 644 {} +
	chmod 755 debian/$SERVICE_NAME/usr/lib/$SERVICE_NAME/$EXECUTABLE_NAME

override_dh_shlibdeps:
	dh_shlibdeps -X usr/lib/$SERVICE_NAME

override_dh_makeshlibs:
	dh_makeshlibs -n
EOF

echo "Creating debian/$SERVICE_NAME.install file"
cat <<EOF > $PKG_DIR/debian/$SERVICE_NAME.install
usr/bin/aenigma-config
usr/bin/aenigma-keys
usr/bin/aenigma-launcher
usr/bin/aenigma-lock-key
usr/bin/aenigma-proxy
usr/bin/aenigma-standard-setup
usr/bin/aenigma-start
usr/bin/aenigma-status
usr/bin/aenigma-tor
usr/bin/aenigma-tor-auth
usr/bin/aenigma-tor-get-auth
usr/bin/aenigma-unlock-key
usr/bin/aenigma-vpn-client
usr/bin/aenigma-vpn-dns
usr/bin/aenigma-vpn-server
usr/bin/aenigma-vpn-server-client
usr/bin/aenigma-vpn-server-host
usr/lib/systemd/system/aenigma.service
usr/lib/$SERVICE_NAME/*
etc/$SERVICE_NAME
EOF

chmod 755 $PKG_DIR/debian/rules \
 $PKG_DIR/debian/postinst \
 $PKG_DIR/debian/postrm \
 $PKG_DIR/usr/bin/*

(cd $PKG_DIR && DEB_BUILD_OPTIONS=crossbuildcanrunhostbinaries dpkg-buildpackage -us -uc -b -a$ARCH)
lintian $DEB_FILE
