#!/bin/bash

set -e

APP_NAME="aenigma"
SERVICE_USER=$APP_NAME
KEYS_DIR="/usr/local/etc/$APP_NAME"
PRIVATE_KEY_FILE="$KEYS_DIR/private-key.pem"
PUBLIC_KEY_FILE="$KEYS_DIR/public-key.pem"
KEY_SIZE="2048"

# Generate the private key with the given size and file name
echo "Generating private key ... "
openssl genrsa -aes256 -out "$PRIVATE_KEY_FILE" "$KEY_SIZE"
echo "Done."

# Extract the public key from the private key and save it to the specified file
echo "Exporting public key ... "
openssl rsa -in "$PRIVATE_KEY_FILE" -outform PEM -pubout -out "$PUBLIC_KEY_FILE"
echo "Done."

echo "Keys generated successfully:"
echo "Private key: $PRIVATE_KEY_FILE"
echo "Public key: $PUBLIC_KEY_FILE"

# Changing ownership for generated files
echo "Changing keys ownership to $SERVICE_USER ..."
sudo chown "$SERVICE_USER":"$SERVICE_USER" "$PRIVATE_KEY_FILE"
sudo chown "$SERVICE_USER":"$SERVICE_USER" "$PUBLIC_KEY_FILE"
echo "Done."
