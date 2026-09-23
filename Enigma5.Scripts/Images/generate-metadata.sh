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

# ---- Configuration (edit these to match your setup) ----
BOX_NAME="m3sserschmitt/aenigma5"
BOX_DESCRIPTION="Virtual images for Aenigma"
VBOX_BOX_PATH="$SCRIPT_DIR/Virtualbox/aenigma-virtualbox.box"
LIBVIRT_BOX_PATH="$SCRIPT_DIR/Qemu/aenigma-qemu.box"
RELEASE_BASE_URL="https://github.com/m3sserschmitt/aenigma-boxes/releases/download"
METADATA_FILE="$SCRIPT_DIR/metadata.json"
# ----------------------------------------------------------

usage() {
  echo "Usage: $0 <version>" >&2
  echo "  <version> must be strict semver, e.g. 1.0.0" >&2
  exit 1
}

[[ $# -eq 1 ]] || usage
VERSION="$1"

if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Error: version '$VERSION' is not strict semver (X.Y.Z)" >&2
  exit 1
fi

command -v jq >/dev/null 2>&1 || { echo "Error: jq is required but not installed." >&2; exit 1; }
command -v sha256sum >/dev/null 2>&1 || { echo "Error: sha256sum is required but not installed." >&2; exit 1; }

for f in "$VBOX_BOX_PATH" "$LIBVIRT_BOX_PATH"; do
  [[ -f "$f" ]] || { echo "Error: box file not found: $f" >&2; exit 1; }
done

echo "Computing checksums..."
VBOX_CHECKSUM=$(sha256sum "$VBOX_BOX_PATH" | awk '{print $1}')
LIBVIRT_CHECKSUM=$(sha256sum "$LIBVIRT_BOX_PATH" | awk '{print $1}')

VBOX_FILENAME=$(basename "$VBOX_BOX_PATH")
LIBVIRT_FILENAME=$(basename "$LIBVIRT_BOX_PATH")

TAG="v${VERSION}"
VBOX_URL="${RELEASE_BASE_URL}/${TAG}/${VBOX_FILENAME}"
LIBVIRT_URL="${RELEASE_BASE_URL}/${TAG}/${LIBVIRT_FILENAME}"

NEW_VERSION_JSON=$(jq -n \
  --arg version "$VERSION" \
  --arg vbox_url "$VBOX_URL" \
  --arg vbox_checksum "$VBOX_CHECKSUM" \
  --arg libvirt_url "$LIBVIRT_URL" \
  --arg libvirt_checksum "$LIBVIRT_CHECKSUM" \
  '{
    version: $version,
    providers: [
      { name: "virtualbox", url: $vbox_url, checksum_type: "sha256", checksum: $vbox_checksum },
      { name: "libvirt",    url: $libvirt_url, checksum_type: "sha256", checksum: $libvirt_checksum }
    ]
  }')

if [[ -f "$METADATA_FILE" ]]; then
  echo "Existing $METADATA_FILE found, updating..."
  EXISTS=$(jq --arg v "$VERSION" '[.versions[] | select(.version == $v)] | length' "$METADATA_FILE")
  if [[ "$EXISTS" -gt 0 ]]; then
    echo "Version $VERSION already present in $METADATA_FILE, replacing its entry."
    jq --arg v "$VERSION" --argjson newv "$NEW_VERSION_JSON" \
      '.versions = ([.versions[] | select(.version != $v)] + [$newv]) | .versions |= sort_by(.version)' \
      "$METADATA_FILE" > "${METADATA_FILE}.tmp"
  else
    jq --argjson newv "$NEW_VERSION_JSON" \
      '.versions += [$newv] | .versions |= sort_by(.version)' \
      "$METADATA_FILE" > "${METADATA_FILE}.tmp"
  fi
else
  echo "Creating new $METADATA_FILE"
  jq -n \
    --arg name "$BOX_NAME" \
    --arg description "$BOX_DESCRIPTION" \
    --argjson newv "$NEW_VERSION_JSON" \
    '{ name: $name, description: $description, versions: [$newv] }' \
    > "${METADATA_FILE}.tmp"
fi

mv "${METADATA_FILE}.tmp" "$METADATA_FILE"
echo "Done. $METADATA_FILE now contains version $VERSION."

exit 0
