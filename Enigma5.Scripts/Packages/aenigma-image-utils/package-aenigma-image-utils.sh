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

SERVICE_NAME="aenigma-image-utils"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT_DIR="$SCRIPT_DIR/Deb"
POSTINST_SCRIPT="$SCRIPT_DIR/postinst"
POSTRM_SCRIPT="$SCRIPT_DIR/postrm"
SERVICE_FILES="$SCRIPT_DIR/Services/*.service"
TIMER_FILES="$SCRIPT_DIR/Services/*.timer"
CHANGELOG_FILE="$SCRIPT_DIR/changelog"
COPYRIGHT_FILE="$SCRIPT_DIR/copyright"

show_help() {
    echo "Usage: $0 -v VERSION"
    echo ""
    echo "Options:"
    echo "  -v VERSION  The version of the application (e.g., 1.0.0)"
    echo ""
    echo "Example:"
    echo "  $0 -v 1.0.0"
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
if [[ ! -v VERSION ]]; then
    echo "Error: VERSION is required."
    show_help
fi

PKG_DIR="$OUT_DIR/${SERVICE_NAME}_${VERSION}_all"
DEB_FILE="$PKG_DIR.deb"

if [ -d "$PKG_DIR" ]; then
    echo "Cleaning up existing package directory: $PKG_DIR"
    rm -rf "$PKG_DIR"
fi

mkdir -pv $PKG_DIR/debian
mkdir -pv $PKG_DIR/usr/lib/systemd/system

cp -v $POSTINST_SCRIPT $PKG_DIR/debian/postinst
cp -v $POSTRM_SCRIPT $PKG_DIR/debian/postrm
cp -v $CHANGELOG_FILE $PKG_DIR/debian/changelog
cp -v $SERVICE_FILES $PKG_DIR/usr/lib/systemd/system
cp -v $TIMER_FILES $PKG_DIR/usr/lib/systemd/system
cp -v $COPYRIGHT_FILE $PKG_DIR/debian/copyright

echo "Creating debian/rules file"
cat <<EOF > $PKG_DIR/debian/rules
#!/usr/bin/make -f
%:
	dh \$@
EOF

echo "Creating debian/$SERVICE_NAME.install file"
cat <<EOF > $PKG_DIR/debian/$SERVICE_NAME.install
usr/lib/systemd/system/aenigma-standard-setup.service
usr/lib/systemd/system/regenerate-ssh-host-keys.service
usr/lib/systemd/system/aenigma-auto-update.service
usr/lib/systemd/system/aenigma-auto-update.timer
EOF

echo "Creating debian/control file"
cat <<EOF > $PKG_DIR/debian/control
Source: $SERVICE_NAME
Section: utils
Priority: optional
Maintainer: Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>
Build-Depends: debhelper-compat (= 13)
Standards-Version: 4.6.2

Package: $SERVICE_NAME
Architecture: all
Depends: \${misc:Depends}, aenigma (>= 5.0.0)
Description: Additional scripts and services
 Contains additional scripts and services required by Aenigma Debian
 images.
EOF

chmod -v 755 $PKG_DIR/debian/postinst
chmod -v 755 $PKG_DIR/debian/postrm
chmod -v 755 $PKG_DIR/debian/rules

(cd $PKG_DIR && dpkg-buildpackage -us -uc -b)
lintian $DEB_FILE
