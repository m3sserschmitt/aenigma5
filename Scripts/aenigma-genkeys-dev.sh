#!/bin/sh

# Get the directory of the running script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Combine it with the relative path "../Scripts"
SCRIPTS_PATH="$SCRIPT_DIR/../Enigma5.App"

# Print the full path
echo "The Scripts directory is located at: $SCRIPTS_PATH"

openssl genrsa -aes256 -out ../Enigma5.App/private-key.pem -passout pass:1234 2048
openssl rsa -in ../Enigma5.App/private-key.pem -outform PEM -pubout -out ../Enigma5.App/public-key.pem -passin pass:1234
