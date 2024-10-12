#!/bin/bash

set -e

APP_NAME="aenigma"
DB_NAME="aenigmaDb.sqlite"
ARCH="amd64"
EXECUTABLE_NAME="Enigma5.App"
PUBLISH_DIR="bin/Release/net8.0/linux-x64/publish"
POSTINST_SCRIPT="../Scripts/postinst"
POSTRM_SCRIPT="../Scripts/postrm"
GENKEYS_NAME="aenigma-genkeys"
AENIGMA_GENKEYS_SCRIPT="../Scripts/$GENKEYS_NAME.sh"

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

# Step 1: Publish the .NET app
rm -fv Migrations/migrate-db.sql
rm -fv "$DB_NAME"
dotnet publish -c Release -r linux-x64 --self-contained true
dotnet ef migrations script -o Migrations/migrate-db.sql
sqlite3 "$DB_NAME" < Migrations/migrate-db.sql

# Step 2: Cleanup old package directory structure, then create a new one
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

# Step 3: Copy application files to /usr/local/APP_NAME
cp -rv $PUBLISH_DIR/* $PKG_DIR/usr/local/$APP_NAME/
cp -v *.sqlite $PKG_DIR/usr/local/$APP_NAME/
cp -v "$POSTINST_SCRIPT" $PKG_DIR/DEBIAN/postinst
cp -v "$POSTRM_SCRIPT" $PKG_DIR/DEBIAN/postrm
cp -v "$AENIGMA_GENKEYS_SCRIPT" $PKG_DIR/usr/local/$APP_NAME/
chmod -v +x $PKG_DIR/DEBIAN/postinst
chmod -v +x $PKG_DIR/DEBIAN/postrm
chmod -v +x $PKG_DIR/usr/local/$APP_NAME/$GENKEYS_NAME.sh

# Step 4: Create a symbolic link in /usr/local/bin pointing to the executable
ln -sv /usr/local/$APP_NAME/$EXECUTABLE_NAME $PKG_DIR/usr/local/bin/$APP_NAME
ln -sv /usr/local/$APP_NAME/$GENKEYS_NAME.sh $PKG_DIR/usr/local/bin/$GENKEYS_NAME

# Step 5: Create control file
echo "Creating DEBIAN/control file" 
cat <<EOF > $PKG_DIR/DEBIAN/control
Package: $APP_NAME
Version: $VERSION
Section: base
Priority: optional
Architecture: $ARCH
Depends: openssl (>= 3.0.2)
Maintainer: Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>
Description: Simple onion routing application.
EOF

# Step 6: Build the Debian package
dpkg-deb --build $PKG_DIR

echo "Package built: ${PKG_DIR}.deb"
