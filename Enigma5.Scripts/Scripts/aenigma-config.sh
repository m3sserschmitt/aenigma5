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

set -Eeuo pipefail

SERVICE_USER="aenigma"
SERVICE_NAME="aenigma"
CONFIGS_DIR="/usr/local/etc/$SERVICE_NAME"
CONFIG_FILE="$CONFIGS_DIR/appsettings.json"

# Usage function
usage() {
    echo "Usage: $0 -p PROPERTY -v PROPERTY_VALUE"
    echo ""
    echo "Example: $0 -p OnionService -v \"o33eowc56dw2os5qehojnqcdwdqmobmgj76y67b6b3xgc47oedmde4yd.onion\""
    exit 1
}

if [[ $EUID -ne 0 ]]; then
    echo "Error: Please run the script as root."
    exit 1
fi

# Parse options
while getopts "p:v:h" opt; do
    case "$opt" in
        p) PROPERTY=$OPTARG ;;
        v) NEW_VALUE=$OPTARG ;;
        h) usage ;;
        *) usage ;;
    esac
done

if [ "$#" -lt 4 ]; then
    usage
    exit 1
fi

# Check that required options are provided
if [[ ! -v PROPERTY || ! -v NEW_VALUE ]]; then
    usage
    exit 1
fi

# Check file exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo "Error: file '$CONFIG_FILE' not found"
    exit 1
fi

TYPE="string"
if [[ "$NEW_VALUE" == "null" ]]; then
    TYPE="null"
elif [[ "$NEW_VALUE" == "true" || "$NEW_VALUE" == "false" ]]; then
    TYPE="bool"
elif [[ "$NEW_VALUE" =~ ^-?[0-9]+([.][0-9]+)?$ ]]; then
    TYPE="number"
fi

TEMPFILE=$(mktemp)
case "$TYPE" in
    string)
        jq --arg p "$PROPERTY" --arg v "$NEW_VALUE" 'setpath(($p|split(".")); $v)' "$CONFIG_FILE" > "$TEMPFILE"
        ;;
    number)
        jq --arg p "$PROPERTY" --argjson v "$NEW_VALUE" 'setpath(($p|split(".")); $v)' "$CONFIG_FILE" > "$TEMPFILE"
        ;;
    bool)
        jq --arg p "$PROPERTY" --argjson v "$NEW_VALUE" 'setpath(($p|split(".")); $v)' "$CONFIG_FILE" > "$TEMPFILE"
        ;;
    null)
        jq --arg p "$PROPERTY" 'setpath(($p|split(".")); null)' "$CONFIG_FILE" > "$TEMPFILE"
        ;;
esac

mv "$TEMPFILE" "$CONFIG_FILE"
chown -v "$SERVICE_USER":"$SERVICE_USER" $CONFIG_FILE

echo "Updated property '$PROPERTY' in '$CONFIG_FILE' to '$NEW_VALUE' ($TYPE)"
exit 0
